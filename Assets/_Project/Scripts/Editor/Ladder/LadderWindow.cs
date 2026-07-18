using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Incremental rating-ladder runner. Pick models and deck conditions; "Run missing" only plays
/// the (a,b,cond) cells absent from Runs/Ladder/matchups.jsonl (unless Overwrite), appends them,
/// then regenerates standings.json + LADDER.md. Mirror decks (same deck both sides per game).
/// </summary>
public class LadderWindow : EditorWindow
{
    private IReadOnlyList<OpponentModelDefinition> models;
    private bool[] modelSel;
    private string[] presetNames;
    private bool[] presetSel;
    private bool useRandomPreset, useRandomDeck;

    private int games = 200, maxActions = 400, parallelGames = 6, baseSeed = 7000;
    private bool overwrite = false;
    private string anchorId = "random";
    private string status = "";
    private Vector2 scroll;

    [MenuItem("EXEC_MAGICA/Ladder...")]
    public static void Open() => GetWindow<LadderWindow>("EXEC_MAGICA Ladder");

    private void OnEnable()
    {
        models = OpponentModelCatalog.GetAll();
        modelSel = new bool[models.Count];

        var pn = RuntimeDeckLoader.GetPresetNames();
        presetNames = new string[pn.Count];
        for (int i = 0; i < pn.Count; i++) presetNames[i] = pn[i];
        presetSel = new bool[presetNames.Length];
    }

    private static string LadderDir =>
        Path.Combine(Path.GetDirectoryName(Application.dataPath), "Runs", "Ladder");
    private static string MatchupsPath => Path.Combine(LadderDir, "matchups.jsonl");

    private void OnGUI()
    {
        if (models == null || models.Count == 0)
        { EditorGUILayout.HelpBox("No models in Resources/OpponentModels.", MessageType.Warning); return; }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Models", EditorStyles.boldLabel);
        for (int i = 0; i < models.Count; i++)
            modelSel[i] = EditorGUILayout.ToggleLeft(
                $"{(string.IsNullOrEmpty(models[i].DisplayName) ? models[i].Id : models[i].DisplayName)}  ({models[i].Id})",
                modelSel[i]);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Deck conditions (mirror)", EditorStyles.boldLabel);
        for (int i = 0; i < presetNames.Length; i++)
            presetSel[i] = EditorGUILayout.ToggleLeft(presetNames[i] + "  (mirror preset)", presetSel[i]);
        useRandomPreset = EditorGUILayout.ToggleLeft("Random presets  (RandomPreset)", useRandomPreset);
        useRandomDeck = EditorGUILayout.ToggleLeft("Random decks  (RandomDeck)", useRandomDeck);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Run settings", EditorStyles.boldLabel);
        games = EditorGUILayout.IntField("Games / cell", games);
        maxActions = EditorGUILayout.IntField("Max actions", maxActions);
        parallelGames = EditorGUILayout.IntField("Parallel games", parallelGames);
        baseSeed = EditorGUILayout.IntField("Base seed", baseSeed);
        anchorId = EditorGUILayout.TextField("Elo anchor (id)", anchorId);
        overwrite = EditorGUILayout.Toggle("Overwrite existing", overwrite);

        EditorGUILayout.Space();
        if (GUILayout.Button("Run missing")) Run();
        if (GUILayout.Button("Run missing (.NET)")) RunDotNet();
        if (GUILayout.Button("Recompute standings (no games)")) Recompute();
        if (GUILayout.Button("Open Runs/Ladder folder"))
        { Directory.CreateDirectory(LadderDir); EditorUtility.RevealInFinder(LadderDir); }

        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
        EditorGUILayout.EndScrollView();
    }

    private List<string> SelectedModels()
    {
        var ids = new List<string>();
        for (int i = 0; i < models.Count; i++) if (modelSel[i]) ids.Add(models[i].Id);
        return ids;
    }

    private List<string> SelectedConditions()
    {
        var c = new List<string>();
        for (int i = 0; i < presetNames.Length; i++) if (presetSel[i]) c.Add(presetNames[i]);
        if (useRandomPreset) c.Add("RandomPreset");
        if (useRandomDeck) c.Add("RandomDeck");
        return c;
    }

