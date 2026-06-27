using System;
using System.Collections.Generic;

/// <summary>
/// Represents either a saved deck or the full card database.
///
/// Saved decks use the "cards" list.
/// Main card database can use structured lists:
/// entities, spells and summonEntities.
/// </summary>
[Serializable]
public class AllCards
{
    private static readonly System.Random shuffleRandom = new System.Random();

    public string setName = "";
    public int orderPriority = 0;

    // Used by saved player/enemy decks.
    public List<Card> cards = new List<Card>();

    // Used by the main AllCards.json database.
    public List<Card> entities = new List<Card>();
    public List<Card> spells = new List<Card>();
    public List<Card> summonEntities = new List<Card>();

    /// <summary>
    /// Prevents empty structured lists from being written into deck JSON files.
    /// Newtonsoft.Json calls these methods automatically.
    /// </summary>
    public bool ShouldSerializecards()
    {
        return cards != null && cards.Count > 0;
    }

    public bool ShouldSerializeentities()
    {
        return entities != null && entities.Count > 0;
    }

    public bool ShouldSerializespells()
    {
        return spells != null && spells.Count > 0;
    }

    public bool ShouldSerializesummonEntities()
    {
        return summonEntities != null && summonEntities.Count > 0;
    }

    public bool ShouldSerializesetName()
    {
        return !string.IsNullOrWhiteSpace(setName);
    }

    public bool ShouldSerializeorderPriority()
    {
        return orderPriority != 0;
    }

    /// <summary>
    /// Returns every card known to this collection.
    /// For the main database, this includes entities, spells and summon-only entities.
    /// For saved decks, this returns the cards list.
    /// </summary>
    public List<Card> GetAllCards()
    {
        var result = new List<Card>();

        if (cards != null && cards.Count > 0)
            result.AddRange(cards);

        if (entities != null && entities.Count > 0)
            result.AddRange(entities);

        if (spells != null && spells.Count > 0)
            result.AddRange(spells);

        if (summonEntities != null && summonEntities.Count > 0)
            result.AddRange(summonEntities);

        return result;
    }

    /// <summary>
    /// Adds card definitions from another card collection into this collection.
    /// Used to merge multiple card set JSON files into one runtime database.
    /// </summary>
    public void AddCardSet(AllCards source)
    {
        if (source == null)
            return;

        if (source.entities != null)
        {
            foreach (Card card in source.entities)
            {
                if (card != null)
                    entities.Add(card);
            }
        }

        if (source.spells != null)
        {
            foreach (Card card in source.spells)
            {
                if (card != null)
                    spells.Add(card);
            }
        }

        if (source.summonEntities != null)
        {
            foreach (Card card in source.summonEntities)
            {
                if (card != null)
                    summonEntities.Add(card);
            }
        }

        // Backward compatibility if some old file still uses "cards".
        if (source.cards != null)
        {
            foreach (Card card in source.cards)
            {
                if (card != null)
                    cards.Add(card);
            }
        }
    }

    /// <summary>
    /// Returns only cards that can be used in player/enemy decks.
    /// Summon-only cards are intentionally excluded.
    /// </summary>
    public List<Card> GetCollectibleCards()
    {
        var result = new List<Card>();

        if (entities != null && entities.Count > 0)
            result.AddRange(entities);

        if (spells != null && spells.Count > 0)
            result.AddRange(spells);

        // Backward compatibility for old flat cards format.
        if ((entities == null || entities.Count == 0) &&
            (spells == null || spells.Count == 0) &&
            cards != null)
        {
            result.AddRange(cards);
        }

        return result.FindAll(card => card != null && card.IsCollectible);
    }

    /// <summary>
    /// Finds any card definition by id, including summon-only cards.
    /// </summary>
    public Card FindCardDefinition(int cardId)
    {
        return GetAllCards().Find(card => card != null && card.id == cardId);
    }

    /// <summary>Finds a card by id and returns it as a <see cref="CardDefinition"/>, or null.</summary>
    public CardDefinition FindDefinition(int cardId)
    {
        Card card = FindCardDefinition(cardId);

        return card != null
            ? card.ToDefinition()
            : null;
    }

    /// <summary>Returns every card in the collection as definitions.</summary>
    public List<CardDefinition> GetAllDefinitions()
    {
        List<CardDefinition> result = new List<CardDefinition>();

        foreach (Card card in GetAllCards())
        {
            if (card != null)
                result.Add(card.ToDefinition());
        }

        return result;
    }

    /// <summary>Returns deck-legal (collectible) cards as definitions.</summary>
    public List<CardDefinition> GetCollectibleDefinitions()
    {
        List<CardDefinition> result = new List<CardDefinition>();

        foreach (Card card in GetCollectibleCards())
        {
            if (card != null)
                result.Add(card.ToDefinition());
        }

        return result;
    }

    /// <summary>True if a card with the same id exists in this collection.</summary>
    public bool ContainsCard(Card checkedCard)
    {
        if (checkedCard == null)
            return false;

        foreach (Card card in GetAllCards())
        {
            if (card != null && card.id == checkedCard.id)
                return true;
        }

        return false;
    }

    /// <summary>Returns an independent deep copy of the whole collection.</summary>
    public AllCards GetDeepCopy()
    {
        var clone = new AllCards();

        if (cards != null)
        {
            foreach (var card in cards)
            {
                if (card != null)
                    clone.cards.Add(card.GetDeepCopy());
            }
        }

        if (entities != null)
        {
            foreach (var card in entities)
            {
                if (card != null)
                    clone.entities.Add(card.GetDeepCopy());
            }
        }

        if (spells != null)
        {
            foreach (var card in spells)
            {
                if (card != null)
                    clone.spells.Add(card.GetDeepCopy());
            }
        }

        if (summonEntities != null)
        {
            foreach (var card in summonEntities)
            {
                if (card != null)
                    clone.summonEntities.Add(card.GetDeepCopy());
            }
        }

        return clone;
    }

    /// <summary>In-place Fisher–Yates shuffle of the deck's <c>cards</c> list.</summary>
    public void Shuffle()
    {
        if (cards == null)
            return;

        int n = cards.Count;

        while (n > 1)
        {
            n--;
            int k = shuffleRandom.Next(n + 1);

            Card value = cards[k];
            cards[k] = cards[n];
            cards[n] = value;
        }
    }
}
