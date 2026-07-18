// Serializable spec for a headless bench run. Unity writes it, the .NET runner reads it.
[System.Serializable]
public class BenchRunSpec
{
    public string mode = "generate";      // "generate" (more modes later)
    public string resourcesRoot = "";     // absolute Assets/Resources; "" = ../Assets/Resources

    // output
    public int generation = 1;
    public string outDir = "";            // "" = Runs/SelfPlayData/gen{generation}
    public string runTag = "";            // "" = gen{generation}
    public int baseSeed = 0;              // 0 = generation*1_000_000 + 1

    // run
    public int games = 3000;
    public int gamesPerFile = 100;
    public int maxActions = 400;
    public int featureVersion = 2;
    public int parallelGames = 0;         // 0 = all cores

    // teacher
    public int iterations = 1000;
    public string rollout = "Random";     // Random / Greedy
    public int maxRolloutActions = 200;
    public string networkResource = "";   // "" = plain MCTS; else e.g. "Models/gen0"
    public double leafRolloutMix = 0.0;
    public double puctC = 1.5;

    // decks
    public double presetFraction = 0.6;

    // --- match mode (player = neural teacher config vs opponent, time-budget sweep) ---
    public string opponent = "greedy";                        // tuned Greedy defaults
    public string[] decks = { "AggroPreset", "ControlPreset" };
    public int gamesPerCell = 200;
    public int[] timeBudgetsMs = { 400, 850, 1700, 2900 };

    // --- ladder / batch modes (agent-spec roster) ---
    public System.Collections.Generic.List<AgentSpec> agents;   // ladder: round-robin roster; batch: [0] vs [1]
    public string anchorId = "random";                          // ladder: Elo anchor id
    public bool overwrite = false;                              // ladder: rerun cells already present
    public string outputRoot = "";                              // batch: SessionWriter root ("" = Runs)
    public bool alternateStart = true;                          // batch / ladder
    public bool logEvents = false;                              // batch: write full event logs
    public bool oneFilePerSession = false;                      // batch: SessionWriter file layout
}
