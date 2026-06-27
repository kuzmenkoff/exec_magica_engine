using System.Collections.Generic;

/// <summary>
/// Pure simulation runner for AI/MCTS.
/// Uses the same GameEngine as the live game.
/// </summary>
public static class GameSimulationRunner
{
    /// <summary>
    /// Plays the given state to terminal (or <paramref name="maxActions"/>) using the two policies on a
    /// deep copy, and returns the final state. Used for diagnostics/playouts.
    /// </summary>
    public static GameState SimulatePlayout(
        GameState initialState,
        IGameActionPolicy playerPolicy,
        IGameActionPolicy enemyPolicy,
        int maxActions = 200)
    {
        if (initialState == null)
            return null;

        GameState simulationState = initialState.GetDeepCopy();
        GameEngine engine = new GameEngine(simulationState);

        for (int i = 0; i < maxActions; i++)
        {
            if (engine.State == null || engine.State.IsGameOver)
                break;

            PlayerSide actorSide = engine.State.ActiveSide;

            List<GameAction> legalActions = engine.GetLegalActions(actorSide);

            if (legalActions == null || legalActions.Count == 0)
                break;

            IGameActionPolicy policy = actorSide == PlayerSide.Player
                ? playerPolicy
                : enemyPolicy;

            if (policy == null)
                break;

            GameAction action = policy.ChooseAction(
                engine.State,
                legalActions,
                actorSide
            );

            if (action == null)
                break;

            GameStepResult result = engine.ApplyAction(action);

            if (!result.Success)
                break;
        }

        return engine.State;
    }

    /// <summary>Convenience playout with seeded random policies for both sides.</summary>
    public static GameState SimulateRandomPlayout(
        GameState initialState,
        int maxActions = 200,
        int seed = 0)
    {
        IGameActionPolicy playerPolicy = new RandomActionPolicy(seed);
        IGameActionPolicy enemyPolicy = new RandomActionPolicy(seed + 1);

        return SimulatePlayout(
            initialState,
            playerPolicy,
            enemyPolicy,
            maxActions
        );
    }
}
