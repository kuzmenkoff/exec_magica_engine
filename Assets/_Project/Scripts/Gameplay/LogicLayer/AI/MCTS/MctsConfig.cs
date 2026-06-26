public class MctsConfig
{
    public enum Budget { Iterations, Time }
    public enum Rollout { Random, Greedy }            // Neural to be added later
    public enum FinalSelection { MaxVisits, MaxValue }

    public Budget BudgetMode = Budget.Iterations;
    public int Iterations = 1000;                      // TOTAL across threads in Iterations mode
    public int TimeBudgetMs = 1000;                    // per thread in Time mode

    public double ExplorationC = 1.41;
    public Rollout RolloutPolicy = Rollout.Random;
    public int MaxRolloutActions = 200;

    public bool Determinize = true;
    public bool KnowsOpponentDeck = true;

    public FinalSelection FinalAction = FinalSelection.MaxVisits;
    public int Seed = 0;

    // --- Parallelization (root parallelization) ---
    public bool Parallelize = false;                   // off by default: deterministic & simplest
    public int ThreadCount = 0;                        // 0 = auto (Environment.ProcessorCount)
}
