using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SelfPlayDataWindow : EditorWindow
{
    private int generation = 0;
    private int games = 10000;
    private int gamesPerFile = 20;
    private int maxActions = 400;

    private int BaseSeed => generation * 1_000_000 + 1;

    private int iterations = 2000;
    private int parallelGames = 1;
    private MctsConfig.Rollout rollout = MctsConfig.Rollout.Random;
    private int maxRolloutActions = 200;
    private int featureVersion = 2;   // 1 = v1-400, 2 = v2-1216

    // Neural teacher (optional) — empty networkResource = plain MCTS
    private string networkResource = "";
    private double leafRolloutMix = 0.0;
    private double puctC = 1.5;

    private string lastResult = "";

    [MenuItem("EXEC_MAGICA/Self-Play Data...")]
    public static void Open() => GetWindow<SelfPlayDataWindow>("Self-Play Data");

    private string OutputDir() =>
        Path.Combine(Path.GetDirectoryName(Application.dataPath), "Runs", "SelfPlayData", $"gen{generation}");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Generation & output", EditorStyles.boldLabel);
        generation = Mathf.Max(0, EditorGUILayout.IntField("Generation", generation));
        EditorGUILayout.HelpBox(OutputDir(), MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Run settings", EditorStyles.boldLabel);
        games = EditorGUILayout.IntField("Games", games);
        gamesPerFile = Mathf.Max(1, EditorGUILayout.IntField("Games per file", gamesPerFile));
        maxActions = EditorGUILayout.IntField("Max actions", maxActions);
        featureVersion = EditorGUILayout.IntPopup("Feature version", featureVersion,
    new[] { "v1 (400)", "v2 (1216)" }, new[] { 1, 2 });
        parallelGames = EditorGUILayout.IntField("Parallel games (cores)", parallelGames);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("MCTS teacher", EditorStyles.boldLabel);
        iterations = EditorGUILayout.IntField("Iterations", iterations);
        rollout = (MctsConfig.Rollout)EditorGUILayout.EnumPopup("Rollout", rollout);
        maxRolloutActions = EditorGUILayout.IntField("Max rollout actions", maxRolloutActions);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Neural teacher (optional — empty = plain MCTS)", EditorStyles.boldLabel);
        networkResource = EditorGUILayout.TextField("Network resource", networkResource);
        leafRolloutMix = EditorGUILayout.DoubleField("Leaf rollout mix", leafRolloutMix);
        puctC = EditorGUILayout.DoubleField("PUCT c", puctC);

        EditorGUILayout.Space();
        if (GUILayout.Button($"Generate {games} games  →  gen{generation}"))
            Run();
        if (GUILayout.Button($"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"" +
            $"Generate via .NET (parallel)  →  gen{generation}"))
            BenchLauncher.Launch(new BenchRunSpec
            {
           generation = generation,
                games = games,
                gamesPerFile = gamesPerFile,
                maxActions = maxActions,
                featureVersion = featureVersion,
                parallelGames = parallelGames,   // 0 = all cores
                iterations = iterations,
                rollout = rollout.ToString(),
                maxRolloutActions = maxRolloutActions,
                networkResource = networkResource,
                leafRolloutMix = leafRolloutMix,
                puctC = puctC,
                presetFraction = 0.6
            });
        if (!string.IsNullOrEmpty(lastResult))
            EditorGUILayout.HelpBox(lastResult, MessageType.Info);
    }

    private void Run()
    {
        try
        {
            AllCards db = LoadDatabase();

            SelfPlayDataGenerator.FeatureVersion = featureVersion;

            var presets = new System.Collections.Generic.List<AllCards>();
            foreach (string name in RuntimeDeckLoader.GetPresetNames())
                presets.Add(RuntimeDeckLoader.LoadPreset(name, db));

            Func<int, AllCards> pDeck = seed =>
                (new System.Random(seed).Next(10) < 6)
                    ? presets[new System.Random(seed).Next(presets.Count)]
                    : RuntimeDeckLoader.RandomDeck(db, seed);
            Func<int, AllCards> eDeck = seed =>
                (new System.Random(seed + 7919).Next(10) < 6)
                    ? presets[new System.Random(seed + 7919).Next(presets.Count)]
                    : RuntimeDeckLoader.RandomDeck(db, seed + 7919);

            MctsConfig Cfg(int seed) => new MctsConfig
            {
                BudgetMode = MctsConfig.Budget.Iterations,
                Iterations = iterations,
                RolloutPolicy = rollout,
                MaxRolloutActions = maxRolloutActions,
                Determinize = true,
                KnowsOpponentDeck = true,
                Parallelize = false,
                Seed = seed
            };
            Func<int, MctsActionPolicy> mp = seed => new MctsActionPolicy(Cfg(seed));
            Func<int, MctsActionPolicy> me = seed => new MctsActionPolicy(Cfg(seed + 1));

            string outDir = OutputDir();
            WriteMeta(outDir);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            int last = 0;

            if (parallelGames <= 1)
            {
                SelfPlayDataGenerator.Generate(pDeck, eDeck, db, mp, me,
                    games, gamesPerFile, BaseSeed, outDir, $"gen{generation}",
                    onProgress: (done, total) =>
                    {
                        if (done > last)
                        {
                            Debug.Log($"[gen{generation}] game {done}/{total}: {sw.ElapsedMilliseconds} ms");
                            sw.Restart(); last = done;
                        }
                        bool cancel = EditorUtility.DisplayCancelableProgressBar(
                            $"Self-Play gen{generation}",
                            $"Game {done}/{total}   (Cancel = stop & save)",
                            done / (float)total);
                        return !cancel;
                    });
            }
            else
            {
                SelfPlayDataGenerator.GenerateParallelByFile(pDeck, eDeck, db, mp, me,
                    games, gamesPerFile, BaseSeed, outDir, $"gen{generation}", parallelGames,
                    onChunkDone: (done, total) =>
                    {
                        Debug.Log($"[gen{generation}] {done}/{total} games ({parallelGames} cores): file in {sw.ElapsedMilliseconds} ms");
                        sw.Restart();
                        bool cancel = EditorUtility.DisplayCancelableProgressBar(
                            $"Self-Play gen{generation}",
                            $"Games {done}/{total}   (Cancel = stop after current file)",
                            done / (float)total);
                        return !cancel;
                    });
            }

            lastResult = $"Done/stopped: ≤{games} games (it={iterations}, {rollout}, parallel={parallelGames}) → {outDir}";
            Debug.Log("[SelfPlay] " + lastResult);
        }
        catch (Exception ex) { lastResult = "Error: " + ex.Message; Debug.LogException(ex); }
        finally { EditorUtility.ClearProgressBar(); }
    }

    private static AllCards LoadDatabase()
    {
        AllCards db = new AllCards();
        foreach (TextAsset ta in Resources.LoadAll<TextAsset>("CardsInfo/AllCards"))
        {
            AllCards set = CardJsonLoader.LoadAllCards(ta.text);
            if (set != null) db.AddCardSet(set);
        }
        return db;
    }
    private void WriteMeta(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var meta = new
        {
            generation,
            createdUtc = DateTime.UtcNow.ToString("o"),
            cardSetRevision = new SessionRecord().CardSetRevision,
            featureVersion = this.featureVersion >= 2 ? StateEncoder.LayoutVersionV2 : StateEncoder.LayoutVersion,
            teacher = new { type = "MCTS", rollout = rollout.ToString(), iterations, maxRolloutActions },
            decks = "preset60/random40",
            baseSeed = BaseSeed,
            targetGames = games,
            maxActions
        };
        File.WriteAllText(Path.Combine(outDir, "meta.json"),
            Newtonsoft.Json.JsonConvert.SerializeObject(meta, Newtonsoft.Json.Formatting.Indented));
    }

}
