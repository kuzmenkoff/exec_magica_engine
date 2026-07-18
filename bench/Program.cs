using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

static class Program
{
    static void Main(string[] args)
    {
        System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        if (args.Length == 0) { Console.Error.WriteLine("usage: bench <config.json>"); return; }
        var s = JsonConvert.DeserializeObject<BenchRunSpec>(File.ReadAllText(args[0]));

        Resources.Root = !string.IsNullOrEmpty(s.resourcesRoot) ? s.resourcesRoot : Path.GetFullPath("../Assets/Resources");
        string repoRoot = Directory.GetParent(Resources.Root).Parent.FullName;

        var db = new AllCards();
        foreach (TextAsset ta in Resources.LoadAll<TextAsset>("CardsInfo/AllCards"))
        { var set = CardJsonLoader.LoadAllCards(ta.text); if (set != null) db.AddCardSet(set); }

        if (s.mode == "generate") Generate(s, db, repoRoot);
        else if (s.mode == "match") Match(s, db, repoRoot);
        else if (s.mode == "ladder") Ladder(s, db, repoRoot);
        else if (s.mode == "duel") Duel(s, db, repoRoot);
        else if (s.mode == "ceiling") Ceiling(s, db, repoRoot);
        else if (s.mode == "batch") Batch(s, db, repoRoot);
        else Console.Error.WriteLine("unknown mode: " + s.mode);
    }

    static void Generate(BenchRunSpec s, AllCards db, string repoRoot)
    {
        int baseSeed = s.baseSeed != 0 ? s.baseSeed : s.generation * 1_000_000 + 1;
        int parallel = s.parallelGames > 0 ? s.parallelGames : Environment.ProcessorCount;
        string outDir = !string.IsNullOrEmpty(s.outDir) ? s.outDir : Path.Combine(repoRoot, "Runs", "SelfPlayData", $"gen{s.generation}");
        string tag = !string.IsNullOrEmpty(s.runTag) ? s.runTag : $"gen{s.generation}";

        SelfPlayDataGenerator.FeatureVersion = s.featureVersion;
        NeuralNet net = string.IsNullOrEmpty(s.networkResource) ? null
            : new NeuralNet(File.ReadAllBytes(Path.Combine(Resources.Root, s.networkResource + ".bytes")));
        var rollout = (MctsConfig.Rollout)Enum.Parse(typeof(MctsConfig.Rollout), s.rollout);

        MctsConfig Cfg(int seed) => new MctsConfig
        {
            BudgetMode = MctsConfig.Budget.Iterations,
            Iterations = s.iterations,
            RolloutPolicy = rollout,
            MaxRolloutActions = s.maxRolloutActions,
            FinalAction = MctsConfig.FinalSelection.MaxVisits,
            Determinize = true,
            KnowsOpponentDeck = true,
            Parallelize = false,
            Seed = seed,
            Network = net,
            PuctC = s.puctC,
            LeafRolloutMix = s.leafRolloutMix
        };
        AllCards MakeDeck(int seed)
        {
            var pick = new Random(seed ^ 0x5f3759);
            return pick.NextDouble() < s.presetFraction ? RuntimeDeckLoader.RandomPreset(db, seed) : RuntimeDeckLoader.RandomDeck(db, seed);
        }

        Directory.CreateDirectory(outDir);
        string layout = s.featureVersion >= 3 ? StateEncoder.LayoutVersionV3
                      : s.featureVersion >= 2 ? StateEncoder.LayoutVersionV2
                      : StateEncoder.LayoutVersion;
        var meta = new
        {
            generation = s.generation,
            createdUtc = DateTime.UtcNow.ToString("o"),
            cardSetRevision = new SessionRecord().CardSetRevision,
            featureVersion = layout,
            teacher = new
            {
                type = net != null ? "NN+MCTS" : "MCTS",
                network = s.networkResource,
                rollout = rollout.ToString(),
                iterations = s.iterations,
                maxRolloutActions = s.maxRolloutActions,
                leafRolloutMix = s.leafRolloutMix,
                puctC = s.puctC
            },
            decks = $"preset{(int)(s.presetFraction * 100)}/random{(int)((1 - s.presetFraction) * 100)}",
            baseSeed,
            targetGames = s.games,
            maxActions = s.maxActions,
            runtime = ".NET bench"
        };
        File.WriteAllText(Path.Combine(outDir, "meta.json"),
            Newtonsoft.Json.JsonConvert.SerializeObject(meta, Newtonsoft.Json.Formatting.Indented));

        Console.WriteLine($"gen{s.generation}: {s.games} games, teacher={(net != null ? $"nnmcts h{net.Hidden} mix{s.leafRolloutMix}" : "MCTS")} it={s.iterations}, {parallel} cores → {outDir}");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        SelfPlayDataGenerator.GenerateParallelByFile(MakeDeck, MakeDeck, db,
            seed => new MctsActionPolicy(Cfg(seed)), seed => new MctsActionPolicy(Cfg(seed + 500_000)),
            s.games, s.gamesPerFile, baseSeed, outDir, tag, parallel,
            (done, total) => { double m = sw.Elapsed.TotalMinutes; Console.WriteLine($"  {done}/{total} — {m:F1} min, {done / Math.Max(0.01, m):F0}/min"); return true; });
        Console.WriteLine($"DONE in {sw.Elapsed.TotalMinutes:F1} min");
    }

