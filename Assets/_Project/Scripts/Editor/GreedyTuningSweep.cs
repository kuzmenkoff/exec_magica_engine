using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Round-robin weight sweep for Greedy. Each candidate (a weight vector) plays
/// mirror matches (same deck both sides, alternating start) against every other
/// candidate over the TRAIN decks; candidates are ranked by Wilson-95 lower bound.
/// Editor-only research tool. Writes a markdown table to Runs/.
/// </summary>
public static class GreedyTuningSweep
{
    // ====== SEARCH SPACE (edit here) ======
    // heroHp is the scale anchor -> keep it a single value (1.0).
    static readonly double[] HeroHp = { 1.0 };
    static readonly double[] Attack = { 1.0, 2.0, 3.0 };
    static readonly double[] Hp = { 1.0 };
    static readonly double[] MinionCount = { 1.0, 2.0 };
    static readonly double[] HandCount = { 0.5, 1.0 };
    // => 1*2*2*2*3 = 24 candidates. Round-robin is O(n^2): grow this carefully.

    // ====== EVALUATION SETTINGS ======
    const int GamesPerPairPerDeck = 50;     // N per deck per pairing (alternating start)
    const int BaseSeed = 1000;
    const int MaxActions = 400;
    static readonly string[] TrainDecks = { "MidrangePreset", "TokenPreset" };  // mirror decks
    // Holdout decks (e.g. MidrangePreset/TokenPreset) are validated in a separate run.

    [MenuItem("EXEC_MAGICA/Tuning/Greedy Round-Robin Sweep")]
    public static void Run()
    {
        AllCards db = LoadDatabase();
        if (db == null || db.GetCollectibleCards().Count == 0)
        {
            Debug.LogError("[GreedyTuning] Card database empty.");
            return;
        }

        List<Cand> cands = BuildCandidates();
        int pairs = cands.Count * (cands.Count - 1) / 2;
        long totalGames = (long)pairs * TrainDecks.Length * GamesPerPairPerDeck;

        if (!EditorUtility.DisplayDialog("Greedy round-robin sweep",
                $"Candidates: {cands.Count}\nPairs: {pairs}\nDecks: {TrainDecks.Length}\n" +
                $"~{totalGames} games total.\n\nRuns on the main thread (Editor will freeze; cancelable).",
                "Run", "Cancel"))
            return;

        int[] wins = new int[cands.Count];
        int[] games = new int[cands.Count];

        var sw = System.Diagnostics.Stopwatch.StartNew();
        int pairIdx = 0;

        for (int i = 0; i < cands.Count; i++)
            for (int j = i + 1; j < cands.Count; j++)
            {
                foreach (string deck in TrainDecks)
                {
                    string d = deck;
                    Func<int, AllCards> mk = _ => RuntimeDeckLoader.LoadPreset(d, db); // same deck both sides

                    BatchConfig cfg = new BatchConfig
                    {
                        Games = GamesPerPairPerDeck,
                        BaseSeed = BaseSeed,
                        MaxActions = MaxActions,
                        AlternateStart = true,
                        LogEvents = false,
                        MaxParallelGames = 1
                    };

                    BatchResult r = BatchRunner.Run(
                        mk, mk, db,
                        cands[i].Factory(), cands[j].Factory(),
                        cands[i].Info(), cands[j].Info(),
                        d, d, cfg);

                    wins[i] += r.Summary.PlayerWins;
                    games[i] += r.Summary.Games;
                    wins[j] += r.Summary.EnemyWins;
                    games[j] += r.Summary.Games;
                }

                pairIdx++;
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Greedy sweep", $"pair {pairIdx}/{pairs}", (float)pairIdx / pairs))
                {
                    EditorUtility.ClearProgressBar();
                    Debug.LogWarning("[GreedyTuning] Cancelled.");
                    return;
                }
            }
        sw.Stop();
        EditorUtility.ClearProgressBar();

        // Rank by Wilson lower bound.
        var ranked = Enumerable.Range(0, cands.Count)
            .Select(k =>
            {
                var ci = BatchRunner.WilsonCI(wins[k], games[k]);
                double wr = games[k] > 0 ? (double)wins[k] / games[k] : 0.0;
                return new { c = cands[k], wr, ci, g = games[k] };
            })
            .OrderByDescending(x => x.ci.low)
            .ToList();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"# Greedy round-robin sweep ({DateTime.Now:yyyy-MM-dd HH:mm})");
        sb.AppendLine();
        sb.AppendLine($"Candidates: {cands.Count} · pairs: {pairs} · decks: {string.Join(", ", TrainDecks)} · " +
                      $"{GamesPerPairPerDeck} games/pair/deck · ~{ranked.First().g} games per candidate · " +
                      $"{sw.Elapsed.TotalSeconds:F0}s");
        sb.AppendLine();
        sb.AppendLine("| rank | id | heroHp | attack | hp | minionCount | handCount | win% | Wilson95 | games |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        int rank = 1;
        foreach (var x in ranked)
            sb.AppendLine($"| {rank++} | {x.c.Id} | {F(x.c.HeroHp)} | {F(x.c.Attack)} | {F(x.c.Hp)} | " +
                          $"{F(x.c.MinionCount)} | {F(x.c.HandCount)} | {x.wr * 100:F1} | " +
                          $"{x.ci.low * 100:F1}–{x.ci.high * 100:F1} | {x.g} |");

        string outPath = Path.Combine(
            Application.dataPath, "..", "Runs",
            "greedy_sweep_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".md");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllText(outPath, sb.ToString());

        Debug.Log("[GreedyTuning] Done in " + sw.Elapsed.TotalSeconds.ToString("F0") + "s\n" +
                  sb.ToString() + "\nSaved: " + outPath);
    }

    static List<Cand> BuildCandidates()
    {
        List<Cand> list = new List<Cand>();
        int id = 0;
        foreach (double h in HeroHp)
            foreach (double a in Attack)
                foreach (double hp in Hp)
                    foreach (double mc in MinionCount)
                        foreach (double hc in HandCount)
                            list.Add(new Cand { Id = id++, HeroHp = h, Attack = a, Hp = hp, MinionCount = mc, HandCount = hc });
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
        public int Id;
        public double HeroHp, Attack, Hp, MinionCount, HandCount;

        public Func<int, IGameActionPolicy> Factory() =>
            seed => new GreedyActionPolicy(seed, HeroHp, Attack, Hp, MinionCount, HandCount);

        public ModelInfo Info() => new ModelInfo
        {
            ModelId = "Greedy#" + Id,
            Params = new Dictionary<string, object>
            {
                { "heroHpWeight", HeroHp }, { "attackWeight", Attack }, { "hpWeight", Hp },
                { "minionCountWeight", MinionCount }, { "handCountWeight", HandCount }
            }
        };
    }
}