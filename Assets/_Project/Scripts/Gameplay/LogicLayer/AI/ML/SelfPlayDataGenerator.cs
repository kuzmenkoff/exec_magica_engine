using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

/// <summary>
/// Runs MCTS self-play and writes a training dataset: per decision a row of
/// (features, MCTS policy π, side); at game end each row gets the outcome z.
/// Output is JSON Lines, written in chunks (one file per N games) so a crash never loses prior data.
/// </summary>
public static class SelfPlayDataGenerator
{
    /// <summary>One logged decision. Action descriptor encoded as [type, src, tgtZone, tgtIdx, visits].</summary>
    public class Sample
    {
        public float[] features;     // StateEncoder output (length 400)
        public string side;          // acting side ("Player"/"Enemy")
        public List<int[]> policy;   // visit distribution over legal actions (π)
        public float z;              // outcome from `side` POV (+1/0/-1), set at game end
        public int t;                // decisions until the end of the game (for the optional value discount)
        public int[] cardIds;        // per-slot CardId (v2 only; null for v1, NullValueHandling.Ignore)
    }

    private static readonly JsonSerializerSettings JsonSettings =
        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

    /// <summary>Encoding for the next run: 1 = v1-400, 2 = v2-1216, 3 = v3-1616. Set before Generate.</summary>
    public static int FeatureVersion = 2;

    /// <summary>Plays one game with two MCTS policies, capturing per-decision samples.</summary>
    public static List<Sample> RunGame(GameState init, MctsActionPolicy player, MctsActionPolicy enemy, int maxActions = 400)
    {
        var rows = new List<Sample>();
        var engine = new GameEngine(init);

        for (int i = 0; i < maxActions && !engine.State.IsGameOver; i++)
        {
            PlayerSide side = engine.State.ActiveSide;
            List<GameAction> legal = engine.GetLegalActions(side);
            if (legal == null || legal.Count == 0) break;

            MctsActionPolicy mcts = side == PlayerSide.Player ? player : enemy;
            MctsResult r = mcts.Decide(engine.State, legal, side);
            if (r.Action == null) break;

            // record BEFORE applying — features describe the decision state
            var pol = new List<int[]>(r.Policy != null ? r.Policy.Count : 0);
            if (r.Policy != null)
                foreach (var pe in r.Policy)
                    pol.Add(Describe(pe.action, engine.State, side, pe.visits));

            rows.Add(new Sample
            {
                features = FeatureVersion >= 3 ? StateEncoder.EncodeV3(engine.State, side)
                         : FeatureVersion >= 2 ? StateEncoder.EncodeV2(engine.State, side)
                         : StateEncoder.Encode(engine.State, side),
                cardIds = FeatureVersion >= 2 ? StateEncoder.EncodeCardIds(engine.State, side) : null,
                side = side.ToString(),
                policy = pol
            });

            if (!engine.ApplyAction(r.Action).Success) break;
        }

        PlayerSide? winner = engine.State.Winner;
        for (int i = 0; i < rows.Count; i++)
        {
            Sample row = rows[i];
            row.z = winner == null ? 0f : (row.side == winner.Value.ToString() ? 1f : -1f); // outcome from each row's own side POV
            row.t = rows.Count - 1 - i;   // turns till the end
        }

        return rows;
    }

