using System.Collections.Generic;

/// <summary>
/// Pure helper service for card keyword operations.
/// This class must not depend on Unity objects.
/// </summary>
public static class KeywordService
{
    /// <summary>Returns whether the card currently has the given keyword.</summary>
    public static bool HasKeyword(CardInstance card, KeywordType keyword)
    {
        return card != null &&
               card.Keywords != null &&
               card.Keywords.Contains(keyword);
    }

    /// <summary>Adds a keyword if not already present (ignores <see cref="KeywordType.None"/>). Returns true if it was added.</summary>
    public static bool AddKeyword(CardInstance card, KeywordType keyword)
    {
        if (card == null)
            return false;

        if (keyword == KeywordType.None)
            return false;

        if (card.Keywords == null)
            card.Keywords = new List<KeywordType>();

        if (card.Keywords.Contains(keyword))
            return false;

        card.Keywords.Add(keyword);
        return true;
    }

    /// <summary>Removes a keyword. Returns true if it was present and removed.</summary>
    public static bool RemoveKeyword(CardInstance card, KeywordType keyword)
    {
        if (card == null || card.Keywords == null)
            return false;

        return card.Keywords.Remove(keyword);
    }

    /// <summary>Removes all keywords from the card (e.g. on Silence).</summary>
    public static void ClearKeywords(CardInstance card)
    {
        if (card == null)
            return;

        if (card.Keywords == null)
            card.Keywords = new List<KeywordType>();

        card.Keywords.Clear();
    }

    /// <summary>Returns the max attacks the card may make per turn (2 with DoubleAttack, otherwise 1).</summary>
    public static int GetMaxAttacksPerTurn(CardInstance card)
    {
        if (HasKeyword(card, KeywordType.DoubleAttack))
            return 2;

        return 1;
    }
}
