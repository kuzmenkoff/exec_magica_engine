using System.Collections.Generic;
using System.Text;

/// <summary>
/// Formats GameEngine events for readable debug output.
/// </summary>
public static class GameEventDebugFormatter
{
    /// <summary>Returns a human-readable, numbered listing of an event stream.</summary>
    public static string Format(List<GameEvent> events)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("========== GAME EVENTS ==========");

        if (events == null || events.Count == 0)
        {
            sb.AppendLine("No events.");
            sb.AppendLine("=================================");
            return sb.ToString();
        }

        for (int i = 0; i < events.Count; i++)
        {
            GameEvent gameEvent = events[i];

            if (gameEvent == null)
            {
                sb.AppendLine($"{i}. null");
                continue;
            }

            sb.AppendLine(
                $"{i}. [{gameEvent.Type}] " +
                $"Turn: {gameEvent.TurnNumber}, " +
                $"ActionIndex: {gameEvent.ActionIndex}, " +
                $"Actor: {gameEvent.ActorSide}, " +
                $"Value: {gameEvent.Value}, " +
                $"Message: {gameEvent.Message}"
            );
        }

        sb.AppendLine("=================================");

        return sb.ToString();
    }
}