    /// <summary>Generates `games` self-play games, writing JSONL chunks of `gamesPerFile`.</summary>
    public static void Generate(
        Func<int, AllCards> makePlayerDeck, Func<int, AllCards> makeEnemyDeck, AllCards db,
        Func<int, MctsActionPolicy> makePlayer, Func<int, MctsActionPolicy> makeEnemy,
        int targetGames, int gamesPerFile, int baseSeed, string outDir, string runTag,
        Func<int, int, bool> onProgress = null)   // return false to cancel
    {
        Directory.CreateDirectory(outDir);
        int existingFiles = Directory.GetFiles(outDir, runTag + "_*.jsonl").Length;
        int fileIdx = existingFiles;
        int startGame = existingFiles * gamesPerFile;

        var buffer = new List<Sample>();
        for (int g = startGame; g < targetGames; g++)
        {
            int seed = baseSeed + g;
            bool playerFirst = (g % 2) == 0;
            GameState init = GameStateBuilder.CreateInitialState(
                makePlayerDeck(seed), makeEnemyDeck(seed), db, playerFirst, shuffleSeed: seed);
            buffer.AddRange(RunGame(init, makePlayer(seed), makeEnemy(seed)));

            bool keepGoing = onProgress == null || onProgress(g + 1, targetGames);
            bool flush = (g + 1) % gamesPerFile == 0 || g == targetGames - 1 || !keepGoing;
            if (flush && buffer.Count > 0)
            {
                WriteChunk(Path.Combine(outDir, $"{runTag}_{fileIdx:D4}.jsonl"), buffer);
                buffer.Clear();
                fileIdx++;
            }
            if (!keepGoing) break;
        }
    }

    public static void GenerateParallelByFile(
        Func<int, AllCards> makePlayerDeck, Func<int, AllCards> makeEnemyDeck, AllCards db,
        Func<int, MctsActionPolicy> makePlayer, Func<int, MctsActionPolicy> makeEnemy,
        int targetGames, int gamesPerFile, int baseSeed, string outDir, string runTag,
        int maxParallel, Func<int, int, bool> onChunkDone = null)   // return false to cancel
    {
        Directory.CreateDirectory(outDir);
        int existingFiles = Directory.GetFiles(outDir, runTag + "_*.jsonl").Length;
        int fileIdx = existingFiles;
        int startGame = existingFiles * gamesPerFile;

        var po = new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, maxParallel) };

        for (int chunkStart = startGame; chunkStart < targetGames; chunkStart += gamesPerFile)
        {
            int chunkEnd = Math.Min(chunkStart + gamesPerFile, targetGames);
            int n = chunkEnd - chunkStart;
            var chunkRows = new List<Sample>[n];

            System.Threading.Tasks.Parallel.For(0, n, po, k =>
            {
                int g = chunkStart + k;
                int seed = baseSeed + g;
                bool playerFirst = (g % 2) == 0;
                GameState init = GameStateBuilder.CreateInitialState(
                    makePlayerDeck(seed), makeEnemyDeck(seed), db, playerFirst, shuffleSeed: seed);
                chunkRows[k] = RunGame(init, makePlayer(seed), makeEnemy(seed));
            });

            var buffer = new List<Sample>();
            for (int k = 0; k < n; k++)
                if (chunkRows[k] != null) buffer.AddRange(chunkRows[k]);
            WriteChunk(Path.Combine(outDir, $"{runTag}_{fileIdx:D4}.jsonl"), buffer);
            fileIdx++;

            if (onChunkDone != null && !onChunkDone(chunkEnd, targetGames)) break;
        }
    }

    private static void WriteChunk(string path, List<Sample> rows)
    {
        var sb = new StringBuilder();
        foreach (Sample row in rows)
            sb.AppendLine(JsonConvert.SerializeObject(row, Formatting.None, JsonSettings));
        File.WriteAllText(path, sb.ToString());
    }

    // Action -> [type, src, tgtZone, tgtIdx, visits], using SLOT indices consistent with StateEncoder.
    // type: 0 EndTurn, 1 PlayCard, 2 AttackCard, 3 AttackHero
    // tgtZone: 0 none, 1 myField, 2 oppField, 3 myHero, 4 oppHero
    private static int[] Describe(GameAction a, GameState s, PlayerSide me, int visits)
    {
        ActionEncoding.Descriptor(a, s, me, out int type, out int src, out int tz, out int ti);
        return new[] { type, src, tz, ti, visits };
    }
}
