/// <summary>
/// Configuration for <see cref="MctsActionPolicy"/> — search budget, exploration, rollout
/// policy, information model, final-move selection, seeding and parallelization.
/// </summary>
public class MctsConfig
{
    /// <summary>Whether the budget is a fixed iteration count or wall-clock time.</summary>
    public enum Budget { Iterations, Time }
    /// <summary>Policy used to play a node out to a terminal state during rollout.</summary>
    public enum Rollout { Random, Greedy }
    /// <summary>How the final move is chosen once search ends: most-visited or highest mean value.</summary>
    public enum FinalSelection { MaxVisits, MaxValue }

    /// <summary>Whether the budget is counted in iterations or milliseconds.</summary>
    public Budget BudgetMode = Budget.Iterations;
    /// <summary>Iteration budget — TOTAL across threads in Iterations mode.</summary>
    public int Iterations = 1000;
    /// <summary>Time budget per thread, in milliseconds (Time mode).</summary>
    public int TimeBudgetMs = 1000;

    /// <summary>UCB1 exploration constant C (≈√2 by default).</summary>
    public double ExplorationC = 1.41;
    /// <summary>Rollout policy used from expanded nodes to terminal.</summary>
    public Rollout RolloutPolicy = Rollout.Random;
    /// <summary>Hard cap on actions per rollout; if reached, the rollout scores as a draw (0.5).</summary>
    public int MaxRolloutActions = 200;
    /// <summary>NN-MCTS only: blend a rollout into the leaf value (0 = pure value, 1 = pure rollout).</summary>
    public double LeafRolloutMix = 0.0;

    /// <summary>Optional distilled net. When set, search uses PUCT priors + value-at-leaf
    /// instead of UCB1 + rollout (NN-guided MCTS). Null = classic ISMCTS.</summary>
    public NeuralNet Network = null;
    /// <summary>PUCT exploration constant (used only when <see cref="Network"/> is set).</summary>
    public double PuctC = 1.5;

    /// <summary>If true, re-determinize hidden info each iteration (ISMCTS); false = perfect-information search.</summary>
    public bool Determinize = true;
    /// <summary>Known-decklist assumption: the opponent's hidden cards are re-dealt from their actual deck.</summary>
    public bool KnowsOpponentDeck = true;

    /// <summary>How the root move is selected after search.</summary>
    public FinalSelection FinalAction = FinalSelection.MaxVisits;
    /// <summary>RNG seed; 0 = non-deterministic, non-zero = reproducible.</summary>
    public int Seed = 0;

    /// <summary>Enable root parallelization (off by default for determinism and simplicity).</summary>
    public bool Parallelize = false;
    /// <summary>Worker thread count; 0 = auto (<see cref="System.Environment.ProcessorCount"/>).</summary>
    public int ThreadCount = 0;
}
