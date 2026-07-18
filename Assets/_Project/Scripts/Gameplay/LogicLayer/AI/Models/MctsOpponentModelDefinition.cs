using UnityEngine;

[CreateAssetMenu(
    fileName = "MctsOpponentModel",
    menuName = "EXEC_MAGICA/Opponent Models/MCTS"
)]
/// <summary>Opponent model backed by the ISMCTS policy; the serialized fields map onto <see cref="MctsConfig"/>.</summary>
public class MctsOpponentModelDefinition : OpponentModelDefinition
{
    [Header("Budget")]
    [SerializeField] private MctsConfig.Budget budgetMode = MctsConfig.Budget.Iterations;
    [SerializeField] private int iterations = 1000;
    [SerializeField] private int timeBudgetMs = 1000;

    [Header("Search")]
    [SerializeField] private double explorationC = 1.41;
    [SerializeField] private MctsConfig.Rollout rolloutPolicy = MctsConfig.Rollout.Random;
    [SerializeField] private int maxRolloutActions = 200;
    [SerializeField] private MctsConfig.FinalSelection finalAction = MctsConfig.FinalSelection.MaxVisits;
    [SerializeField] private double leafRolloutMix = 0.0;

    [Header("Information model")]
    [SerializeField] private bool determinize = true;
    [SerializeField] private bool knowsOpponentDeck = true;

    [Header("Parallelization")]
    [SerializeField] private bool parallelize = false;
    [SerializeField] private int threadCount = 0;     // 0 = auto (ProcessorCount)

    [Header("Reproducibility")]
    [SerializeField] private int seed = 0;            // 0 = non-deterministic

    [Header("Neural guidance (optional)")]
    [Tooltip("Resources path to a weight blob → enables PUCT + value-at-leaf (NN+MCTS). Empty = classic ISMCTS.")]
    [SerializeField] private string networkResource = "";
    [SerializeField] private double puctC = 1.5;

    private MctsConfig BuildConfig(int seed) => new MctsConfig
    {
        BudgetMode = budgetMode,
        Iterations = iterations,
        TimeBudgetMs = timeBudgetMs,
        ExplorationC = explorationC,
        RolloutPolicy = rolloutPolicy,
        MaxRolloutActions = maxRolloutActions,
        FinalAction = finalAction,
        Determinize = determinize,
        KnowsOpponentDeck = knowsOpponentDeck,
        Parallelize = parallelize,
        ThreadCount = threadCount,
        Seed = seed,
        Network = NeuralNetLoader.Load(networkResource),   // null when path empty
        PuctC = puctC,
        LeafRolloutMix = leafRolloutMix,
    };

    /// <inheritdoc/>
    public override IGameActionPolicy CreatePolicy(int seed) => new MctsActionPolicy(BuildConfig(seed));

    /// <inheritdoc/>
    public override ModelInfo BuildModelInfo() => new ModelInfo
    {
        ModelId = string.IsNullOrEmpty(Id) ? "MCTS" : Id,
        Params = new System.Collections.Generic.Dictionary<string, object>
        {
            { "budgetMode", budgetMode.ToString() },
            { "iterations", iterations },
            { "timeBudgetMs", timeBudgetMs },
            { "explorationC", explorationC },
            { "rolloutPolicy", rolloutPolicy.ToString() },
            { "maxRolloutActions", maxRolloutActions },
            { "determinize", determinize },
            { "knowsOpponentDeck", knowsOpponentDeck },
            { "finalAction", finalAction.ToString() },
            { "parallelize", parallelize },
            { "threadCount", threadCount },
            { "networkResource", networkResource },
            { "puctC", puctC },
            { "leafRolloutMix", leafRolloutMix },
        }
    };

    public override AgentSpec ToAgentSpec() => new AgentSpec
    {
        id = Id,
        kind = "mcts",
        budgetMode = budgetMode,
        iterations = iterations,
        timeBudgetMs = timeBudgetMs,
        explorationC = explorationC,
        rolloutPolicy = rolloutPolicy,
        maxRolloutActions = maxRolloutActions,
        finalAction = finalAction,
        leafRolloutMix = leafRolloutMix,
        determinize = determinize,
        knowsOpponentDeck = knowsOpponentDeck,
        networkResource = networkResource,
        puctC = puctC
    };
}
