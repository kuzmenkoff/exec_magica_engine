using System.Collections.Generic;

public class ModelInfo
{
    public string ModelId;
    public Dictionary<string, object> Params = new Dictionary<string, object>();
}

public class OutcomeRecord
{
    public string Winner;        // "Player" / "Enemy" / null
    public string Reason;        // GameEndReason name
    public int TotalTurns;
    public int TotalActions;
    public double DurationMs;
}

public class PerSideMetrics
{
    public double AvgManaEfficiency;
    public double MeanThinkMs;
    public double MedianThinkMs;
}

public class SessionRecord
{
    public int SchemaVersion = 1;
    public string SessionId;
    public int Seed;
    public string StartedAtUtc;
    public string CardSetRevision = "2026-06-13";

    public Dictionary<string, ModelInfo> Players = new Dictionary<string, ModelInfo>();
    public Dictionary<string, string> Decks = new Dictionary<string, string>();
    public string StartingSide;

    public OutcomeRecord Outcome = new OutcomeRecord();
    public Dictionary<string, PerSideMetrics> PerSideMetrics = new Dictionary<string, PerSideMetrics>();

    public Dictionary<int, double> CardImpact = new Dictionary<int, double>();

    public List<GameEvent> Events;   // written only when logEvents is on
}
