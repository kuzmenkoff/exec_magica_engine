using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

/// <summary>
/// Information Set MCTS policy.
/// Tree keyed by canonical ACTION keys; each iteration re-determinizes the hidden world
/// (opponent hand/deck and both deck orders), descends via the real GameEngine, expands one
/// node, rolls out to terminal, and backpropagates a win/loss from the ROOT player's
/// perspective (per-node chooser flip). UCB1 uses availability counts (Cowling 2012) so
/// actions that are legal only in some determinizations are handled correctly.
///
/// Information model (Phase 2.0): no model sees hidden info. Hidden opponent hand cards are
/// keyed by CardId (type), not InstanceId, so the agent never relies on specific hidden
/// identities; it reasons over the information set.
/// </summary>
public class MctsActionPolicy : IGameActionPolicy
{
    private sealed class Node
    {
        public readonly string Key;                       // canonical action key (null at root)
        public readonly Node Parent;
        public readonly PlayerSide Chooser;               // side to move at the parent
        public readonly Dictionary<string, Node> Children = new Dictionary<string, Node>();

        public int Visits;
        public double RootWins;                            // ROOT player's perspective
        public int Availability = 1;

        public Node(string key, Node parent, PlayerSide chooser)
        {
            Key = key;
            Parent = parent;
            Chooser = chooser;
        }
    }

    private readonly MctsConfig config;
    private readonly System.Random rng;

    public MctsActionPolicy(MctsConfig config = null)
    {
        this.config = config ?? new MctsConfig();
        rng = this.config.Seed == 0 ? new System.Random() : new System.Random(this.config.Seed);
    }

    public GameAction ChooseAction(GameState state, List<GameAction> legalActions, PlayerSide actorSide)
    {
        if (state == null || legalActions == null || legalActions.Count == 0)
            return null;
        if (legalActions.Count == 1)
            return legalActions[0];

        int threads = ResolveThreadCount();
        Dictionary<string, (int visits, double wins)> aggregate;

        if (!config.Parallelize || threads <= 1)
        {
            Node root = RunSearch(state, actorSide, config.Iterations, rng);
            aggregate = AggregateRoots(new[] { root });
        }
        else
        {
            // Iterations is the TOTAL budget, split across threads (keeps the knob comparable).
            int perThread = config.BudgetMode == MctsConfig.Budget.Iterations
                ? Math.Max(1, (config.Iterations + threads - 1) / threads)
                : 0; // Time mode: each thread runs to TimeBudgetMs (handled in RunSearch)

            Node[] roots = new Node[threads];
            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = threads };
            Parallel.For(0, threads, po, i =>
            {
                // Per-thread RNG: deterministic when seeded (sum aggregation is commutative,
                // so the result is independent of thread scheduling).
                System.Random threadRng = config.Seed == 0
                    ? new System.Random()
                    : new System.Random(config.Seed + i + 1);
                roots[i] = RunSearch(state, actorSide, perThread, threadRng);
            });
            aggregate = AggregateRoots(roots);
        }

        string bestKey = SelectFinalKey(aggregate);
        if (bestKey == null)
            return legalActions[0];

