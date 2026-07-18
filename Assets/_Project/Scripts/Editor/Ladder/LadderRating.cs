using System;

/// <summary>
/// Rating math for the ladder: Bradley-Terry (MM) strengths → Elo (anchored at one model = 0),
/// bootstrap 95% CIs, and Wilson score intervals. Single implementation reused by the ladder
/// window (replaces the copies in the legacy RatingTournament / LadderRun tools).
/// </summary>
public static class LadderRating
{
    /// <summary>Pairwise W/L/D counts for n models; only the upper triangle (i&lt;j) is used.</summary>
    public class PairTable
    {
        public readonly int N;
        public readonly int[,] WinsI, WinsJ, Draws, Games;

        public PairTable(int n)
        {
            N = n;
            WinsI = new int[n, n]; WinsJ = new int[n, n];
            Draws = new int[n, n]; Games = new int[n, n];
        }

        /// <summary>Sets pair (i&lt;j): i-wins, j-wins, draws (draws include stalls, counted 0.5 each).</summary>
        public void Set(int i, int j, int wi, int wj, int dr)
        {
            WinsI[i, j] = wi; WinsJ[i, j] = wj; Draws[i, j] = dr; Games[i, j] = wi + wj + dr;
        }
    }

    /// <summary>Elo per model with bootstrap CIs and pooled score/games.</summary>
    public class EloResult
    {
        public double[] Elo, CiLow, CiHigh, Score;
        public int[] GamesPlayed;
    }

    /// <summary>Fits Bradley-Terry → Elo (model at <paramref name="anchorIndex"/> fixed to 0) with bootstrap CIs.</summary>
    public static EloResult Rate(PairTable t, int anchorIndex, int bootstrapSamples = 300, int seed = 12345)
    {
        int n = t.N;
        var res = new EloResult
        {
            Elo = FitElo(t, anchorIndex),
            CiLow = new double[n],
            CiHigh = new double[n],
            Score = new double[n],
            GamesPlayed = new int[n]
        };

        // Bootstrap CIs: resample each pair's games, refit, take 2.5/97.5 percentiles.
        var rng = new System.Random(seed);
        double[][] boot = new double[n][];
        for (int k = 0; k < n; k++) boot[k] = new double[bootstrapSamples];
        for (int b = 0; b < bootstrapSamples; b++)
        {
            var bt = new PairTable(n);
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    Resample(t.Games[i, j], t.WinsI[i, j], t.WinsJ[i, j], t.Draws[i, j], rng,
                             out int ri, out int rj, out int rd);
                    bt.WinsI[i, j] = ri; bt.WinsJ[i, j] = rj; bt.Draws[i, j] = rd; bt.Games[i, j] = t.Games[i, j];
                }
            double[] e = FitElo(bt, anchorIndex);
            for (int k = 0; k < n; k++) boot[k][b] = e[k];
        }
        for (int k = 0; k < n; k++)
        {
            double[] s = (double[])boot[k].Clone(); Array.Sort(s);
            res.CiLow[k] = s[(int)(0.025 * (bootstrapSamples - 1))];
            res.CiHigh[k] = s[(int)(0.975 * (bootstrapSamples - 1))];
        }

        // Pooled per-model score (wins + 0.5 draws) and games.
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                res.Score[i] += t.WinsI[i, j] + 0.5 * t.Draws[i, j]; res.GamesPlayed[i] += t.Games[i, j];
                res.Score[j] += t.WinsJ[i, j] + 0.5 * t.Draws[i, j]; res.GamesPlayed[j] += t.Games[i, j];
            }
        return res;
    }

    // Bradley-Terry MM fit -> Elo (anchored so model[anchorIndex] = 0).
    private static double[] FitElo(PairTable t, int anchorIndex)
    {
        int n = t.N;
        double[,] score = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = i + 1; j < n; j++)
            {
                score[i, j] = t.WinsI[i, j] + 0.5 * t.Draws[i, j];
                score[j, i] = t.WinsJ[i, j] + 0.5 * t.Draws[i, j];
            }

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
                    if (j != i && t.Games[Math.Min(i, j), Math.Max(i, j)] > 0)
                        denom += t.Games[Math.Min(i, j), Math.Max(i, j)] / (p[i] + p[j]);
                np[i] = (denom > 0 && W[i] > 0) ? W[i] / denom : Math.Max(p[i], 1e-9);
            }
            double logsum = 0; for (int i = 0; i < n; i++) logsum += Math.Log(Math.Max(np[i], 1e-12));
            double gmean = Math.Exp(logsum / n);
            for (int i = 0; i < n; i++) p[i] = np[i] / gmean;
        }

        double scale = 400.0 / Math.Log(10.0);
        double[] elo = new double[n];
        for (int i = 0; i < n; i++) elo[i] = scale * Math.Log(Math.Max(p[i], 1e-12));
        double shift = (anchorIndex >= 0 && anchorIndex < n) ? elo[anchorIndex] : 0;
        for (int i = 0; i < n; i++) elo[i] -= shift;
        return elo;
    }

    private static void Resample(int total, int wi, int wj, int d, System.Random rng,
                                 out int ri, out int rj, out int rd)
    {
        ri = rj = rd = 0; if (total <= 0) return;
        double pi = (double)wi / total, pj = (double)wj / total;
        for (int s = 0; s < total; s++)
        {
            double u = rng.NextDouble();
            if (u < pi) ri++; else if (u < pi + pj) rj++; else rd++;
        }
    }

    /// <summary>Wilson 95% score interval for a win proportion (wins may include 0.5 per draw).</summary>
    public static void WilsonCi(double wins, int games, out double low, out double high)
    {
        if (games <= 0) { low = 0; high = 1; return; }
        const double z = 1.959964, z2 = z * z;
        double p = wins / games;
        double denom = 1 + z2 / games;
        double center = (p + z2 / (2.0 * games)) / denom;
        double half = z * Math.Sqrt(p * (1 - p) / games + z2 / (4.0 * games * games)) / denom;
        low = Math.Max(0, center - half); high = Math.Min(1, center + half);
    }
}
