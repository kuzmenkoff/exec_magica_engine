using System;
using System.Collections.Generic;

[Serializable]
public class CardInstance
{
    public int InstanceId;
    public int CardId;

    public string Title;
    public Card.CardClass Class;

    public int Attack;
    public int HP;
    public int MaxHP;
    public int ManaCost;

    public bool CanAttack;
    public bool CanAttackOnlyCardsThisTurn;
    public int RemainingAttacksThisTurn;

    public bool IsPlaced;
    public bool IsSilenced;

    public PlayerSide OwnerSide;
    public GameZone Zone;

    public int LastKnownFieldIndex = -1;

    public List<KeywordType> Keywords = new List<KeywordType>();
    public List<CardEffect> Effects = new List<CardEffect>();

    public bool IsSpell => Class == Card.CardClass.SPELL;
    public bool IsEntity => Class == Card.CardClass.ENTITY;

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
