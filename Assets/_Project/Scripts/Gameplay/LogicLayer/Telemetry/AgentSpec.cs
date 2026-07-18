/// <summary>
/// Serializable description of an agent, portable between the Unity Editor windows and the .NET
/// bench runner. <see cref="AgentFactory"/> turns it into an <see cref="IGameActionPolicy"/>.
/// Covers the standard kinds: random, greedy, mcts (optional network = NN-guided), neural (standalone).
/// </summary>
public class AgentSpec
{
    public string id = "";
    public string kind = "random";        // random | greedy | mcts | neural

    // greedy weights
    public double heroHpWeight = 1.0, attackWeight = 2.0, hpWeight = 1.0,
                  minionCountWeight = 1.0, handCountWeight = 1.0;

    // mcts
    public MctsConfig.Budget budgetMode = MctsConfig.Budget.Iterations;
    public int iterations = 1000;
    public int timeBudgetMs = 1000;
    public double explorationC = 1.41;
    public MctsConfig.Rollout rolloutPolicy = MctsConfig.Rollout.Random;
    public int maxRolloutActions = 40;
    public MctsConfig.FinalSelection finalAction = MctsConfig.FinalSelection.MaxVisits;
    public double leafRolloutMix = 0.0;
    public bool determinize = true;
    public bool knowsOpponentDeck = true;

    // network (mcts NN-guided OR neural standalone) + puct
    public string networkResource = "";
    public double puctC = 1.5;
    public bool sample = false;           // neural standalone: false = argmax (top logit)
}
