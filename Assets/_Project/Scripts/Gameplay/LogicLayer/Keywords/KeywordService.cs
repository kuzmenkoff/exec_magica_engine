using System.Collections.Generic;

/// <summary>
/// Pure helper service for card keyword operations.
/// This class must not depend on Unity objects.
/// </summary>
public static class KeywordService
{
    public static bool HasKeyword(CardInstance card, KeywordType keyword)
    {
        return card != null &&
               card.Keywords != null &&
               card.Keywords.Contains(keyword);
    }

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

    public static bool RemoveKeyword(CardInstance card, KeywordType keyword)
    {
        if (card == null || card.Keywords == null)
            return false;

        return card.Keywords.Remove(keyword);
    }

    public static void ClearKeywords(CardInstance card)
    {
        if (card == null)
            return;

        if (card.Keywords == null)
            card.Keywords = new List<KeywordType>();

        card.Keywords.Clear();
    }

    public static int GetMaxAttacksPerTurn(CardInstance card)
    {
        if (HasKeyword(card, KeywordType.DoubleAttack))
            return 2;

        return 1;
    }
}