        foreach (GameAction a in legalActions)
            if (ActionKey(a, state, actorSide) == bestKey)
                return a;
        return legalActions[0];
    }

    private int ResolveThreadCount()
    {
        int t = config.ThreadCount <= 0 ? Environment.ProcessorCount : config.ThreadCount;
        return Math.Max(1, t);
    }

    private Node RunSearch(GameState trueState, PlayerSide actorSide, int iterationBudget, System.Random localRng)
    {
        Node root = new Node(null, null, actorSide);

        if (config.BudgetMode == MctsConfig.Budget.Time)
        {
            Stopwatch sw = Stopwatch.StartNew();
            do { Iterate(trueState, actorSide, root, localRng); }
            while (sw.ElapsedMilliseconds < config.TimeBudgetMs);
        }
        else
        {
            int n = Math.Max(1, iterationBudget);
            for (int i = 0; i < n; i++)
                Iterate(trueState, actorSide, root, localRng);
        }

        return root;
    }

    private static Dictionary<string, (int visits, double wins)> AggregateRoots(IEnumerable<Node> roots)
    {
        Dictionary<string, (int visits, double wins)> agg = new Dictionary<string, (int, double)>();
        foreach (Node root in roots)
        {
            if (root == null) continue;
            foreach (KeyValuePair<string, Node> kv in root.Children)
            {
                agg.TryGetValue(kv.Key, out (int visits, double wins) cur);
                agg[kv.Key] = (cur.visits + kv.Value.Visits, cur.wins + kv.Value.RootWins);
            }
        }
        return agg;
    }

    private string SelectFinalKey(Dictionary<string, (int visits, double wins)> aggregate)
    {
        string best = null;
        double bestScore = double.NegativeInfinity;
        foreach (KeyValuePair<string, (int visits, double wins)> kv in aggregate)
        {
            double score = config.FinalAction == MctsConfig.FinalSelection.MaxValue
                ? (kv.Value.visits > 0 ? kv.Value.wins / kv.Value.visits : double.NegativeInfinity)
                : kv.Value.visits;
            if (score > bestScore) { bestScore = score; best = kv.Key; }
        }
        return best;
    }

    private void Iterate(GameState trueState, PlayerSide root, Node rootNode, System.Random localRng)
    {
        GameState world = BuildDeterminizedRoot(trueState, root, localRng);
        GameEngine engine = new GameEngine(world);

        Node node = rootNode;
        while (!engine.State.IsGameOver)
        {
            PlayerSide toMove = engine.State.ActiveSide;
            List<GameAction> legal = engine.GetLegalActions(toMove);
            if (legal == null || legal.Count == 0)
                break;

            // Map canonical key -> a concrete legal action in THIS world.
            Dictionary<string, GameAction> byKey = new Dictionary<string, GameAction>();
            foreach (GameAction a in legal)
            {
                string k = ActionKey(a, engine.State, root);
                if (!byKey.ContainsKey(k))
                    byKey[k] = a;
            }

            // Expansion: a legal key with no child yet.
            List<string> untried = null;
            foreach (string k in byKey.Keys)
                if (!node.Children.ContainsKey(k))
                    (untried ??= new List<string>()).Add(k);

            if (untried != null)
            {
                string k = untried[localRng.Next(untried.Count)];
                engine.ApplyAction(byKey[k]);
                Node child = new Node(k, node, toMove);
                node.Children[k] = child;
                node = child;
                break;
            }

            // Selection: best child whose key is legal in this world (bump availability first).
            foreach (KeyValuePair<string, Node> kv in node.Children)
                if (byKey.ContainsKey(kv.Key))
                    kv.Value.Availability++;

            Node best = null;
            string bestKey = null;
            double bestVal = double.NegativeInfinity;
            foreach (KeyValuePair<string, Node> kv in node.Children)
            {
                if (!byKey.ContainsKey(kv.Key))
                    continue;
                double val = Ucb(kv.Value, root);
                if (val > bestVal) { bestVal = val; best = kv.Value; bestKey = kv.Key; }
            }
            if (best == null)
                break;

            engine.ApplyAction(byKey[bestKey]);
            node = best;
        }

        double result = Rollout(engine, root, localRng);

        for (Node n = node; n != null; n = n.Parent)
        {
            n.Visits++;
            n.RootWins += result;
        }
    }

    private GameState BuildDeterminizedRoot(GameState trueState, PlayerSide root, System.Random localRng)
    {
        GameState copy = trueState.GetDeepCopy();
        if (!config.Determinize)
            return copy; // perfect-information mode

        PlayerState me = copy.GetPlayerState(root);
        PlayerState opp = copy.GetOpponentState(root);

        // Own deck: contents known, order hidden -> shuffle.
        Shuffle(me.Deck, localRng);

        // Opponent hidden zones (hand + deck). Under the known-decklist assumption these are
        // exactly the cards we cannot see; re-deal them randomly (handCount to hand, rest to deck).
        // KnowsOpponentDeck=false would instead sample from the global card pool (TODO).
        List<CardInstance> hidden = new List<CardInstance>();
        if (opp.Hand != null) hidden.AddRange(opp.Hand);
        if (opp.Deck != null) hidden.AddRange(opp.Deck);
        Shuffle(hidden, localRng);

        int handCount = opp.Hand != null ? opp.Hand.Count : 0;
        opp.Hand = new List<CardInstance>();
        opp.Deck = new List<CardInstance>();
        for (int i = 0; i < hidden.Count; i++)
        {
            CardInstance c = hidden[i];
            if (c == null) continue;
            if (i < handCount) { c.Zone = GameZone.Hand; opp.Hand.Add(c); }
            else { c.Zone = GameZone.Deck; opp.Deck.Add(c); }
        }

        return copy;
    }

    private static void Shuffle(List<CardInstance> list, System.Random rng)
    {
        if (list == null) return;
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            CardInstance tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private double Ucb(Node c, PlayerSide root)
    {
        if (c.Visits == 0)
            return double.MaxValue;
        double q = c.RootWins / c.Visits;
        double exploit = (c.Chooser == root) ? q : (1.0 - q);
        double explore = config.ExplorationC * Math.Sqrt(Math.Log(c.Availability) / c.Visits);
        return exploit + explore;
    }

    private double Rollout(GameEngine engine, PlayerSide root, System.Random localRng)
    {
        GameState s = engine.State;
        if (s.IsGameOver)
            return Outcome(s, root);

        int rolloutSeed = config.Seed == 0 ? 0 : localRng.Next(1, int.MaxValue);
        IGameActionPolicy policy = config.RolloutPolicy == MctsConfig.Rollout.Greedy
            ? new GreedyActionPolicy(rolloutSeed)
            : (IGameActionPolicy)new RandomActionPolicy(rolloutSeed);

        // Roll out in place on this iteration's private world (no extra deep copy).
        for (int i = 0; i < config.MaxRolloutActions; i++)
        {
            if (s.IsGameOver) break;
            List<GameAction> legal = engine.GetLegalActions(s.ActiveSide);
            if (legal == null || legal.Count == 0) break;
            GameAction action = policy.ChooseAction(s, legal, s.ActiveSide);
            if (action == null) break;
            if (!engine.ApplyAction(action).Success) break;
        }

        return Outcome(s, root);
    }

    private static double Outcome(GameState s, PlayerSide root)
    {
        if (!s.IsGameOver || !s.Winner.HasValue)
            return 0.5;
        return s.Winner.Value == root ? 1.0 : 0.0;
    }

    // Canonical key. Hidden opponent hand plays are keyed by CardId (type), everything else
    // by stable public InstanceId / hero side, so the key is consistent across determinizations.
    private static string ActionKey(GameAction a, GameState world, PlayerSide root)
    {
        if (a.Type == GameActionType.EndTurn)
            return "E";

        string src;
        if (a.ActorSide != root && a.Type == GameActionType.PlayCard)
            src = "C" + LookupCardId(world, a.SourceInstanceId);   // hidden -> by type
        else
            src = "I" + a.SourceInstanceId;                         // visible -> by instance

        string tgt;
        if (a.TargetType == PlayTargetType.Card && a.TargetInstanceId.HasValue)
            tgt = "t" + a.TargetInstanceId.Value;
        else if (a.TargetType == PlayTargetType.Hero && a.TargetHeroSide.HasValue)
            tgt = "h" + a.TargetHeroSide.Value;
        else
            tgt = "n";

        return a.Type + "|" + src + "|" + tgt;
    }

    private static int LookupCardId(GameState world, int instanceId)
    {
        CardId(world.Player, instanceId, out int id);
        if (id != int.MinValue) return id;
        CardId(world.Enemy, instanceId, out id);
        return id == int.MinValue ? -1 : id;

        void CardId(PlayerState ps, int iid, out int found)
        {
            found = int.MinValue;
            if (ps?.Hand == null) return;
            foreach (CardInstance c in ps.Hand)
                if (c != null && c.InstanceId == iid) { found = c.CardId; return; }
        }
    }
}
