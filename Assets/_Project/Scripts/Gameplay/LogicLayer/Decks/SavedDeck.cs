using System;
using System.Collections.Generic;

[Serializable]
public class SavedDeck
{
    public string DeckName;
    public List<SavedDeckEntry> Cards = new List<SavedDeckEntry>();
}

[Serializable]
public class SavedDeckEntry
{
    public int CardId;
    public int Count;
}
