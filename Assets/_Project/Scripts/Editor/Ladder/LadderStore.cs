using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Append-only store of ladder matchup aggregates (Runs/Ladder/matchups.jsonl). One line per
/// (a, b, cond) cell; the pair is canonicalized a&lt;=b so direction never duplicates. On load
/// the last line for a key wins, so a re-run / overwrite just appends a fresher line.
/// </summary>
public static class LadderStore
{
    /// <summary>One matchup aggregate (a vs b on a deck condition). Counts/rates are from a's POV.</summary>
    public class LadderRecord
    {
        public string a, b, cond;
        public int games, aWins, bWins, draws, nonDecisive;
        public double aWinRate, ciLow, ciHigh;          // score-rate (draws = 0.5) + Wilson CI
        public double avgTurns, avgActions, aThinkMs, bThinkMs;
        public double aIters, bIters;   // avg search iterations/move per side (0 = no search / ambiguous)
        public bool alternateStart;
        public int maxActions, baseSeed;
        public string cardSetRev, createdUtc;

        [JsonIgnore] public string Key => LadderStore.Key(a, b, cond);
    }

    private static readonly JsonSerializerSettings Json =
        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

    /// <summary>Canonical dedup key — pair ordered lexicographically so a|b == b|a.</summary>
    public static string Key(string a, string b, string cond)
    {
        bool ab = string.CompareOrdinal(a, b) <= 0;
        return (ab ? a : b) + "|" + (ab ? b : a) + "|" + cond;
    }

    /// <summary>True if (a, b) is already in canonical order (a &lt;= b).</summary>
    public static bool IsCanonical(string a, string b) => string.CompareOrdinal(a, b) <= 0;

    /// <summary>
    /// Loads matchups, keeping the latest line per key. If <paramref name="cardSetRev"/> is given,
    /// only records of that revision are returned (others are treated as stale).
    /// </summary>
    public static Dictionary<string, LadderRecord> Load(string path, string cardSetRev = null)
    {
        var map = new Dictionary<string, LadderRecord>();
        if (!File.Exists(path)) return map;
        foreach (string line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            LadderRecord r;
            try { r = JsonConvert.DeserializeObject<LadderRecord>(line); } catch { continue; }
            if (r == null || string.IsNullOrEmpty(r.a)) continue;
            if (cardSetRev != null && r.cardSetRev != cardSetRev) continue;
            map[r.Key] = r;   // last line wins
        }
        return map;
    }

    /// <summary>Appends one record as a JSONL line (creates the file/dir if needed).</summary>
    public static void Append(string path, LadderRecord rec)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.AppendAllText(path, JsonConvert.SerializeObject(rec, Formatting.None, Json) + Environment.NewLine);
    }

    /// <summary>
    /// Builds a record from a finished batch. Assumes the batch ran player=a vs enemy=b in
    /// canonical order (a &lt;= b) — the window guarantees this. Draws + stalls are pooled as draws
    /// (counted 0.5 in the score-rate), matching the Elo convention.
    /// </summary>
    public static LadderRecord FromBatch(string a, string b, string cond, BatchConfig cfg,
                                         BatchResult result, string cardSetRev)
    {
        BatchSummary s = result.Summary;
        double aScore = s.PlayerWins + 0.5 * (s.Draws + s.NonDecisive);
        LadderRating.WilsonCi(aScore, s.Games, out double lo, out double hi);

        return new LadderRecord
        {
            a = a,
            b = b,
            cond = cond,
            games = s.Games,
            aWins = s.PlayerWins,
            bWins = s.EnemyWins,
            draws = s.Draws,
            nonDecisive = s.NonDecisive,
            aWinRate = s.Games > 0 ? aScore / s.Games : 0,
            ciLow = lo,
            ciHigh = hi,
            avgTurns = s.AvgTurns,
            avgActions = s.AvgActions,
            aThinkMs = s.PlayerMeanThinkMs,
            bThinkMs = s.EnemyMeanThinkMs,
            alternateStart = cfg.AlternateStart,
            maxActions = cfg.MaxActions,
            baseSeed = cfg.BaseSeed,
            cardSetRev = cardSetRev,
            createdUtc = DateTime.UtcNow.ToString("o")
        };
    }
}
