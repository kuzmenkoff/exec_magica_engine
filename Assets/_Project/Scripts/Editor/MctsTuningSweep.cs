using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MCTS Phase A: sweep search params (explorationC / rollout / maxRolloutActions)
/// at a FIXED modest iteration budget, each candidate vs the tuned Greedy reference.
/// Ranks by Wilson-95 lower bound of MCTS win rate. Budget itself is tuned in Phase B.
/// Editor-only; writes a markdown table to Runs/.
/// </summary>
public static class MctsTuningSweep
{
    // ====== SEARCH SPACE (Phase A) ======
    static readonly double[] ExplorationCs = { 0.7, 1.41, 2.0 };
    static readonly MctsConfig.Rollout[] Rollouts = { MctsConfig.Rollout.Random, MctsConfig.Rollout.Greedy };
    static readonly int[] MaxRolloutActionsSet = { 20, 40 };
    const MctsConfig.FinalSelection FinalAct = MctsConfig.FinalSelection.MaxVisits; // fixed (standard)
    // 3 * 2 * 2 = 12 candidates

    // ====== FIXED FOR PHASE A ======
    const int FixedIterations = 400;   // budget tuned later (Phase B)
    const int GamesPerDeck = 30;    // MCTS vs Greedy per train deck (alternating start)
    const int BaseSeed = 1000;
    const int MaxActions = 400;
    static readonly string[] TrainDecks = { "AggroPreset", "ControlPreset" };

    [MenuItem("EXEC_MAGICA/Tuning/MCTS Phase A (vs tuned Greedy)")]
    public static void Run()
    {
        AllCards db = LoadDatabase();
        if (db == null || db.GetCollectibleCards().Count == 0) { Debug.LogError("[MctsTuning] DB empty."); return; }

        List<Cand> cands = BuildCandidates();
        long totalGames = (long)cands.Count * TrainDecks.Length * GamesPerDeck;

        if (!EditorUtility.DisplayDialog("MCTS Phase A",
                $"Candidates: {cands.Count}\nBudget: {FixedIterations} iters/move\nOpponent: tuned Greedy\n" +
                $"~{totalGames} MCTS games — SLOW (minutes+).\n\nMain thread; cancelable.",
                "Run", "Cancel"))
            return;

        Func<int, IGameActionPolicy> greedy = seed => new GreedyActionPolicy(seed); // frozen tuned defaults
        ModelInfo greedyInfo = new ModelInfo { ModelId = "Greedy", Params = new Dictionary<string, object>() };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var rows = new List<Row>();
        bool cancelled = false;

        for (int k = 0; k < cands.Count && !cancelled; k++)
        {
            int wins = 0, games = 0;

            foreach (string deck in TrainDecks)
            {
                if (cancelled) break;
                string d = deck;
                Func<int, AllCards> mk = _ => RuntimeDeckLoader.LoadPreset(d, db);

                BatchConfig cfg = new BatchConfig
                {
                    Games = GamesPerDeck,
                    BaseSeed = BaseSeed,
                    MaxActions = MaxActions,
                    AlternateStart = true,
                    LogEvents = false,
                    MaxParallelGames = 1
                };

                int kk = k;
                BatchResult r = BatchRunner.Run(
                    mk, mk, db,
                    cands[k].Factory(),  // MCTS = Player
                    greedy,              // Greedy = Enemy
                    cands[k].Info(), greedyInfo,
                    d, d, cfg,
                    onProgress: (g, tot) =>
                    {
                        bool cancel = EditorUtility.DisplayCancelableProgressBar(
                            "MCTS Phase A",
                            $"cand {kk + 1}/{cands.Count} [{cands[kk].Label}] · {d} {g}/{tot}",
                            (kk + (float)g / tot / TrainDecks.Length) / cands.Count);
                        if (cancel) cancelled = true;
                        return !cancel;
                    });

                wins += r.Summary.PlayerWins;
                games += r.Summary.Games;
            }

            if (cancelled) break;
            var ci = BatchRunner.WilsonCI(wins, games);
            rows.Add(new Row { c = cands[k], games = games, wr = games > 0 ? (double)wins / games : 0, ci = ci });
        }

        sw.Stop();
        EditorUtility.ClearProgressBar();
        if (cancelled) { Debug.LogWarning("[MctsTuning] Cancelled."); return; }

        var ranked = rows.OrderByDescending(x => x.ci.low).ToList();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"# MCTS Phase A — search params vs tuned Greedy ({DateTime.Now:yyyy-MM-dd HH:mm})");
        sb.AppendLine();
        sb.AppendLine($"Budget: {FixedIterations} iters/move · decks: {string.Join(", ", TrainDecks)} · " +
                      $"{GamesPerDeck} games/deck · {ranked[0].games} games/candidate · {sw.Elapsed.TotalSeconds:F0}s");
        sb.AppendLine();
        sb.AppendLine("| rank | explorationC | rollout | maxRolloutActions | finalSel | win% vs Greedy | Wilson95 | games |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        int rank = 1;
        foreach (var x in ranked)
            sb.AppendLine($"| {rank++} | {F(x.c.ExplorationC)} | {x.c.Rollout} | {x.c.MaxRolloutActions} | {FinalAct} | " +
                          $"{x.wr * 100:F1} | {x.ci.low * 100:F1}–{x.ci.high * 100:F1} | {x.games} |");

        string outPath = Path.Combine(Application.dataPath, "..", "Runs",
            "mcts_phaseA_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".md");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log("[MctsTuning] Phase A done in " + sw.Elapsed.TotalSeconds.ToString("F0") + "s\n" + sb + "\nSaved: " + outPath);
    }

    static List<Cand> BuildCandidates()
    {
        var list = new List<Cand>();
        foreach (double c in ExplorationCs)
            foreach (var ro in Rollouts)
                foreach (int mr in MaxRolloutActionsSet)
                    list.Add(new Cand { ExplorationC = c, Rollout = ro, MaxRolloutActions = mr });
        return list;
    }

    static AllCards LoadDatabase()
    {
        AllCards db = new AllCards();
        foreach (TextAsset ta in Resources.LoadAll<TextAsset>("CardsInfo/AllCards"))
        {
            AllCards set = CardJsonLoader.LoadAllCards(ta.text);
            if (set != null) db.AddCardSet(set);
        }
        return db;
    }

    static string F(double d) => d.ToString("0.##", CultureInfo.InvariantCulture);

    class Cand
    {
        public double ExplorationC;
        public MctsConfig.Rollout Rollout;
        public int MaxRolloutActions;

        public string Label => $"C={ExplorationC} {Rollout} mr={MaxRolloutActions}";

        MctsConfig Build(int seed) => new MctsConfig
        {
            BudgetMode = MctsConfig.Budget.Iterations,
            Iterations = FixedIterations,
            ExplorationC = ExplorationC,
            RolloutPolicy = Rollout,
            MaxRolloutActions = MaxRolloutActions,
            FinalAction = FinalAct,
            Determinize = true,
            KnowsOpponentDeck = true,
            Parallelize = false,
            Seed = seed
        };

        public Func<int, IGameActionPolicy> Factory() => seed => new MctsActionPolicy(Build(seed));

        public ModelInfo Info() => new ModelInfo
        {
            ModelId = "MCTS",
            Params = new Dictionary<string, object>
            {
                { "iterations", FixedIterations },
                { "explorationC", ExplorationC },
                { "rollout", Rollout.ToString() },
                { "maxRolloutActions", MaxRolloutActions },
                { "finalSelection", FinalAct.ToString() }
            }
        };
    }

    struct Row { public Cand c; public int games; public double wr; public (double low, double high) ci; }
}
