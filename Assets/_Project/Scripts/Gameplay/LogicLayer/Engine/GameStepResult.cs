using System.Collections.Generic;

/// <summary>
/// Result of applying one GameAction to GameState.
/// </summary>
public class GameStepResult
{
    /// <summary>Whether the action was applied successfully.</summary>
    public bool Success;
    /// <summary>Failure reason when <see cref="Success"/> is false; null on success.</summary>
    public string ErrorMessage;
    /// <summary>Events emitted while applying the action (empty on failure).</summary>
    public List<GameEvent> Events = new List<GameEvent>();

    /// <summary>Builds a successful result carrying the given event stream.</summary>
    public static GameStepResult SuccessResult(List<GameEvent> events)
    {
        return new GameStepResult
        {
            Success = true,
            ErrorMessage = null,
            Events = events ?? new List<GameEvent>()
        };
    }

    /// <summary>Builds a failed result with an error message and no events.</summary>
    public static GameStepResult Failure(string errorMessage)
    {
        return new GameStepResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Events = new List<GameEvent>()
        };
    }
}
