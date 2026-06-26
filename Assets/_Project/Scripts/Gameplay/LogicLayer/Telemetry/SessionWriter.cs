using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;

public class RunSummary
{
    public int SchemaVersion = 1;
    public string RunId;
    public string StartedAtUtc;
    public BatchConfig Config;
    public Dictionary<string, ModelInfo> Players;
    public Dictionary<string, string> Decks;
    public BatchSummary Summary;
}

public class RunIndexEntry
{
    public string RunId;
    public string StartedAtUtc;
    public string PlayerModel;
    public string EnemyModel;
    public string PlayerDeck;
    public string EnemyDeck;
    public string Matchup;
    public int Games;
    public int PlayerWins;
    public int EnemyWins;
    public int Draws;
    public int NonDecisive;
    public double PlayerWinRate;
    public double CiLow;
    public double CiHigh;
    public int SchemaVersion = 1;
    public string Folder;
}

public static class SessionWriter
{
    private static readonly JsonSerializerSettings Settings =
        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

    /// <summary>Writes one run folder (summary.json + sessions.jsonl) and appends index.jsonl.
    /// Returns the run folder path.</summary>
    public static string WriteRun(string outputRoot, BatchResult result, bool oneFilePerSession = false)
    {
        string pModel = result.PlayerModel?.ModelId ?? "P";
        string eModel = result.EnemyModel?.ModelId ?? "E";

        string folderName =
            $"{SafeTimestamp(result.StartedAtUtc)}__{Safe(pModel)}-vs-{Safe(eModel)}" +
            $"__{Safe(result.PlayerDeck)}-vs-{Safe(result.EnemyDeck)}";

        string folder = Path.Combine(outputRoot, folderName);
        Directory.CreateDirectory(folder);

        RunSummary runSummary = new RunSummary
        {
            RunId = result.RunId,
            StartedAtUtc = result.StartedAtUtc,
            Config = result.Config,
            Players = new Dictionary<string, ModelInfo> { { "Player", result.PlayerModel }, { "Enemy", result.EnemyModel } },
            Decks = new Dictionary<string, string> { { "Player", result.PlayerDeck }, { "Enemy", result.EnemyDeck } },
            Summary = result.Summary
        };
        File.WriteAllText(Path.Combine(folder, "summary.json"),
            JsonConvert.SerializeObject(runSummary, Formatting.Indented, Settings));

        if (oneFilePerSession)
        {
            string sub = Path.Combine(folder, "sessions");
            Directory.CreateDirectory(sub);
            for (int i = 0; i < result.Sessions.Count; i++)
                File.WriteAllText(Path.Combine(sub, $"session_{i + 1:D5}.json"),
                    JsonConvert.SerializeObject(result.Sessions[i], Formatting.Indented, Settings));
        }
        else
        {
            StringBuilder sb = new StringBuilder();
            foreach (SessionRecord s in result.Sessions)
                sb.AppendLine(JsonConvert.SerializeObject(s, Formatting.None, Settings));
            File.WriteAllText(Path.Combine(folder, "sessions.jsonl"), sb.ToString());
        }

        RunIndexEntry idx = new RunIndexEntry
        {
            RunId = result.RunId,
            StartedAtUtc = result.StartedAtUtc,
            PlayerModel = pModel,
            EnemyModel = eModel,
            PlayerDeck = result.PlayerDeck,
            EnemyDeck = result.EnemyDeck,
            Matchup = $"{pModel}-vs-{eModel}",
            Games = result.Summary.Games,
            PlayerWins = result.Summary.PlayerWins,
            EnemyWins = result.Summary.EnemyWins,
            Draws = result.Summary.Draws,
            NonDecisive = result.Summary.NonDecisive,
            PlayerWinRate = result.Summary.PlayerWinRate,
            CiLow = result.Summary.PlayerWinRateCiLow,
            CiHigh = result.Summary.PlayerWinRateCiHigh,
            Folder = folderName
        };
        Directory.CreateDirectory(outputRoot);
        File.AppendAllText(Path.Combine(outputRoot, "index.jsonl"),
            JsonConvert.SerializeObject(idx, Formatting.None, Settings) + Environment.NewLine);

        return folder;
    }

    private static string Safe(string s)
    {
        if (string.IsNullOrEmpty(s)) return "_";
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '-');
        return s.Replace(' ', '-');
    }

    private static string SafeTimestamp(string iso)
    {
        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime dt))
            return dt.ToString("yyyy-MM-ddTHH-mm-ssZ");
        return (iso ?? "").Replace(':', '-');
    }
}
