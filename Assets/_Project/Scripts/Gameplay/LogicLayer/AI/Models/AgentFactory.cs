/// <summary>
/// Builds a runtime <see cref="IGameActionPolicy"/> from a serializable <see cref="AgentSpec"/>.
/// Shared by the Unity Editor tools and the .NET bench runner, so both construct identical policies
/// (network loaded via <see cref="NeuralNetLoader"/>, which resolves .bytes on both runtimes).
/// </summary>
public static class AgentFactory
{
    public static IGameActionPolicy Build(AgentSpec s, int seed)
    {
        switch (s.kind)
        {
            case "greedy":
                return new GreedyActionPolicy(seed, s.heroHpWeight, s.attackWeight, s.hpWeight,
                                              s.minionCountWeight, s.handCountWeight);

            case "neural":
                return new NeuralActionPolicy(NeuralNetLoader.Load(s.networkResource), seed, s.sample);

            case "mcts":
                return new MctsActionPolicy(new MctsConfig
                {
                    BudgetMode = s.budgetMode,
                    Iterations = s.iterations,
                    TimeBudgetMs = s.timeBudgetMs,
                    ExplorationC = s.explorationC,
                    RolloutPolicy = s.rolloutPolicy,
                    MaxRolloutActions = s.maxRolloutActions,
                    FinalAction = s.finalAction,
                    LeafRolloutMix = s.leafRolloutMix,
                    Determinize = s.determinize,
                    KnowsOpponentDeck = s.knowsOpponentDeck,
                    Parallelize = false,           // per-game parallelism is BatchRunner's job
                    Seed = seed,
                    Network = NeuralNetLoader.Load(s.networkResource),   // null when path empty
                    PuctC = s.puctC
                });

            default:  // "random"
                return new RandomActionPolicy(seed);
        }
    }
}
