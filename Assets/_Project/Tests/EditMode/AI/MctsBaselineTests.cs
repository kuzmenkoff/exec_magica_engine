using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Step 1 gate for MCTS: the perfect-information ISMCTS skeleton must beat Random.
/// Synthetic mirror deck isolates policy quality. Small budgets keep the test fast;
/// the rigorous "win rate vs iterations" curve on real decks is a Phase 3.1 deliverable.
/// </summary>
[TestFixture]
public class MctsBaselineTests
{
    private static readonly (int id, int atk, int hp, int cost)[] Curve =
    {
        (9001, 1, 2, 1), (9002, 2, 1, 1), (9003, 2, 3, 2), (9004, 3, 2, 2),
        (9005, 3, 4, 3), (9006, 4, 3, 3), (9007, 4, 5, 4), (9008, 5, 4, 4),
        (9009, 5, 6, 5), (9010, 6, 5, 5), (9011, 6, 7, 6), (9012, 7, 6, 6),
        (9013, 7, 8, 7), (9014, 8, 8, 8), (9015, 4, 4, 3),
    };

    private static Card Minion(int id, int atk, int hp, int cost) => new Card
    {
        id = id,
        Title = "VM" + id,
        Class = Card.CardClass.ENTITY,
        Attack = atk,
        HP = hp,
        MaxHP = hp,
        ManaCost = cost,
        IsCollectible = true,
        Keywords = new List<KeywordType>(),
        Effects = new List<CardEffect>()
    };

    private static AllCards BuildDeck()
    {
        AllCards d = new AllCards { cards = new List<Card>() };
        foreach (var c in Curve) { d.cards.Add(Minion(c.id, c.atk, c.hp, c.cost)); d.cards.Add(Minion(c.id, c.atk, c.hp, c.cost)); }
        return d;
    }

    private static AllCards BuildDatabase()
    {
        AllCards db = new AllCards { cards = new List<Card>() };
        foreach (var c in Curve) db.cards.Add(Minion(c.id, c.atk, c.hp, c.cost));
        return db;
    }

    private static MctsConfig PerfectInfo(int iterations, int seed) => new MctsConfig
    {
        BudgetMode = MctsConfig.Budget.Iterations,
        Iterations = iterations,
        Determinize = true,
        RolloutPolicy = MctsConfig.Rollout.Random,
        MaxRolloutActions = 150,
        Seed = seed
    };

    [Test]
    public void Mcts_BeatsRandom_AtLeast70Percent()
    {
        const int games = 20;
        const int iterations = 100;
        AllCards db = BuildDatabase();

        int mctsWins = 0, decisive = 0;
        for (int i = 0; i < games; i++)
        {
            bool mctsIsPlayer = (i % 2) == 0;
            bool playerFirst = ((i / 2) % 2) == 0;

            GameState initial = GameStateBuilder.CreateInitialState(
                BuildDeck(), BuildDeck(), db, playerFirst, shuffleSeed: 2000 + i);

            IGameActionPolicy mcts = new MctsActionPolicy(PerfectInfo(iterations, 5000 + i));
            IGameActionPolicy random = new RandomActionPolicy(6000 + i);

            IGameActionPolicy p = mctsIsPlayer ? mcts : random;
            IGameActionPolicy e = mctsIsPlayer ? random : mcts;

            GameState final = GameSimulationRunner.SimulatePlayout(initial, p, e, maxActions: 400);
            if (!final.IsGameOver || !final.Winner.HasValue)
                continue;

            decisive++;
            PlayerSide mctsSide = mctsIsPlayer ? PlayerSide.Player : PlayerSide.Enemy;
            if (final.Winner.Value == mctsSide)
                mctsWins++;
        }

        double winRate = (double)mctsWins / decisive;
        TestContext.WriteLine($"MCTS({iterations}, perfect-info) vs Random: {winRate:P1} ({mctsWins}/{decisive} decisive)");
        Assert.That(decisive, Is.GreaterThan(games / 2), $"Too many non-decisive games: {decisive}/{games}");
        Assert.That(winRate, Is.GreaterThan(0.70), $"MCTS win rate {winRate:P1} (need > 70%)");
    }

    [Test]
    [Category("Benchmark")]
    public void Mcts_Parallel_DecisionSpeed()
    {
        AllCards db = BuildDatabase();

        // Advance to a rich mid-game position so the agent has a real (non-trivial) decision.
        GameState state = GameStateBuilder.CreateInitialState(BuildDeck(), BuildDeck(), db, true, shuffleSeed: 42);
        GameEngine eng = new GameEngine(state);
        var warm = new RandomActionPolicy(99);
        for (int i = 0; i < 40 && !state.IsGameOver; i++)
        {
            var la = eng.GetLegalActions(state.ActiveSide);
            if (la != null && la.Count >= 6 && i >= 12) break; // stop at a busy decision point
            var act = warm.ChooseAction(state, la, state.ActiveSide);
            if (act == null) break;
            eng.ApplyAction(act);
        }
        state = eng.State;

        List<GameAction> legal = CoreLegalActionGenerator.GetLegalActions(state, state.ActiveSide);
        Assert.That(state.IsGameOver, Is.False, "Mid-game setup ended the game; tweak seed/steps");
        Assert.That(legal.Count, Is.GreaterThan(1), "Need a non-trivial decision to benchmark");

        const int totalIterations = 5000;  // real work now; scale to taste

        MctsConfig Cfg(bool parallel, int threadCount) => new MctsConfig
        {
            BudgetMode = MctsConfig.Budget.Iterations,
            Iterations = totalIterations,
            RolloutPolicy = MctsConfig.Rollout.Random,
            MaxRolloutActions = 120,
            Determinize = true,
            Parallelize = parallel,
            ThreadCount = threadCount,
            Seed = 123
        };

        double Measure(bool parallel, int threadCount)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            GameAction a = new MctsActionPolicy(Cfg(parallel, threadCount))
                .ChooseAction(state, legal, state.ActiveSide);
            sw.Stop();
            Assert.That(a, Is.Not.Null);
            return sw.Elapsed.TotalMilliseconds;
        }

        int cores = System.Environment.ProcessorCount;
        Measure(false, 1);            // warm up (JIT), discard
        Measure(true, cores);

        var counts = new SortedSet<int> { 1, 2, 4, 8, 16, cores };
        counts.RemoveWhere(c => c > cores);

        double baseline = Measure(false, 1);
        TestContext.WriteLine($"Cores={cores}, totalIterations={totalIterations}, legalActions={legal.Count}");
        TestContext.WriteLine($"threads= 1 : {baseline,9:0.0} ms  (x1.00, baseline)");
        foreach (int t in counts)
        {
            if (t == 1) continue;
            double ms = Measure(true, t);
            TestContext.WriteLine($"threads={t,2} : {ms,9:0.0} ms  (x{baseline / ms:0.00})");
        }
    }
}
