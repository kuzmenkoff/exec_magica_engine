using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RandomOpponentModel", menuName = "EXEC_MAGICA/Opponent Models/Random")]
/// <summary>Opponent model backed by the random policy.</summary>
public class RandomOpponentModelDefinition : OpponentModelDefinition
{
    /// <inheritdoc/>
    public override IGameActionPolicy CreatePolicy(int seed) => new RandomActionPolicy(seed);

    /// <inheritdoc/>
    public override ModelInfo BuildModelInfo() => new ModelInfo
    {
        ModelId = string.IsNullOrEmpty(Id) ? "Random" : Id,
        Params = new Dictionary<string, object>()
    };
}
