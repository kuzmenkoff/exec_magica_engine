using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Final rating ladder over the FROZEN family representatives (one config per algorithm,
/// NOT the game difficulty presets). Round-robin, Bradley-Terry -> Elo anchored at
/// Random=0 with bootstrap CIs, plus per-agent mean think-time and a >2 s eligibility flag.
/// Fatigue-on (engine-level). Writes ladder + matchup matrix to Runs/. Does NOT touch assets.
/// </summary>
public static class LadderRun
{
    static readonly string[] Decks = { "AggroPreset", "ControlPreset", "MidrangePreset", "TokenPreset" };
    const int GamesPerDeck = 40;        // 4 decks => 160 games/pair
    const int BaseSeed = 9000;
    const int MaxActions = 400;
    const int BootstrapSamples = 300;
    const double CapMs = 2000.0;        // ladder eligibility: mean think-time per move
    const int AnchorIndex = 0;          // Random fixed to Elo 0

    class Agent
    {
        public string Name, Id;
        public Func<int, IGameActionPolicy> Factory;
        public ModelInfo Info;
    }

    static MctsConfig Cfg(int iters, MctsConfig.Rollout rollout, int seed) => new MctsConfig
    {
        BudgetMode = MctsConfig.Budget.Iterations,
        Iterations = iters,
        ExplorationC = 1.41,
        RolloutPolicy = rollout,
        MaxRolloutActions = 40,
        FinalAction = MctsConfig.FinalSelection.MaxVisits,
        Determinize = true,
        KnowsOpponentDeck = true,
        Parallelize = false,
        Seed = seed
    };

    static Dictionary<string, object> P(params (string, object)[] kv)
    { var d = new Dictionary<string, object>(); foreach (var (k, v) in kv) d[k] = v; return d; }

    static List<Agent> Roster() => new List<Agent>
    {
        new Agent { Name = "Random", Id = "random",
            Factory = s => new RandomActionPolicy(s),
            Info = new ModelInfo { ModelId = "Random", Params = P() } },
        new Agent { Name = "Greedy", Id = "greedy",
            Factory = s => new GreedyActionPolicy(s),
            Info = new ModelInfo { ModelId = "Greedy", Params = P() } },
        new Agent { Name = "MCTS · Greedy rollout", Id = "mcts-greedy",
            Factory = s => new MctsActionPolicy(Cfg(350, MctsConfig.Rollout.Greedy, s)),
            Info = new ModelInfo { ModelId = "MCTS-GreedyRollout", Params = P(("iterations", 350), ("rollout", "Greedy")) } },
        new Agent { Name = "MCTS · Random rollout", Id = "mcts-random",
            Factory = s => new MctsActionPolicy(Cfg(3200, MctsConfig.Rollout.Random, s)),
            Info = new ModelInfo { ModelId = "MCTS-RandomRollout", Params = P(("iterations", 3200), ("rollout", "Random")) } },
    };

    [MenuItem("EXEC_MAGICA/Tuning/Ladder — frozen families")]
    public static void Run()
    {
        var agents = Roster();
        int n = agents.Count;

        AllCards db = LoadDatabase();
        if (db == null || db.GetCollectibleCards().Count == 0) { Debug.LogError("[Ladder] DB empty."); return; }

        int pairs = n * (n - 1) / 2;
        long totalGames = (long)pairs * Decks.Length * GamesPerDeck;
        if (!EditorUtility.DisplayDialog("Ladder — frozen families",
                $"Agents: {n}\nPairs: {pairs}\n~{totalGames} games · decks {string.Join(", ", Decks)} · fatigue-on\n" +
                $"Elo anchored Random=0 · cap {CapMs / 1000:F0} s/move flagged.\nMain thread; cancelable.",
                "Run", "Cancel"))
            return;

        int[,] wI = new int[n, n], wJ = new int[n, n], dr = new int[n, n], gm = new int[n, n];
        double[] thinkSum = new double[n]; int[] thinkCnt = new int[n];

        var sw = System.Diagnostics.Stopwatch.StartNew();
        bool cancelled = false; int pairIdx = 0;

        for (int i = 0; i < n && !cancelled; i++)
            for (int j = i + 1; j < n && !cancelled; j++)
            {
                int iw = 0, jw = 0, dd = 0, g = 0;
                foreach (string deck in Decks)
                {
                    if (cancelled) break;
                    string d = deck;
                    Func<int, AllCards> mk = _ => RuntimeDeckLoader.LoadPreset(d, db);
                    int ii = i, jj = j, pi = pairIdx;

                    BatchConfig cfg = new BatchConfig
                    {
                        Games = GamesPerDeck,
                        BaseSeed = BaseSeed,
                        MaxActions = MaxActions,
                        AlternateStart = true,
                        LogEvents = false,
                        MaxParallelGames = 1
                    };

                    BatchResult r = BatchRunner.Run(
                        mk, mk, db,
                        seed => agents[ii].Factory(seed), seed => agents[jj].Factory(seed),
                        agents[ii].Info, agents[jj].Info, d, d, cfg,
                        onProgress: (gg, tot) =>
                        {
                            bool c = EditorUtility.DisplayCancelableProgressBar("Ladder — frozen families",
                                $"{agents[ii].Name} vs {agents[jj].Name} · {d} {gg}/{tot} (pair {pi + 1}/{pairs})",
                                (pi + (float)gg / tot / Decks.Length) / pairs);
                            if (c) cancelled = true;
                            return !c;
                        });

                    iw += r.Summary.PlayerWins; jw += r.Summary.EnemyWins;
                    dd += r.Summary.Draws + r.Summary.NonDecisive; g += r.Summary.Games;
                    thinkSum[i] += r.Summary.PlayerMeanThinkMs; thinkCnt[i]++;
                    thinkSum[j] += r.Summary.EnemyMeanThinkMs; thinkCnt[j]++;
                }
                wI[i, j] = iw; wJ[i, j] = jw; dr[i, j] = dd; gm[i, j] = g;
                pairIdx++;
            }

        sw.Stop(); EditorUtility.ClearProgressBar();
        if (cancelled) { Debug.LogWarning("[Ladder] Cancelled — nothing written."); return; }

        double[] elo = FitElo(wI, wJ, dr, gm, n);

        // bootstrap CIs
        var rng = new System.Random(BaseSeed);
        double[][] boot = new double[n][]; for (int k = 0; k < n; k++) boot[k] = new double[BootstrapSamples];
        for (int b = 0; b < BootstrapSamples; b++)
        {
            int[,] bI = new int[n, n], bJ = new int[n, n], bD = new int[n, n];
            for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++)
                    Resample(gm[i, j], wI[i, j], wJ[i, j], dr[i, j], rng, out bI[i, j], out bJ[i, j], out bD[i, j]);
            double[] e = FitElo(bI, bJ, bD, gm, n);
            for (int k = 0; k < n; k++) boot[k][b] = e[k];
        }
        double[] lo = new double[n], hi = new double[n];
        for (int k = 0; k < n; k++)
        {
            var s = (double[])boot[k].Clone(); Array.Sort(s);
            lo[k] = s[(int)(0.025 * (BootstrapSamples - 1))]; hi[k] = s[(int)(0.975 * (BootstrapSamples - 1))];
        }

        double[] mScore = new double[n]; int[] mGames = new int[n];
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++)
            {
                mScore[i] += wI[i, j] + 0.5 * dr[i, j]; mGames[i] += gm[i, j];
                mScore[j] += wJ[i, j] + 0.5 * dr[i, j]; mGames[j] += gm[i, j];
            }

        var order = Enumerable.Range(0, n).OrderByDescending(k => elo[k]).ToList();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"# Rating ladder — frozen families ({DateTime.Now:yyyy-MM-dd HH:mm})");
        sb.AppendLine();
        sb.AppendLine($"Decks {string.Join(", ", Decks)} · {GamesPerDeck} games/pair/deck · fatigue-on · " +
                      $"anchor Random=0 · bootstrap {BootstrapSamples} · {sw.Elapsed.TotalMinutes:F0} min");
        sb.AppendLine();
        sb.AppendLine("| rank | agent | Elo | 95% CI | win% | think/move | games |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        int rank = 1;
        foreach (int k in order)
        {
            double wr = mGames[k] > 0 ? mScore[k] / mGames[k] : 0;
            double tm = thinkCnt[k] > 0 ? thinkSum[k] / thinkCnt[k] : 0;
            string tmStr = tm < 1 ? "<1 ms" : (tm < 1000 ? $"{tm:F0} ms" : $"{tm / 1000:F2} s");
            string flag = tm > CapMs ? " ⚠ >cap" : "";
            sb.AppendLine($"| {rank++} | {agents[k].Name} | {elo[k]:F0} | {lo[k]:F0}–{hi[k]:F0} | " +
                          $"{wr * 100:F1} | {tmStr}{flag} | {mGames[k]} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Matchup matrix (row win% vs col)");
        sb.Append("| vs |"); foreach (int j in order) sb.Append($" {agents[j].Id} |"); sb.AppendLine();
        sb.Append("|---|"); foreach (var _ in order) sb.Append("---|"); sb.AppendLine();
        foreach (int i in order)
        {
            sb.Append($"| {agents[i].Id} |");
            foreach (int j in order)
            {
                if (i == j) { sb.Append(" — |"); continue; }
                int a = Math.Min(i, j), bb = Math.Max(i, j);
                double si = (i < j) ? wI[a, bb] + 0.5 * dr[a, bb] : wJ[a, bb] + 0.5 * dr[a, bb];
                double pct = gm[a, bb] > 0 ? si / gm[a, bb] * 100 : 0;
                sb.Append($" {pct:F0} |");
            }
            sb.AppendLine();
        }

        string outPath = Path.Combine(Application.dataPath, "..", "Runs",
            "ladder_families_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".md");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllText(outPath, sb.ToString());
        Debug.Log("[Ladder] Done in " + sw.Elapsed.TotalMinutes.ToString("F0") + " min\n" + sb + "\nSaved: " + outPath);
    }

    // Bradley-Terry MM fit -> Elo (anchored so AnchorIndex = 0).
    static double[] FitElo(int[,] wI, int[,] wJ, int[,] dr, int[,] gm, int n)
    {
        double[,] score = new double[n, n];
        for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++)
            { score[i, j] = wI[i, j] + 0.5 * dr[i, j]; score[j, i] = wJ[i, j] + 0.5 * dr[i, j]; }

        double[] p = new double[n]; for (int i = 0; i < n; i++) p[i] = 1.0;
        double[] W = new double[n];
        for (int i = 0; i < n; i++) { double w = 0; for (int j = 0; j < n; j++) if (j != i) w += score[i, j]; W[i] = w; }

        for (int it = 0; it < 200; it++)
        {
            double[] np = new double[n];
            for (int i = 0; i < n; i++)
            {
                double denom = 0;
                for (int j = 0; j < n; j++)
                    if (j != i && gm[Math.Min(i, j), Math.Max(i, j)] > 0)
                        denom += gm[Math.Min(i, j), Math.Max(i, j)] / (p[i] + p[j]);
                np[i] = (denom > 0 && W[i] > 0) ? W[i] / denom : Math.Max(p[i], 1e-9);
            }
            double logsum = 0; for (int i = 0; i < n; i++) logsum += Math.Log(Math.Max(np[i], 1e-12));
            double gmean = Math.Exp(logsum / n);
            for (int i = 0; i < n; i++) p[i] = np[i] / gmean;
        }

        double scale = 400.0 / Math.Log(10.0);
        double[] elo = new double[n];
        for (int i = 0; i < n; i++) elo[i] = scale * Math.Log(Math.Max(p[i], 1e-12));
        double shift = elo[AnchorIndex];
        for (int i = 0; i < n; i++) elo[i] -= shift;
        return elo;
    }

    static void Resample(int total, int wi, int wj, int d, System.Random rng, out int ri, out int rj, out int rd)
    {
        ri = rj = rd = 0; if (total <= 0) return;
        double pi = (double)wi / total, pj = (double)wj / total;
        for (int s = 0; s < total; s++)
        {
            double u = rng.NextDouble();
            if (u < pi) ri++; else if (u < pi + pj) rj++; else rd++;
        }
    }

    static AllCards LoadDatabase()
    {
        AllCards db = new AllCards();
        foreach (TextAsset ta in Resources.LoadAll<TextAsset>("CardsInfo/AllCards"))
        { AllCards set = CardJsonLoader.LoadAllCards(ta.text); if (set != null) db.AddCardSet(set); }
        return db;
    }
}
