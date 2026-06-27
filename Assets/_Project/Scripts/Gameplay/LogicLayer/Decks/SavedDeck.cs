using System;
using System.Collections.Generic;

/// <summary>A saved deck: a name plus card-count entries. Serializable persistence format for the deck editor.</summary>
[Serializable]
public class SavedDeck
{
    public string DeckName;
    public List<SavedDeckEntry> Cards = new List<SavedDeckEntry>();
}

/// <summary>One entry in a <see cref="SavedDeck"/>: a card id and how many copies.</summary>
[Serializable]
public class SavedDeckEntry
{
    public int CardId;
    public int Count;
}
