using System.Collections.Generic;

/// <summary>
/// Result of applying one GameAction to GameState.
/// </summary>
public class GameStepResult
{
    public bool Success;
    public string ErrorMessage;

    public List<GameEvent> Events = new List<GameEvent>();

    public static GameStepResult SuccessResult(List<GameEvent> events)
    {
        return new GameStepResult
        {
            Success = true,
            ErrorMessage = null,
            Events = events ?? new List<GameEvent>()
        };
    }

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
