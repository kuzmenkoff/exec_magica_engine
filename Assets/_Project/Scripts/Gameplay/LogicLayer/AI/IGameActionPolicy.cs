using System.Collections.Generic;

/// <summary>
/// Pure AI policy interface.
/// 
/// MCTS, random AI, heuristic AI and neural-network policies should use this,
/// not Unity objects.
/// </summary>
public interface IGameActionPolicy
{
    GameAction ChooseAction(
        GameState state,
        List<GameAction> legalActions,
        PlayerSide actorSide);
}
