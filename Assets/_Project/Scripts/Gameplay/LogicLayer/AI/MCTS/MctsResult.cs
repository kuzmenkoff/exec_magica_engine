using System.Collections.Generic;

/// <summary>
/// Result of an MCTS decision: the chosen action plus the root visit distribution (the policy
/// target for ML) and the root value estimate. Returned by <see cref="MctsActionPolicy.Decide"/>.
/// </summary>
public struct MctsResult
{
    public GameAction Action;
    public List<(GameAction action, int visits)> Policy;
    public double Value;
}
