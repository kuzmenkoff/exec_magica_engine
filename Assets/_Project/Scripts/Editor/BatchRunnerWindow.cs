using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using System;

/// <summary>
/// Editor window for running headless AI-vs-AI batches: pick the two models and decks, set the
/// run parameters, and execute <see cref="BatchRunner"/>, writing results via
/// <see cref="SessionWriter"/>. Research tooling — not part of the engine API.
/// </summary>
public class BatchRunnerWindow : EditorWindow
{
    private IReadOnlyList<OpponentModelDefinition> models;
    private string[] modelNames;
    private string[] deckResourceNames;

    private int model1, model2 = 1;
    private int deck1, deck2;

    private int games = 100, baseSeed = 1, maxActions = 400;
    private bool alternateStart = true, logEvents = false, oneFilePerSession = false;
    private string outputRoot = "";
    private string lastResult = "";

    private OpponentModelDefinition cloneP, cloneE;
    private SerializedObject soP, soE;
    private int cachedP = -1, cachedE = -1;
    private bool showP = false, showE = false;   // Parameters foldouts collapsed by default
    private Vector2 scroll;
    private string[] deckOptions;

    private int parallelGames = 1;

    /// <summary>Opens the Batch Runner window (menu: EXEC_MAGICA ▸ Run Batch...).</summary>
    [MenuItem("EXEC_MAGICA/Run Batch...")]
    public static void Open() => GetWindow<BatchRunnerWindow>("EXEC_MAGICA Batch");

    private void OnEnable()
    {
        models = OpponentModelCatalog.GetAll();
        List<string> mn = new List<string>();
        foreach (OpponentModelDefinition m in models) mn.Add(string.IsNullOrEmpty(m.DisplayName) ? m.Id : m.DisplayName);
        modelNames = mn.ToArray();

        List<string> dn = new List<string>();
        foreach (TextAsset p in Resources.LoadAll<TextAsset>("CardsInfo/DeckPresets")) dn.Add(p.name);
        deckResourceNames = dn.ToArray();

        List<string> opts = new List<string>(deckResourceNames);
        opts.Add("Player deck");
        opts.Add("Enemy deck");
        opts.Add("Random cards");
        opts.Add("Random preset");
        deckOptions = opts.ToArray();

        if (string.IsNullOrEmpty(outputRoot))
            outputRoot = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Runs");
    }

