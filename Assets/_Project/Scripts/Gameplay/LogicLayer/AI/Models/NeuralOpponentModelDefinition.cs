using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NeuralOpponentModel", menuName = "EXEC_MAGICA/Opponent Models/Neural (NN)")]
/// <summary>Opponent model backed by the pure distilled network (no search).</summary>
public class NeuralOpponentModelDefinition : OpponentModelDefinition
{
    [Header("Model")]
    [Tooltip("Resources path to the weight blob, e.g. Models/gen0")]
    [SerializeField] private string networkResource = "Models/gen0";
    [Tooltip("False = pick the top-logit move (deterministic); True = sample from masked softmax.")]
    [SerializeField] private bool sample = false;

    /// <inheritdoc/>
    public override IGameActionPolicy CreatePolicy(int seed)
        => new NeuralActionPolicy(NeuralNetLoader.Load(networkResource), seed, sample);

    /// <inheritdoc/>
    public override ModelInfo BuildModelInfo() => new ModelInfo
    {
        ModelId = string.IsNullOrEmpty(Id) ? "NN" : Id,
        Params = new Dictionary<string, object>
        {
            { "networkResource", networkResource },
            { "sample", sample }
        }
    };

    public override AgentSpec ToAgentSpec() => new AgentSpec
    {
        id = Id,
        kind = "neural",
        networkResource = networkResource,
        sample = sample
    };
}
