using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GreedyOpponentModel", menuName = "EXEC_MAGICA/Opponent Models/Greedy")]
public class GreedyOpponentModelDefinition : OpponentModelDefinition
{
    [Header("Evaluation weights")]
    [SerializeField] private double heroHpWeight = 1.0;
    [SerializeField] private double attackWeight = 2.0;
    [SerializeField] private double hpWeight = 1.0;
    [SerializeField] private double minionCountWeight = 1.0;
    [SerializeField] private double handCountWeight = 1.0;

    public override IGameActionPolicy CreatePolicy(int seed)
        => new GreedyActionPolicy(seed, heroHpWeight, attackWeight, hpWeight, minionCountWeight, handCountWeight);

    public override ModelInfo BuildModelInfo() => new ModelInfo
    {
        ModelId = string.IsNullOrEmpty(Id) ? "Greedy" : Id,
        Params = new Dictionary<string, object>
        {
            { "heroHpWeight", heroHpWeight },
            { "attackWeight", attackWeight },
            { "hpWeight", hpWeight },
            { "minionCountWeight", minionCountWeight },
            { "handCountWeight", handCountWeight }
        }
    };
}