    private void OnGUI()
    {
        if (modelNames == null || modelNames.Length == 0) { EditorGUILayout.HelpBox("No models in Resources/OpponentModels.", MessageType.Warning); return; }
        if (deckResourceNames == null || deckResourceNames.Length == 0) { EditorGUILayout.HelpBox("No presets in Resources/CardsInfo/DeckPresets.", MessageType.Warning); return; }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Player side", EditorStyles.boldLabel);
        model1 = EditorGUILayout.Popup("Model", model1, modelNames);
        deck1 = EditorGUILayout.Popup("Deck", deck1, deckOptions);
        EnsureClone(ref cloneP, ref soP, ref cachedP, model1);
        showP = EditorGUILayout.Foldout(showP, "Parameters", true);
        if (showP) { EditorGUILayout.BeginVertical(EditorStyles.helpBox); DrawParams(soP); EditorGUILayout.EndVertical(); }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Enemy side", EditorStyles.boldLabel);
        model2 = EditorGUILayout.Popup("Model", model2, modelNames);
        deck2 = EditorGUILayout.Popup("Deck", deck2, deckOptions);
        EnsureClone(ref cloneE, ref soE, ref cachedE, model2);
        showE = EditorGUILayout.Foldout(showE, "Parameters", true);
        if (showE) { EditorGUILayout.BeginVertical(EditorStyles.helpBox); DrawParams(soE); EditorGUILayout.EndVertical(); }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Run settings", EditorStyles.boldLabel);
        games = EditorGUILayout.IntField("Games", games);
        baseSeed = EditorGUILayout.IntField("Base seed", baseSeed);
        maxActions = EditorGUILayout.IntField("Max actions", maxActions);
        parallelGames = EditorGUILayout.IntField("Parallel games", parallelGames);
        alternateStart = EditorGUILayout.Toggle("Alternate start", alternateStart);
        logEvents = EditorGUILayout.Toggle("Save event logs", logEvents);
        oneFilePerSession = EditorGUILayout.Toggle("One file per session", oneFilePerSession);
        outputRoot = EditorGUILayout.TextField("Output root", outputRoot);

        EditorGUILayout.Space();
        if (GUILayout.Button("Run batch")) Run();
        if (GUILayout.Button("Run batch (.NET)")) RunDotNet();
        if (!string.IsNullOrEmpty(lastResult)) EditorGUILayout.HelpBox(lastResult, MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private void EnsureClone(ref OpponentModelDefinition clone, ref SerializedObject so, ref int cached, int selected)
    {
        if (cached == selected && clone != null) return;
        if (clone != null) DestroyImmediate(clone);
        clone = Instantiate(models[selected]);
        clone.hideFlags = HideFlags.DontSave;
        so = new SerializedObject(clone);
        cached = selected;
    }

    private static void DrawParams(SerializedObject so)
    {
        if (so == null) return;
        so.Update();
        SerializedProperty p = so.GetIterator();
        bool enter = true;
        while (p.NextVisible(enter))
        {
            enter = false;
            if (p.name == "m_Script" || p.name == "id" || p.name == "displayName" ||
                p.name == "showInGame" || p.name == "heroAvatarPath" || p.name == "eloRating" ||
                p.name == "rated" || p.name == "description") continue;
            EditorGUILayout.PropertyField(p, true);
        }
        so.ApplyModifiedProperties();
    }

    private void OnDisable()
    {
        if (cloneP != null) DestroyImmediate(cloneP);
        if (cloneE != null) DestroyImmediate(cloneE);
    }

    private void Run()
    {
        try
        {
            AllCards db = LoadDatabase();

            Func<int, AllCards> pFactory = BuildDeckFactory(deck1, db, false, out string pDeckName);
            Func<int, AllCards> eFactory = BuildDeckFactory(deck2, db, true, out string eDeckName);

            OpponentModelDefinition pDef = cloneP;
            OpponentModelDefinition eDef = cloneE;

            BatchConfig cfg = new BatchConfig
            {
                Games = games,
                BaseSeed = baseSeed,
                MaxActions = maxActions,
                AlternateStart = alternateStart,
                LogEvents = logEvents,
                MaxParallelGames = parallelGames
            };

            if (parallelGames > 1)
                EditorUtility.DisplayProgressBar("EXEC_MAGICA Batch", $"Running {games} games (x{parallelGames})...", 0.5f);

            BatchResult result = BatchRunner.Run(
                pFactory, eFactory, db,
                seed => pDef.CreatePolicy(seed),
                seed => eDef.CreatePolicy(seed),
                pDef.BuildModelInfo(), eDef.BuildModelInfo(),
                pDeckName, eDeckName, cfg,
                (done, total) =>
                {
                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        "EXEC_MAGICA Batch", $"Game {done}/{total}", done / (float)total);
                    return !cancel;
                });

            string folder = SessionWriter.WriteRun(outputRoot, result, oneFilePerSession);

            BatchSummary s = result.Summary;
            lastResult =
                $"Done. Player({pDef.Id}) win {s.PlayerWinRate:P1}  CI[{s.PlayerWinRateCiLow:P1}, {s.PlayerWinRateCiHigh:P1}]\n" +
                $"P{s.PlayerWins} / E{s.EnemyWins} / D{s.Draws} / ND{s.NonDecisive}   avgTurns {s.AvgTurns:0.0}   total {s.TotalDurationMs / 1000.0:0.0}s\n{folder}";
            Debug.Log("[BatchRunner] " + lastResult);
        }
        catch (System.Exception ex)
        {
            lastResult = "Error: " + ex.Message;
            Debug.LogException(ex);
        }
        finally { EditorUtility.ClearProgressBar(); }
    }

    private void RunDotNet()
    {
        string pDeck = BenchDeckName(deck1), eDeck = BenchDeckName(deck2);
        if (pDeck == null || eDeck == null)
        { lastResult = ".NET supports preset / random decks only (not saved Player/Enemy decks)."; return; }

        BenchLauncher.Launch(new BenchRunSpec
        {
            mode = "batch",
            agents = new List<AgentSpec> { cloneP.ToAgentSpec(), cloneE.ToAgentSpec() },
            decks = new[] { pDeck, eDeck },
            games = games,
            baseSeed = baseSeed,
            maxActions = maxActions,
            parallelGames = parallelGames,
            alternateStart = alternateStart,
            logEvents = logEvents,
            oneFilePerSession = oneFilePerSession,
            outputRoot = outputRoot
        });
        lastResult = $"Launched .NET batch: {cloneP.Id} ({pDeck}) vs {cloneE.Id} ({eDeck}), {games} games (see terminal).";
    }

    private string BenchDeckName(int idx)
    {
        int P = deckResourceNames.Length;
        if (idx < P) return deckResourceNames[idx];
        if (idx == P + 2) return "RandomDeck";
        if (idx == P + 3) return "RandomPreset";
        return null;   // saved Player/Enemy decks — Unity-only
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

    private static AllCards LoadDeck(string presetResourceName, AllCards db)
    {
        TextAsset ta = Resources.Load<TextAsset>("CardsInfo/DeckPresets/" + presetResourceName);
        DeckPresetDto preset = JsonConvert.DeserializeObject<DeckPresetDto>(ta.text);
        AllCards deck = new AllCards { cards = new List<Card>() };
        if (preset?.Cards != null)
            foreach (DeckEntry e in preset.Cards)
            {
                Card def = db.FindCardDefinition(e.CardId);
                if (def == null) { Debug.LogWarning($"[BatchRunner] Card id {e.CardId} not found in DB."); continue; }
                for (int i = 0; i < e.Count; i++) deck.cards.Add(def.GetDeepCopy());
            }
        return deck;
    }

    private class DeckPresetDto { public string PresetName; public List<DeckEntry> Cards; }
    private class DeckEntry { public int CardId; public int Count; }

    private Func<int, AllCards> BuildDeckFactory(int idx, AllCards db, bool isEnemy, out string name)
    {
        int P = deckResourceNames.Length;
        if (idx < P)
        {
            string preset = deckResourceNames[idx];
            AllCards d = LoadDeck(preset, db);     // fixed preset, shared read-only
            name = preset;
            return _ => d;
        }
        if (idx == P) { name = "PlayerDeck"; AllCards d = LoadSavedDeck("MyDeck.json", db); return _ => d; }
        if (idx == P + 1) { name = "EnemyDeck"; AllCards d = LoadSavedDeck("EnemyDeck.json", db); return _ => d; }

        int offset = isEnemy ? 7919 : 0;           // decorrelate enemy random decks from player
        if (idx == P + 2) { name = "RandomCards"; return seed => RandomDeck(db, seed + offset); }

        name = "RandomPreset";
        return seed => LoadDeck(deckResourceNames[new System.Random(seed + offset).Next(deckResourceNames.Length)], db);
    }

    private static AllCards RandomDeck(AllCards db, int seed)
    {
        System.Random rng = new System.Random(seed);
        List<Card> pool = db.GetCollectibleCards();
        AllCards deck = new AllCards { cards = new List<Card>() };
        Dictionary<int, int> counts = new Dictionary<int, int>();
        int guard = 0;
        while (deck.cards.Count < GameRules.DeckSize && guard++ < 100000 && pool.Count > 0)
        {
            Card c = pool[rng.Next(pool.Count)];
            counts.TryGetValue(c.id, out int n);
            if (n >= GameRules.MaxCopiesPerCardInDeck) continue;   // ≤2 copies
            counts[c.id] = n + 1;
            deck.cards.Add(c.GetDeepCopy());
        }
        return deck;
    }

    private static AllCards LoadSavedDeck(string fileName, AllCards db)
    {
        AllCards deck = new AllCards { cards = new List<Card>() };
        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (!File.Exists(path)) { Debug.LogWarning($"[BatchRunner] Saved deck not found: {path}"); return deck; }

        SavedDeck saved = JsonUtility.FromJson<SavedDeck>(File.ReadAllText(path));
        if (saved?.Cards != null)
            foreach (SavedDeckEntry e in saved.Cards)
            {
                Card def = db.FindCardDefinition(e.CardId);
                if (def == null) continue;
                for (int i = 0; i < e.Count; i++) deck.cards.Add(def.GetDeepCopy());
            }
        return deck;
    }
}
