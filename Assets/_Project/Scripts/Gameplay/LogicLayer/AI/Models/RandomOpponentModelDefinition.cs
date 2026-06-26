using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RandomOpponentModel", menuName = "EXEC_MAGICA/Opponent Models/Random")]
public class RandomOpponentModelDefinition : OpponentModelDefinition
{
    public override IGameActionPolicy CreatePolicy(int seed) => new RandomActionPolicy(seed);

    public override ModelInfo BuildModelInfo() => new ModelInfo
    {
        ModelId = string.IsNullOrEmpty(Id) ? "Random" : Id,
        Params = new Dictionary<string, object>()
    };
}