    static void Match(BenchRunSpec s, AllCards db, string repoRoot)
    {
        var net = string.IsNullOrEmpty(s.networkResource) ? null : new NeuralNet(File.ReadAllBytes(Path.Combine(Resources.Root, s.networkResource + ".bytes")));
        int parallel = s.parallelGames > 0 ? s.parallelGames : Environment.ProcessorCount;
        var decks = new Dictionary<string, AllCards>();
        foreach (var d in s.decks) decks[d] = RuntimeDeckLoader.LoadPreset(d, db);

        MctsConfig B(int sd, int ms) => new MctsConfig
        {
            BudgetMode = MctsConfig.Budget.Time,
            TimeBudgetMs = ms,
            ExplorationC = 1.41,
            FinalAction = MctsConfig.FinalSelection.MaxVisits,
            Determinize = true,
            KnowsOpponentDeck = true,
            Parallelize = false,
            Seed = sd
        };
        MctsConfig Grd(int sd, int ms) { var c = B(sd, ms); c.RolloutPolicy = MctsConfig.Rollout.Greedy; c.MaxRolloutActions = 40; return c; }
        MctsConfig Rnd(int sd, int ms) { var c = B(sd, ms); c.RolloutPolicy = MctsConfig.Rollout.Random; c.MaxRolloutActions = 40; return c; }
        MctsConfig Nn(int sd, int ms) { var c = B(sd, ms); c.RolloutPolicy = MctsConfig.Rollout.Random; c.MaxRolloutActions = 40; c.Network = net; c.PuctC = s.puctC; c.LeafRolloutMix = s.leafRolloutMix; return c; }

        var agents = new List<(string name, Func<int, int, MctsConfig> cfg, bool neural)> { ("mcts-greedy", Grd, false), ("mcts-random", Rnd, false) };
        if (net != null) agents.Add(("nnmcts", Nn, true));

        var sb = new StringBuilder();
        sb.AppendLine($"# Strength vs time ({DateTime.Now:yyyy-MM-dd HH:mm})\n");
        sb.AppendLine("| agent | deck | budget ms | win% vs Greedy | Wilson95 | iters/move | think/move | turns |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var (name, cfgFn, neural) in agents)
            foreach (var deckName in s.decks)
                foreach (int ms in s.timeBudgetsMs)
                {
                    MctsActionPolicy.ResetDiagnostics();
                    var cfg = new BatchConfig { Games = s.gamesPerCell, BaseSeed = 7000, MaxActions = s.maxActions, AlternateStart = true, LogEvents = false, MaxParallelGames = parallel };
                    var r = BatchRunner.Run(_ => decks[deckName], _ => decks[deckName], db,
                        seed => new MctsActionPolicy(cfgFn(seed, ms)), seed => new GreedyActionPolicy(seed + 1),
                        new ModelInfo { ModelId = name, Params = new Dictionary<string, object>() }, new ModelInfo { ModelId = "greedy", Params = new Dictionary<string, object>() },
                        deckName, deckName, cfg);
                    var q = r.Summary;
                    long it = neural ? MctsActionPolicy.DiagIterNeural : MctsActionPolicy.DiagIterPlain;
                    long mv = neural ? MctsActionPolicy.DiagMovesNeural : MctsActionPolicy.DiagMovesPlain;
                    double ipm = mv > 0 ? (double)it / mv : 0;
                    sb.AppendLine($"| {name} | {deckName} | {ms} | {q.PlayerWinRate * 100:F1} | {q.PlayerWinRateCiLow * 100:F1}–{q.PlayerWinRateCiHigh * 100:F1} | {ipm:F0} | {q.PlayerMeanThinkMs:F0} ms | {q.AvgTurns:F1} |");
                    Console.WriteLine($"{name} {deckName} {ms}ms: {q.PlayerWinRate * 100:F1}% | {ipm:F0} it");
                }
        string outFile = Path.Combine(repoRoot, "Runs", $"strength_vs_time_{DateTime.Now:yyyyMMdd_HHmmss}.md");
        Directory.CreateDirectory(Path.GetDirectoryName(outFile));
        File.WriteAllText(outFile, sb.ToString());
        Console.WriteLine("saved: " + outFile);
    }

    static void Ladder(BenchRunSpec s, AllCards db, string repoRoot)
    {
        int parallel = s.parallelGames > 0 ? s.parallelGames : Environment.ProcessorCount;

        var agents = new List<(string id, AgentSpec spec, Func<int, IGameActionPolicy> mk)>();
        foreach (var spec in s.agents)
            agents.Add((spec.id, spec, seed => AgentFactory.Build(spec, seed)));

        static int SearchKind(AgentSpec sp) =>
            sp.kind == "mcts" && !string.IsNullOrEmpty(sp.networkResource) ? 1 :   // NN-guided MCTS → DiagNeural
            sp.kind == "mcts" ? 2 :                                                // plain MCTS → DiagPlain
            0;                                                                      // no search
        double AvgIters(int kind) =>
            kind == 1 ? (MctsActionPolicy.DiagMovesNeural > 0 ? (double)MctsActionPolicy.DiagIterNeural / MctsActionPolicy.DiagMovesNeural : 0)
          : kind == 2 ? (MctsActionPolicy.DiagMovesPlain > 0 ? (double)MctsActionPolicy.DiagIterPlain / MctsActionPolicy.DiagMovesPlain : 0)
          : 0;

        var presetDecks = new Dictionary<string, AllCards>();
        foreach (var d in s.decks)
            if (d != "RandomDeck" && d != "RandomPreset") presetDecks[d] = RuntimeDeckLoader.LoadPreset(d, db);
        var randomPresetPool = new List<AllCards>();
        if (Array.IndexOf(s.decks, "RandomPreset") >= 0)
            foreach (var p in RuntimeDeckLoader.GetPresetNames()) randomPresetPool.Add(RuntimeDeckLoader.LoadPreset(p, db));
        Func<string, Func<int, AllCards>> factoryFor = cond => {
            if (cond == "RandomDeck") return seed => RuntimeDeckLoader.RandomDeck(db, seed);
            if (cond == "RandomPreset") return seed => randomPresetPool[new Random(seed).Next(randomPresetPool.Count)];
            var d = presetDecks[cond]; return _ => d;
        };

        string ladderDir = Path.Combine(repoRoot, "Runs", "Ladder");
        Directory.CreateDirectory(ladderDir);
        string store = Path.Combine(ladderDir, "matchups.jsonl");
        string rev = new SessionRecord().CardSetRevision;
        var map = LadderStore.Load(store, rev);

        foreach (var deckName in s.decks)
        {
            var mk = factoryFor(deckName);
            for (int i = 0; i < agents.Count; i++)
                for (int j = i + 1; j < agents.Count; j++)
                {
                    string a = agents[i].id, b = agents[j].id;
                    if (!s.overwrite && map.ContainsKey(LadderStore.Key(a, b, deckName))) continue;
                    var cfg = new BatchConfig { Games = s.gamesPerCell, BaseSeed = 7000, MaxActions = s.maxActions, AlternateStart = s.alternateStart, LogEvents = false, MaxParallelGames = parallel };
                    MctsActionPolicy.ResetDiagnostics();
                    var r = BatchRunner.Run(mk, mk, db, agents[i].mk, agents[j].mk,
                        new ModelInfo { ModelId = a, Params = new Dictionary<string, object>() }, new ModelInfo { ModelId = b, Params = new Dictionary<string, object>() },
                        deckName, deckName, cfg);
                    var rec = LadderStore.FromBatch(a, b, deckName, cfg, r, rev);
                    int ka = SearchKind(agents[i].spec), kb = SearchKind(agents[j].spec);
                    rec.aIters = (ka != 0 && ka == kb) ? 0 : AvgIters(ka);
                    rec.bIters = (kb != 0 && ka == kb) ? 0 : AvgIters(kb);
                    LadderStore.Append(store, rec); map[rec.Key] = rec;
                    Console.WriteLine($"{a} vs {b} [{deckName}]: {rec.aWinRate * 100:F1}%  ({rec.games}g  a~{rec.aIters:F0}it b~{rec.bIters:F0}it)");
                }
        }

        var st = LadderStandings.Generate(ladderDir, string.IsNullOrEmpty(s.anchorId) ? "random" : s.anchorId, rev);
        Console.WriteLine("\n=== Standings ===");
        foreach (var m in st.models)
            Console.WriteLine($"  {m.id,-14} Elo {m.elo,4:F0} [{m.ciLow:F0}–{m.ciHigh:F0}]  win {m.winRate * 100:F1}%  ~{m.iters:F0} it");
    }

    static void Duel(BenchRunSpec s, AllCards db, string repoRoot)
    {
        if (s.agents == null || s.agents.Count < 2) { Console.Error.WriteLine("duel needs 2 agents in 'agents'"); return; }
        AgentSpec sa = s.agents[0], sb = s.agents[1];
        int parallel = s.parallelGames > 0 ? s.parallelGames : Environment.ProcessorCount;

        var presetDecks = new Dictionary<string, AllCards>();
        foreach (var d in s.decks)
            if (d != "RandomDeck" && d != "RandomPreset") presetDecks[d] = RuntimeDeckLoader.LoadPreset(d, db);
        var randomPresetPool = new List<AllCards>();
        if (Array.IndexOf(s.decks, "RandomPreset") >= 0)
            foreach (var p in RuntimeDeckLoader.GetPresetNames()) randomPresetPool.Add(RuntimeDeckLoader.LoadPreset(p, db));
        Func<string, Func<int, AllCards>> factoryFor = cond => {
            if (cond == "RandomDeck") return seed => RuntimeDeckLoader.RandomDeck(db, seed);
            if (cond == "RandomPreset") return seed => randomPresetPool[new Random(seed).Next(randomPresetPool.Count)];
            var d = presetDecks[cond]; return _ => d;
        };

        Console.WriteLine($"duel: A={sa.id} vs B={sb.id}, {s.gamesPerCell} games/deck");
        foreach (var deckName in s.decks)
        {
            var mk = factoryFor(deckName);
            var cfg = new BatchConfig { Games = s.gamesPerCell, BaseSeed = 7000, MaxActions = s.maxActions, AlternateStart = s.alternateStart, LogEvents = false, MaxParallelGames = parallel };
            var r = BatchRunner.Run(mk, mk, db,
                sd => AgentFactory.Build(sa, sd), sd => AgentFactory.Build(sb, sd + 1),
                new ModelInfo { ModelId = sa.id, Params = new Dictionary<string, object>() }, new ModelInfo { ModelId = sb.id, Params = new Dictionary<string, object>() },
                deckName, deckName, cfg);
            var q = r.Summary;
            Console.WriteLine($"  {deckName}: A={sa.id} {q.PlayerWinRate * 100:F1}% [{q.PlayerWinRateCiLow * 100:F1}-{q.PlayerWinRateCiHigh * 100:F1}] vs B={sb.id}");
        }
    }

    static void Ceiling(BenchRunSpec s, AllCards db, string repoRoot)
    {
        var net = new NeuralNet(File.ReadAllBytes(Path.Combine(Resources.Root, s.networkResource + ".bytes")));
        int parallel = s.parallelGames > 0 ? s.parallelGames : Environment.ProcessorCount;
        int baseMs = (s.timeBudgetsMs != null && s.timeBudgetsMs.Length > 0) ? s.timeBudgetsMs[0] : 1000;
        int[] mult = { 1, 2, 4, 8, 16 };                   // strong side = K× the reference's time/move

        MctsConfig C(int sd, int ms) => new MctsConfig
        {
            BudgetMode = MctsConfig.Budget.Time,           // compute measured in TIME per move (standard = baseMs)
            TimeBudgetMs = ms,
            ExplorationC = 1.41,
            RolloutPolicy = MctsConfig.Rollout.Random,
            MaxRolloutActions = 40,
            FinalAction = MctsConfig.FinalSelection.MaxVisits,
            Determinize = true,
            KnowsOpponentDeck = true,
            Parallelize = false,
            Seed = sd,
            Network = net,
            PuctC = s.puctC > 0 ? s.puctC : 1.5,
            LeafRolloutMix = s.leafRolloutMix
        };

        var presetDecks = new Dictionary<string, AllCards>();
        foreach (var d in s.decks)
            if (d != "RandomDeck" && d != "RandomPreset") presetDecks[d] = RuntimeDeckLoader.LoadPreset(d, db);
        var randomPresetPool = new List<AllCards>();
        if (Array.IndexOf(s.decks, "RandomPreset") >= 0)
            foreach (var p in RuntimeDeckLoader.GetPresetNames()) randomPresetPool.Add(RuntimeDeckLoader.LoadPreset(p, db));
        Func<string, Func<int, AllCards>> factoryFor = cond => {
            if (cond == "RandomDeck") return seed => RuntimeDeckLoader.RandomDeck(db, seed);
            if (cond == "RandomPreset") return seed => randomPresetPool[new Random(seed).Next(randomPresetPool.Count)];
            var d = presetDecks[cond]; return _ => d;
        };

        var csv = new StringBuilder("deck,ratio,ms_strong,ms_ref,games,win_strong,ci_low,ci_high\n");
        var md = new StringBuilder();
        md.AppendLine($"# Compute-scaling ceiling test ({DateTime.Now:yyyy-MM-dd HH:mm})\n");
        md.AppendLine($"Champion `{s.networkResource}` vs itself. Strong side gets K× the reference's {baseMs} ms/move.");
        md.AppendLine($"Win% of the strong side; **~50% = extra thinking time buys nothing** (tactical depth exhausted).\n");
        md.AppendLine("| deck | ratio | time/move (strong vs ref) | win% strong | Wilson95 | games |");
        md.AppendLine("|---|---|---|---|---|---|");

        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string csvPath = Path.Combine(repoRoot, "Runs", $"ceiling_{stamp}.csv");
        string mdPath = Path.Combine(repoRoot, "Runs", $"ceiling_{stamp}.md");
        Directory.CreateDirectory(Path.GetDirectoryName(csvPath));

        foreach (var deckName in s.decks)
        {
            var mk = factoryFor(deckName);
            foreach (int k in mult)
            {
                int msStrong = baseMs * k;
                Console.WriteLine($"-> {deckName} {k}x ({msStrong} vs {baseMs} ms), {s.gamesPerCell} games...");
                var cfg = new BatchConfig { Games = s.gamesPerCell, BaseSeed = 7000, MaxActions = s.maxActions, AlternateStart = true, LogEvents = false, MaxParallelGames = parallel };
                var r = BatchRunner.Run(mk, mk, db,
                    sd => new MctsActionPolicy(C(sd, msStrong)), sd => new MctsActionPolicy(C(sd + 1, baseMs)),
                    new ModelInfo { ModelId = "strong", Params = new Dictionary<string, object>() },
                    new ModelInfo { ModelId = "ref", Params = new Dictionary<string, object>() },
                    deckName, deckName, cfg);
                var q = r.Summary;
                double score = q.PlayerWins + 0.5 * (q.Draws + q.NonDecisive);
                LadderRating.WilsonCi(score, q.Games, out double lo, out double hi);
                double win = q.Games > 0 ? score / q.Games : 0;

                csv.Append(FormattableString.Invariant($"{deckName},{k},{msStrong},{baseMs},{q.Games},{win:F4},{lo:F4},{hi:F4}\n"));
                md.AppendLine(FormattableString.Invariant($"| {deckName} | {k}× | {msStrong} vs {baseMs} ms | {win * 100:F1} | {lo * 100:F1}–{hi * 100:F1} | {q.Games} |"));
                Console.WriteLine(FormattableString.Invariant($"{deckName} {k}× ({msStrong} vs {baseMs} ms): {win * 100:F1}% [{lo * 100:F1}-{hi * 100:F1}]"));

                File.WriteAllText(csvPath, csv.ToString());   // flush after every cell — survives a crash / power loss
                File.WriteAllText(mdPath, md.ToString());
            }
        }

        Console.WriteLine("saved: " + csvPath);
        Console.WriteLine("saved: " + mdPath);
    }

    static void Batch(BenchRunSpec s, AllCards db, string repoRoot)
    {
        if (s.agents == null || s.agents.Count < 2) { Console.Error.WriteLine("batch needs 2 agents in 'agents'"); return; }
        if (s.decks == null || s.decks.Length < 2) { Console.Error.WriteLine("batch needs 2 decks in 'decks' [player, enemy]"); return; }
        AgentSpec pa = s.agents[0], ea = s.agents[1];
        string pDeckName = s.decks[0], eDeckName = s.decks[1];
        int parallel = s.parallelGames > 0 ? s.parallelGames : Environment.ProcessorCount;

        var presetPool = new List<AllCards>();
        if (pDeckName == "RandomPreset" || eDeckName == "RandomPreset")
            foreach (var p in RuntimeDeckLoader.GetPresetNames()) presetPool.Add(RuntimeDeckLoader.LoadPreset(p, db));

        Func<string, int, Func<int, AllCards>> deckFactory = (cond, offset) => {
            if (cond == "RandomDeck") return seed => RuntimeDeckLoader.RandomDeck(db, seed + offset);
            if (cond == "RandomPreset") return seed => presetPool[new Random(seed + offset).Next(presetPool.Count)];
            var d = RuntimeDeckLoader.LoadPreset(cond, db); return _ => d;
        };
        var pFactory = deckFactory(pDeckName, 0);
        var eFactory = deckFactory(eDeckName, 7919);      // decorrelate enemy random decks

        var cfg = new BatchConfig
        {
            Games = s.games,
            BaseSeed = s.baseSeed == 0 ? 1 : s.baseSeed,
            MaxActions = s.maxActions,
            AlternateStart = s.alternateStart,
            LogEvents = s.logEvents,
            MaxParallelGames = parallel
        };

        Console.WriteLine($"batch: {pa.id} ({pDeckName}) vs {ea.id} ({eDeckName}), {s.games} games");
        var r = BatchRunner.Run(pFactory, eFactory, db,
            sd => AgentFactory.Build(pa, sd), sd => AgentFactory.Build(ea, sd),
            new ModelInfo { ModelId = pa.id, Params = new Dictionary<string, object>() },
            new ModelInfo { ModelId = ea.id, Params = new Dictionary<string, object>() },
            pDeckName, eDeckName, cfg);

        string outRoot = string.IsNullOrEmpty(s.outputRoot) ? Path.Combine(repoRoot, "Runs") : s.outputRoot;
        string folder = SessionWriter.WriteRun(outRoot, r, s.oneFilePerSession);

        var q = r.Summary;
        Console.WriteLine($"done: {pa.id} win {q.PlayerWinRate * 100:F1}% [{q.PlayerWinRateCiLow * 100:F1}-{q.PlayerWinRateCiHigh * 100:F1}]  " +
                          $"P{q.PlayerWins}/E{q.EnemyWins}/D{q.Draws}/ND{q.NonDecisive}  avgTurns {q.AvgTurns:F1}");
        Console.WriteLine("saved: " + folder);
    }
}