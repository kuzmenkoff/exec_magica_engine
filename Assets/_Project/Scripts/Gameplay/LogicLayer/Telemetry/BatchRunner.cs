using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class BatchConfig
{
    public int Games = 100;
    public int BaseSeed = 1;
    public int MaxActions = 400;
    public bool AlternateStart = true;
    public bool LogEvents = false;     // off for mass runs
    public int MaxParallelGames = 1;
}

public class BatchSummary
{
    public int Games;
    public int PlayerWins;
    public int EnemyWins;
    public int Draws;
    public int NonDecisive;            // MaxActionsReached / stalls
    public double PlayerWinRate;
    public double PlayerWinRateCiLow;  // Wilson 95%
    public double PlayerWinRateCiHigh;
    public Dictionary<string, int> EndReasonCounts = new Dictionary<string, int>();
    public double AvgTurns;
    public double AvgActions;
    public double PlayerMeanThinkMs;
    public double EnemyMeanThinkMs;
    public Dictionary<int, double> CardImpactById = new Dictionary<int, double>(); // avg impact per game
    public double TotalDurationMs;
}

public class BatchResult
{
    public string RunId = System.Guid.NewGuid().ToString();
    public string StartedAtUtc = System.DateTime.UtcNow.ToString("o");
    public ModelInfo PlayerModel;
    public ModelInfo EnemyModel;
    public string PlayerDeck;
    public string EnemyDeck;
    public BatchConfig Config;

    public List<SessionRecord> Sessions = new List<SessionRecord>();
    public BatchSummary Summary = new BatchSummary();
}

public static class BatchRunner
{
    public static BatchResult Run(
        Func<int, AllCards> makePlayerDeck, Func<int, AllCards> makeEnemyDeck, AllCards database,
        Func<int, IGameActionPolicy> makePlayerPolicy,
        Func<int, IGameActionPolicy> makeEnemyPolicy,
        ModelInfo playerModel, ModelInfo enemyModel,
        string playerDeckName, string enemyDeckName,
        BatchConfig config,
        Func<int, int, bool> onProgress = null)
    {
        config = config ?? new BatchConfig();

        BatchResult result = new BatchResult
        {
            PlayerModel = playerModel,
            EnemyModel = enemyModel,
            PlayerDeck = playerDeckName,
            EnemyDeck = enemyDeckName,
            Config = config
        };

        SessionRecord[] sessions = new SessionRecord[config.Games];

        SessionRecord RunOneGame(int g)
        {
            int seed = config.BaseSeed + g;
            bool playerFirst = !config.AlternateStart || (g % 2 == 0);

            AllCards pDeck = makePlayerDeck(seed);
            AllCards eDeck = makeEnemyDeck(seed);

            GameState init = GameStateBuilder.CreateInitialState(
                pDeck, eDeck, database, playerFirst, shuffleSeed: seed);

            IGameActionPolicy p = makePlayerPolicy(seed);
            IGameActionPolicy e = makeEnemyPolicy(seed);

            SessionRecord meta = new SessionRecord
            {
                SessionId = Guid.NewGuid().ToString(),
                Seed = seed,
                StartedAtUtc = DateTime.UtcNow.ToString("o"),
                StartingSide = playerFirst ? "Player" : "Enemy",
                Players = new Dictionary<string, ModelInfo> { { "Player", playerModel }, { "Enemy", enemyModel } },
                Decks = new Dictionary<string, string> { { "Player", playerDeckName }, { "Enemy", enemyDeckName } }
            };

            SessionRunner.RunGame(init, p, e, meta, config.MaxActions, config.LogEvents);
            return meta;
        }

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        if (config.MaxParallelGames <= 1)
        {
            for (int g = 0; g < config.Games; g++)
            {
                sessions[g] = RunOneGame(g);
                if (onProgress != null && !onProgress(g + 1, config.Games))
                    break; // cancelled -> summarize completed games
            }
        }
        else
        {
            // Game-level parallelism. Shared inputs (database/decks/model defs) are read-only;
            // each game has its own state + policies. onProgress is NOT called from worker
            // threads (EditorUtility is main-thread only).
            ParallelOptions po = new ParallelOptions { MaxDegreeOfParallelism = config.MaxParallelGames };
            Parallel.For(0, config.Games, po, g => { sessions[g] = RunOneGame(g); });
        }

        sw.Stop();

        foreach (SessionRecord s in sessions)
            if (s != null) result.Sessions.Add(s);

        result.Summary = Summarize(result.Sessions);
        result.Summary.TotalDurationMs = sw.Elapsed.TotalMilliseconds;
        return result;
    }

    private static BatchSummary Summarize(List<SessionRecord> sessions)
    {
        BatchSummary sum = new BatchSummary { Games = sessions.Count };
        double turns = 0, actions = 0, pThink = 0, eThink = 0;

        Dictionary<int, double> impactTotals = new Dictionary<int, double>();

        foreach (SessionRecord s in sessions)
        {
            string w = s.Outcome.Winner;
            if (w == "Player") sum.PlayerWins++;
            else if (w == "Enemy") sum.EnemyWins++;
            else if (s.Outcome.Reason == GameEndReason.Draw.ToString()) sum.Draws++;
            else sum.NonDecisive++;

            string reason = s.Outcome.Reason ?? "Unknown";
            sum.EndReasonCounts.TryGetValue(reason, out int c);
            sum.EndReasonCounts[reason] = c + 1;

            turns += s.Outcome.TotalTurns;
            actions += s.Outcome.TotalActions;
            if (s.PerSideMetrics.TryGetValue("Player", out PerSideMetrics pm)) pThink += pm.MeanThinkMs;
            if (s.PerSideMetrics.TryGetValue("Enemy", out PerSideMetrics em)) eThink += em.MeanThinkMs;

            if (s.CardImpact != null)
                foreach (KeyValuePair<int, double> kv in s.CardImpact)
                {
                    impactTotals.TryGetValue(kv.Key, out double cur);
                    impactTotals[kv.Key] = cur + kv.Value;
                }
        }

        int n = Math.Max(1, sessions.Count);

        foreach (KeyValuePair<int, double> kv in impactTotals)
            sum.CardImpactById[kv.Key] = kv.Value / n;   // average impact per game

        sum.PlayerWinRate = (double)sum.PlayerWins / n;
        (sum.PlayerWinRateCiLow, sum.PlayerWinRateCiHigh) = WilsonCI(sum.PlayerWins, sessions.Count);
        sum.AvgTurns = turns / n;
        sum.AvgActions = actions / n;
        sum.PlayerMeanThinkMs = pThink / n;
        sum.EnemyMeanThinkMs = eThink / n;
        return sum;
    }

    public static (double low, double high) WilsonCI(int wins, int total, double z = 1.96)
    {
        if (total <= 0) return (0.0, 0.0);
        double p = (double)wins / total;
        double z2 = z * z;
        double denom = 1.0 + z2 / total;
        double centre = (p + z2 / (2.0 * total)) / denom;
        double margin = (z * Math.Sqrt(p * (1.0 - p) / total + z2 / (4.0 * total * total))) / denom;
        return (centre - margin, centre + margin);
    }
}
