using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

/// <summary>
/// Derives ladder standings from the matchup source: pools all deck conditions, fits
/// Bradley-Terry → Elo (anchored), and writes Runs/Ladder/standings.json + LADDER.md.
/// Never touches docs/ — the report is a Runs/ artifact the user curates by hand.
/// </summary>
public static class LadderStandings
{
    public class Cell { public double winRate; public int games; }

    public class ModelStanding
    {
        public string id, displayName;
        public double elo, ciLow, ciHigh, winRate, thinkMs, iters;
        public int games;
    }

    public class Standings
    {
        public string generatedUtc, cardSetRev, anchor;
        public List<string> conditions;
        public List<ModelStanding> models;
        public Dictionary<string, Dictionary<string, Cell>> matrix; // matrix[a][b] = a's score-rate vs b (canonical a<=b)
    }

    /// <summary>Loads matchups, computes standings, and writes standings.json + LADDER.md into <paramref name="ladderDir"/>.</summary>
    public static Standings Generate(string ladderDir, string anchorId, string cardSetRev)
    {
        var map = LadderStore.Load(Path.Combine(ladderDir, "matchups.jsonl"), cardSetRev);
        Standings st = Build(map, anchorId, cardSetRev);

        Directory.CreateDirectory(ladderDir);
        File.WriteAllText(Path.Combine(ladderDir, "standings.json"),
            JsonConvert.SerializeObject(st, Formatting.Indented));
        File.WriteAllText(Path.Combine(ladderDir, "LADDER.md"), ToMarkdown(st));
        return st;
    }

    /// <summary>Builds the standings object from a deduped matchup map.</summary>
    public static Standings Build(Dictionary<string, LadderStore.LadderRecord> map, string anchorId, string cardSetRev)
    {
        // Index models in first-seen order.
        var ids = new List<string>();
        var idx = new Dictionary<string, int>();
        foreach (var r in map.Values) { Index(ids, idx, r.a); Index(ids, idx, r.b); }
        int n = ids.Count;

        var pt = new LadderRating.PairTable(n);
        double[] thinkSum = new double[n]; int[] thinkGames = new int[n];
        double[] iterSum = new double[n]; int[] iterGames = new int[n];
        var mScore = new Dictionary<string, double>(); var mGames = new Dictionary<string, int>();
        var conds = new SortedSet<string>();

        foreach (var r in map.Values)
        {
            int i = idx[r.a], j = idx[r.b], dr = r.draws + r.nonDecisive;
            if (i < j) { pt.WinsI[i, j] += r.aWins; pt.WinsJ[i, j] += r.bWins; pt.Draws[i, j] += dr; pt.Games[i, j] += r.games; }
            else if (i > j) { pt.WinsI[j, i] += r.bWins; pt.WinsJ[j, i] += r.aWins; pt.Draws[j, i] += dr; pt.Games[j, i] += r.games; }

            thinkSum[i] += r.aThinkMs * r.games; thinkGames[i] += r.games;
            thinkSum[j] += r.bThinkMs * r.games; thinkGames[j] += r.games;

            if (r.aIters > 0) { iterSum[i] += r.aIters * r.games; iterGames[i] += r.games; }
            if (r.bIters > 0) { iterSum[j] += r.bIters * r.games; iterGames[j] += r.games; }

            string k = r.a + "|" + r.b;                       // record is already canonical a<=b by id string
            mScore[k] = mScore.GetValueOrDefault(k) + r.aWins + 0.5 * dr;
            mGames[k] = mGames.GetValueOrDefault(k) + r.games;
            conds.Add(r.cond);
        }

        int anchorIdx = idx.TryGetValue(anchorId, out int ai) ? ai : 0;
        LadderRating.EloResult e = n > 0 ? LadderRating.Rate(pt, anchorIdx) : null;

        var st = new Standings
        {
            generatedUtc = DateTime.UtcNow.ToString("o"),
            cardSetRev = cardSetRev,
            anchor = anchorId,
            conditions = conds.ToList(),
            models = new List<ModelStanding>(),
            matrix = new Dictionary<string, Dictionary<string, Cell>>()
        };

        if (e != null)
            foreach (int k in Enumerable.Range(0, n).OrderByDescending(x => e.Elo[x]))
                st.models.Add(new ModelStanding
                {
                    id = ids[k],
                    displayName = DisplayName(ids[k]),
                    elo = Math.Round(e.Elo[k]),
                    ciLow = Math.Round(e.CiLow[k]),
                    ciHigh = Math.Round(e.CiHigh[k]),
                    winRate = e.GamesPlayed[k] > 0 ? e.Score[k] / e.GamesPlayed[k] : 0,
                    games = e.GamesPlayed[k],
                    thinkMs = thinkGames[k] > 0 ? thinkSum[k] / thinkGames[k] : 0,
                    iters = iterGames[k] > 0 ? iterSum[k] / iterGames[k] : 0,
                });

        foreach (var kv in mScore)
        {
            string[] p = kv.Key.Split('|');
            int g = mGames[kv.Key];
            if (!st.matrix.TryGetValue(p[0], out var row)) { row = new Dictionary<string, Cell>(); st.matrix[p[0]] = row; }
            row[p[1]] = new Cell { winRate = g > 0 ? kv.Value / g : 0, games = g };
        }
        return st;
    }