    private void Run()
    {
        try
        {
            List<string> ids = SelectedModels(), conds = SelectedConditions();
            if (ids.Count < 2) { status = "Select at least 2 models."; return; }
            if (conds.Count == 0) { status = "Select at least 1 deck condition."; return; }

            AllCards db = LoadDatabase();
            if (db == null || db.GetCollectibleCards().Count == 0) { status = "Card DB empty."; return; }

            string cardSetRev = new SessionRecord().CardSetRevision;
            var byId = new Dictionary<string, OpponentModelDefinition>();
            foreach (var m in models) byId[m.Id] = m;

            var existing = LadderStore.Load(MatchupsPath, cardSetRev);

            // Pre-load decks on the MAIN thread (Resources.Load is main-thread only; BatchRunner
            // builds decks on worker threads when parallelGames > 1).
            RuntimeDeckLoader.GetPresetNames();   // warm the preset-name cache here, not on a worker
            var presetDecks = new Dictionary<string, AllCards>();
            foreach (string cond in conds)
                if (cond != "RandomPreset" && cond != "RandomDeck")
                    presetDecks[cond] = RuntimeDeckLoader.LoadPreset(cond, db);

            List<AllCards> randomPresetPool = null;
            if (conds.Contains("RandomPreset"))
            {
                randomPresetPool = new List<AllCards>();
                foreach (string p in presetNames) randomPresetPool.Add(RuntimeDeckLoader.LoadPreset(p, db));
            }

            // Thread-safe factory from pre-loaded data (no Resources.Load on worker threads).
            Func<string, Func<int, AllCards>> factoryFor = cond =>
            {
                if (cond == "RandomDeck") return seed => RuntimeDeckLoader.RandomDeck(db, seed);   // in-memory, safe
                if (cond == "RandomPreset") return seed => randomPresetPool[new System.Random(seed).Next(randomPresetPool.Count)];
                AllCards d = presetDecks[cond];
                return _ => d;                                                                       // shared read-only mirror deck
            };

            // Build the missing cells (canonical pair a<=b).
            var cells = new List<(string a, string b, string cond)>();
            for (int i = 0; i < ids.Count; i++)
                for (int j = i + 1; j < ids.Count; j++)
                {
                    bool ab = string.CompareOrdinal(ids[i], ids[j]) <= 0;
                    string a = ab ? ids[i] : ids[j], b = ab ? ids[j] : ids[i];
                    foreach (string cond in conds)
                        if (overwrite || !existing.ContainsKey(LadderStore.Key(a, b, cond)))
                            cells.Add((a, b, cond));
                }

            if (cells.Count == 0) { status = "Nothing to run — all selected cells already present."; Recompute(); return; }

            int total = cells.Count, done = 0;
            bool cancelled = false;

            for (int ci = 0; ci < cells.Count && !cancelled; ci++)
            {
                var (a, b, cond) = cells[ci];
                Func<int, AllCards> mk = factoryFor(cond);
                OpponentModelDefinition dA = byId[a], dB = byId[b];

                BatchConfig cfg = new BatchConfig
                {
                    Games = games,
                    BaseSeed = baseSeed,
                    MaxActions = maxActions,
                    AlternateStart = true,
                    LogEvents = false,
                    MaxParallelGames = parallelGames
                };

                int cell = ci;
                if (parallelGames > 1)
                    EditorUtility.DisplayProgressBar("Ladder",
                        $"{a} vs {b} · {cond}  (cell {cell + 1}/{total}, x{parallelGames})", (cell + 0.5f) / total);

                BatchResult r = BatchRunner.Run(
                    mk, mk, db,
                    seed => dA.CreatePolicy(seed), seed => dB.CreatePolicy(seed),
                    dA.BuildModelInfo(), dB.BuildModelInfo(), cond, cond, cfg,
                    onProgress: (g, tot) =>
                    {
                        bool c = EditorUtility.DisplayCancelableProgressBar("Ladder",
                            $"{a} vs {b} · {cond} · {g}/{tot}  (cell {cell + 1}/{total})",
                            (cell + (float)g / tot) / total);
                        if (c) cancelled = true;
                        return !c;
                    });

                if (cancelled) break;                       // don't store a partial cell
                LadderStore.Append(MatchupsPath, LadderStore.FromBatch(a, b, cond, cfg, r, cardSetRev));
                done++;
            }

            EditorUtility.ClearProgressBar();

            var st = LadderStandings.Generate(LadderDir, anchorId, cardSetRev);
            string top = st.models.Count > 0 ? $" · top: {st.models[0].id} {st.models[0].elo:F0}" : "";
            status = $"Ran {done}/{total} cells{(cancelled ? " (cancelled)" : "")}. " +
                     $"Standings: {st.models.Count} models{top}.\n{LadderDir}";
            Debug.Log("[Ladder] " + status);
        }
        catch (Exception ex)
        {
            status = "Error: " + ex.Message;
            Debug.LogException(ex);
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    private void RunDotNet()
    {
        var specs = new List<AgentSpec>();
        for (int i = 0; i < models.Count; i++) if (modelSel[i]) specs.Add(models[i].ToAgentSpec());
        var conds = SelectedConditions();
        if (specs.Count < 2) { status = "Select at least 2 models."; return; }
        if (conds.Count == 0) { status = "Select at least 1 deck condition."; return; }

        BenchLauncher.Launch(new BenchRunSpec
        {
            mode = "ladder",
            agents = specs,
            decks = conds.ToArray(),
            gamesPerCell = games,
            maxActions = maxActions,
            parallelGames = parallelGames,
            anchorId = anchorId,
            overwrite = overwrite,
            alternateStart = true
        });
        status = $"Launched .NET ladder: {specs.Count} models × {conds.Count} conditions (see terminal).";
    }

    private void Recompute()
    {
        try
        {
            string cardSetRev = new SessionRecord().CardSetRevision;
            var st = LadderStandings.Generate(LadderDir, anchorId, cardSetRev);
            status = $"Recomputed standings: {st.models.Count} models." +
                     (st.models.Count > 0 ? $" Top: {st.models[0].id} {st.models[0].elo:F0}." : "");
        }
        catch (Exception ex) { status = "Error: " + ex.Message; Debug.LogException(ex); }
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
}
