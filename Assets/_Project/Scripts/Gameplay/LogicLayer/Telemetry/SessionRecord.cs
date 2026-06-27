using System.Collections.Generic;

/// <summary>AI model identity recorded with a session: a model id plus its parameters.</summary>
public class ModelInfo
{
    public string ModelId;
    public Dictionary<string, object> Params = new Dictionary<string, object>();
}

/// <summary>Final outcome of one game: winner, end reason, length and duration.</summary>
public class OutcomeRecord
{
    /// <summary>"Player", "Enemy", or null for a draw / no winner.</summary>
    public string Winner;
    /// <summary><see cref="GameEndReason"/> name.</summary>
    public string Reason;
    public int TotalTurns;
    public int TotalActions;
    public double DurationMs;
}

/// <summary>Per-side aggregate metrics for one game (mana efficiency, think time).</summary>
public class PerSideMetrics
{
    public double AvgManaEfficiency;
    public double MeanThinkMs;
    public double MedianThinkMs;
}

/// <summary>
/// Full telemetry record for a single game: identity, models, decks, outcome, per-side metrics,
/// per-card impact and (optionally) the raw event stream. Serialized to JSON/JSONL.
/// </summary>
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

    /// <summary>Cumulative per-card impact, keyed by CardId.</summary>
    public Dictionary<int, double> CardImpact = new Dictionary<int, double>();
    /// <summary>Raw event stream — populated only when event logging is enabled.</summary>
    public List<GameEvent> Events;
}
