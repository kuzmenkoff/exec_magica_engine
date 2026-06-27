using System.Collections.Generic;

/// <summary>
/// Pure AI policy interface.
/// 
/// MCTS, random AI, heuristic AI and neural-network policies should use this,
/// not Unity objects.
/// </summary>
public interface IGameActionPolicy
{
    /// <summary>
    /// Chooses one action for <paramref name="actorSide"/> from the given legal actions in the
    /// current state. Implementations must not mutate <paramref name="state"/>. Returns null when
    /// no action is available.
    /// </summary>
    GameAction ChooseAction(
        GameState state,
        List<GameAction> legalActions,
        PlayerSide actorSide);
}
