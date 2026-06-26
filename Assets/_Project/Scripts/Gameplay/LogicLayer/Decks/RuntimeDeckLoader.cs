using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Runtime deck resolution for the setup panel. Mirrors the Editor BatchRunnerWindow
/// deck logic (presets / random cards / random preset) so menu-launched runs match
/// research runs. Saved "Player deck"/"Enemy deck" are resolved by the panel via
/// DecksManagerScr (already synced with the DB), not here.
/// </summary>
public static class RuntimeDeckLoader
{
    private const string PresetsPath = "CardsInfo/DeckPresets";
    private static List<string> cachedPresetNames;

    public static IReadOnlyList<string> GetPresetNames()
    {
        if (cachedPresetNames == null)
        {
            cachedPresetNames = new List<string>();
            foreach (TextAsset ta in Resources.LoadAll<TextAsset>(PresetsPath))
                if (ta != null) cachedPresetNames.Add(ta.name);
            cachedPresetNames.Sort(StringComparer.Ordinal);   // stable dropdown order
        }
        return cachedPresetNames;
    }

    public static AllCards LoadPreset(string presetName, AllCards db)
    {
        AllCards deck = new AllCards { cards = new List<Card>() };

        TextAsset ta = Resources.Load<TextAsset>(PresetsPath + "/" + presetName);
        if (ta == null) { Debug.LogWarning($"[RuntimeDeckLoader] Preset not found: {presetName}"); return deck; }

        DeckPresetDto preset = JsonConvert.DeserializeObject<DeckPresetDto>(ta.text);
        if (preset?.Cards != null)
            foreach (DeckEntry e in preset.Cards)
            {
                Card def = db.FindCardDefinition(e.CardId);
                if (def == null) { Debug.LogWarning($"[RuntimeDeckLoader] Card id {e.CardId} not in DB."); continue; }
                for (int i = 0; i < e.Count; i++) deck.cards.Add(def.GetDeepCopy());
            }
        return deck;
    }

    public static AllCards RandomDeck(AllCards db, int seed)
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

    public static AllCards RandomPreset(AllCards db, int seed)
    {
        IReadOnlyList<string> names = GetPresetNames();
        if (names.Count == 0) return new AllCards { cards = new List<Card>() };
        string pick = names[new System.Random(seed).Next(names.Count)];
        return LoadPreset(pick, db);
    }

    private class DeckPresetDto { public string PresetName; public List<DeckEntry> Cards; }
    private class DeckEntry { public int CardId; public int Count; }
}
