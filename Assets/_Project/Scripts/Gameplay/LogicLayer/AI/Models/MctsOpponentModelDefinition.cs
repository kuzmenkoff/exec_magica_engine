using UnityEngine;

[CreateAssetMenu(
    fileName = "MctsOpponentModel",
    menuName = "EXEC_MAGICA/Opponent Models/MCTS"
)]
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

    [Header("Information model")]
    [SerializeField] private bool determinize = true;
    [SerializeField] private bool knowsOpponentDeck = true;

    [Header("Parallelization")]
    [SerializeField] private bool parallelize = false;
    [SerializeField] private int threadCount = 0;     // 0 = auto (ProcessorCount)

    [Header("Reproducibility")]
    [SerializeField] private int seed = 0;            // 0 = non-deterministic

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
        Seed = seed
    };

    public override IGameActionPolicy CreatePolicy(int seed) => new MctsActionPolicy(BuildConfig(seed));

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
            { "threadCount", threadCount }
        }
    };
}
