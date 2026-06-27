/// <summary>
/// Why a match ended — used to classify and aggregate outcomes in telemetry.
/// </summary>
public enum GameEndReason
{
    /// <summary>A hero reached 0 HP from card or attack damage.</summary>
    HeroLethal,

    /// <summary>A hero died to fatigue — escalating self-damage from drawing on an empty deck.</summary>
    Fatigue,

    /// <summary>The playout hit the action cap or stalled; no winner.</summary>
    MaxActionsReached,

    /// <summary>The game ended with no winner.</summary>
    Draw
}
