using System;
using System.Collections.Generic;

/// <summary>
/// A card as it exists during a match — a concrete instance with its own id, current
/// stats, combat flags and zone. Unlike <see cref="CardDefinition"/>, it carries per-match
/// runtime state and lives inside a <see cref="PlayerState"/> zone.
/// </summary>
[Serializable]
public class CardInstance
{
    /// <summary>Unique id of this instance within the match.</summary>
    public int InstanceId;
    /// <summary>Definition id (shared by all copies of the same card).</summary>
    public int CardId;

    public string Title;
    public Card.CardClass Class;

    public int Attack;
    public int HP;
    public int MaxHP;
    public int ManaCost;

    /// <summary>Whether this card may attack this turn.</summary>
    public bool CanAttack;
    /// <summary>If true, may attack cards but not the enemy hero this turn (Rush).</summary>
    public bool CanAttackOnlyCardsThisTurn;
    /// <summary>Attacks remaining this turn (e.g. 2 for DoubleAttack).</summary>
    public int RemainingAttacksThisTurn;

    public bool IsPlaced;
    /// <summary>True once silenced — abilities and keywords are stripped.</summary>
    public bool IsSilenced;

    public PlayerSide OwnerSide;
    public GameZone Zone;

    /// <summary>Last field slot index occupied; -1 if never placed (stable ordering for AI/UI).</summary>
    public int LastKnownFieldIndex = -1;

    public List<KeywordType> Keywords = new List<KeywordType>();
    public List<CardEffect> Effects = new List<CardEffect>();

    /// <summary>True if this is a spell card.</summary>
    public bool IsSpell => Class == Card.CardClass.SPELL;
    /// <summary>True if this is a creature card.</summary>
    public bool IsEntity => Class == Card.CardClass.ENTITY;

    /// <summary>Returns an independent deep copy of this card instance.</summary>
    public CardInstance GetDeepCopy()
    {
        return new CardInstance
        {
            InstanceId = InstanceId,
            CardId = CardId,
            Title = Title,
            Class = Class,

            Attack = Attack,
            HP = HP,
            MaxHP = MaxHP,
            ManaCost = ManaCost,

            CanAttack = CanAttack,
            CanAttackOnlyCardsThisTurn = CanAttackOnlyCardsThisTurn,
            RemainingAttacksThisTurn = RemainingAttacksThisTurn,

            IsPlaced = IsPlaced,
            IsSilenced = IsSilenced,

            OwnerSide = OwnerSide,
            Zone = Zone,

            LastKnownFieldIndex = LastKnownFieldIndex,

            Keywords = Keywords != null
                ? new List<KeywordType>(Keywords)
                : new List<KeywordType>(),

            Effects = Effects != null
                ? new List<CardEffect>(Effects)
                : new List<CardEffect>()
        };
    }
}
