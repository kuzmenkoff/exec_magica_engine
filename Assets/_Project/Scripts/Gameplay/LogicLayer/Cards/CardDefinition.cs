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

    public bool IsSpell
    {
        get { return Class == Card.CardClass.SPELL; }
    }

    public bool IsEntity
    {
        get { return Class == Card.CardClass.ENTITY; }
    }

    public bool HasOnPlayEffects
    {
        get
        {
            return Effects != null &&
                   Effects.Exists(effect => effect != null && effect.Trigger == EffectTrigger.OnPlay);
        }
    }

    public bool HasKeyword(KeywordType keyword)
    {
        return Keywords != null && Keywords.Contains(keyword);
    }

    public PlayTargetRequirement GetPlayTargetRequirement()
    {
        return PlayTargetRequirementResolver.GetRequirement(this);
    }

    public void NormalizeHealth()
    {
        if (MaxHP <= 0)
            MaxHP = HP;
    }

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

    public Card ToLegacyCard()
    {
        Card card = new Card();

        card.ApplyDefinition(this);

        return card;
    }
}
