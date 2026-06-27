using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Pure random policy.
/// Can be used both in live game and inside rollouts.
/// </summary>
public class RandomActionPolicy : IGameActionPolicy
{
    private readonly Random random;

    /// <summary>Creates a random policy. <paramref name="seed"/>: 0 = non-deterministic, non-zero = reproducible.</summary>
    public RandomActionPolicy(int seed = 0)
    {
        random = seed == 0
            ? new Random()
            : new Random(seed);
    }

    /// <summary>Picks a uniformly random legal action, preferring non-EndTurn actions (ends the turn only when forced).</summary>
    public GameAction ChooseAction(
        GameState state,
        List<GameAction> legalActions,
        PlayerSide actorSide)
    {
        if (state == null || legalActions == null || legalActions.Count == 0)
            return null;

        List<GameAction> actorActions = legalActions
            .Where(action => action != null && action.ActorSide == actorSide)
            .ToList();

        if (actorActions.Count == 0)
            return null;

        List<GameAction> nonEndTurnActions = actorActions
            .Where(action => action.Type != GameActionType.EndTurn)
            .ToList();

        if (nonEndTurnActions.Count > 0)
            return nonEndTurnActions[random.Next(nonEndTurnActions.Count)];

        return actorActions[random.Next(actorActions.Count)];
    }
}
