using System;
using System.Collections.Generic;

/// <summary>
/// Immutable-like card definition loaded from JSON/database.
///
/// This class describes what a card is:
/// title, stats, cost, effects, keywords, artwork path, collectibility.
///
/// It should not contain per-match runtime state such as:
/// InstanceId, current zone, owner, CanAttack, IsPlaced, temporary damage counters.
/// </summary>
[Serializable]
public class CardDefinition
{
    public int id;

    public string Title;
    public string Description;
    public string LogoPath;

    public Card.CardClass Class;

    public int Attack;
    public int HP;
    public int MaxHP;
    public int ManaCost;

    /// <summary>
    /// Determines whether this card can be used in player/enemy decks.
    /// Token or summoned-only cards should have IsCollectible = false.
    /// </summary>
    public bool IsCollectible = true;

    public List<CardEffect> Effects = new List<CardEffect>();
    public List<KeywordType> Keywords = new List<KeywordType>();

    /// <summary>True if this card is a spell.</summary>
    public bool IsSpell
    {
        get { return Class == Card.CardClass.SPELL; }
    }

    /// <summary>True if this card is a creature.</summary>
    public bool IsEntity
    {
        get { return Class == Card.CardClass.ENTITY; }
    }

    /// <summary>True if this card has any on-play effect.</summary>
    public bool HasOnPlayEffects
    {
        get
        {
            return Effects != null &&
                   Effects.Exists(effect => effect != null && effect.Trigger == EffectTrigger.OnPlay);
        }
    }

    /// <summary>Returns whether this definition has the given keyword.</summary>
    public bool HasKeyword(KeywordType keyword)
    {
        return Keywords != null && Keywords.Contains(keyword);
    }

    /// <summary>Resolves whether playing this card needs a target, and of what kind.</summary>
    public PlayTargetRequirement GetPlayTargetRequirement()
    {
        return PlayTargetRequirementResolver.GetRequirement(this);
    }

    /// <summary>Ensures <see cref="MaxHP"/> defaults to <see cref="HP"/> when unspecified.</summary>
    public void NormalizeHealth()
    {
        if (MaxHP <= 0)
            MaxHP = HP;
    }

    /// <summary>Returns an independent copy of this definition.</summary>
    public CardDefinition GetDeepCopy()
    {
        return new CardDefinition
        {
            id = id,

            Title = Title,
            Description = Description,
            LogoPath = LogoPath,

            Class = Class,

            Attack = Attack,
            HP = HP,
            MaxHP = MaxHP,
            ManaCost = ManaCost,

            IsCollectible = IsCollectible,

            Effects = Effects != null
                ? new List<CardEffect>(Effects)
                : new List<CardEffect>(),

            Keywords = Keywords != null
                ? new List<KeywordType>(Keywords)
                : new List<KeywordType>()
        };
    }

    /// <summary>Builds a definition from a legacy <see cref="Card"/> (drops runtime state).</summary>
    public static CardDefinition FromCard(Card card)
    {
        if (card == null)
            return null;

        return new CardDefinition
        {
            id = card.id,

            Title = card.Title,
            Description = card.Description,
            LogoPath = card.LogoPath,

            Class = card.Class,

            Attack = card.Attack,
            HP = card.HP,
            MaxHP = card.MaxHP,
            ManaCost = card.ManaCost,

            IsCollectible = card.IsCollectible,

            Effects = card.Effects != null
                ? new List<CardEffect>(card.Effects)
                : new List<CardEffect>(),

            Keywords = card.Keywords != null
                ? new List<KeywordType>(card.Keywords)
                : new List<KeywordType>()
        };
    }

    /// <summary>Materializes a legacy <see cref="Card"/> from this definition.</summary>
    public Card ToLegacyCard()
    {
        Card card = new Card();

        card.ApplyDefinition(this);

        return card;
    }
}