    private static void Index(List<string> ids, Dictionary<string, int> idx, string id)
    { if (!idx.ContainsKey(id)) { idx[id] = ids.Count; ids.Add(id); } }

    private static string DisplayName(string id)
    {
        var m = OpponentModelCatalog.GetById(id);
        return m != null && !string.IsNullOrEmpty(m.DisplayName) ? m.DisplayName : id;
    }

    // a's score-rate vs b, looking up the canonical cell and inverting if needed.
    private static Cell Lookup(Standings st, string a, string b)
    {
        if (st.matrix.TryGetValue(a, out var ra) && ra.TryGetValue(b, out var c)) return c;
        if (st.matrix.TryGetValue(b, out var rb) && rb.TryGetValue(a, out var c2))
            return new Cell { winRate = 1 - c2.winRate, games = c2.games };
        return null;
    }

    private static string Think(double ms) => ms < 1 ? "<1 ms"
        : (ms < 1000 ? FormattableString.Invariant($"{ms:F0} ms")
                     : FormattableString.Invariant($"{ms / 1000:F2} s"));

    /// <summary>Human-readable report (Runs/Ladder/LADDER.md).</summary>
    public static string ToMarkdown(Standings st)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Rating ladder ({st.generatedUtc})");
        sb.AppendLine();
        sb.AppendLine($"Anchor `{st.anchor}` = 0 · card-set `{st.cardSetRev}` · conditions: {string.Join(", ", st.conditions)}");
        sb.AppendLine();
        sb.AppendLine(FormattableString.Invariant(
            $"Environment: {Environment.ProcessorCount} logical cores · {System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim()} · {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription} · server GC {(System.Runtime.GCSettings.IsServerGC ? "on" : "off")}"));
        sb.AppendLine();
        sb.AppendLine("| rank | model | id | Elo | 95% CI | win% | iters/move | think/move | games |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
        int rank = 1;
        foreach (var m in st.models)
            sb.AppendLine(FormattableString.Invariant($"| {rank++} | {m.displayName} | {m.id} | {m.elo:F0} | {m.ciLow:F0}–{m.ciHigh:F0} | {m.winRate * 100:F1} | {(m.iters > 0 ? m.iters.ToString("F0") : "—")} | {Think(m.thinkMs)} | {m.games} |"));

        sb.AppendLine();
        sb.AppendLine("## Matchup matrix (row score% vs col)");
        var order = st.models.Select(m => m.id).ToList();
        sb.Append("| vs |"); foreach (var c in order) sb.Append($" {c} |"); sb.AppendLine();
        sb.Append("|---|"); foreach (var _ in order) sb.Append("---|"); sb.AppendLine();
        foreach (var rowId in order)
        {
            sb.Append($"| {rowId} |");
            foreach (var colId in order)
            {
                if (rowId == colId) { sb.Append(" — |"); continue; }
                Cell c = Lookup(st, rowId, colId);
                sb.Append(c != null ? FormattableString.Invariant($" {c.winRate * 100:F0} |") : " · |");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
