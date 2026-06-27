using System;
using System.Collections.Generic;

/// <summary>
/// Legacy Unity card view/runtime model.
///
/// Important:
/// - GameEngine must use CardInstance, not Card.
/// - CardDefinition describes immutable card data loaded from JSON.
/// - Card is kept temporarily because Unity UI, deck editor and prefabs still expect it.
///
/// Later this class should be reduced or replaced by a dedicated UnityCardViewModel.
/// </summary>
[Serializable]
public class Card
{
    /// <summary>Whether a card is a board creature (ENTITY) or a one-shot SPELL.</summary>
    public enum CardClass
    {
        /// <summary>A creature summoned to the battlefield that can attack.</summary>
        ENTITY,
        /// <summary>A one-shot card that resolves its effects and never enters the field.</summary>
        SPELL
    }

    // ---------------------------------------------------------------------
    // Definition data
    // ---------------------------------------------------------------------

    public int id;

    public string Title;
    public string Description;
    public string LogoPath;

    public CardClass Class;

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

    // ---------------------------------------------------------------------
    // Legacy runtime/view state
    // ---------------------------------------------------------------------

    public bool CanAttack;

    /// <summary>
    /// Runtime flag. If true, this card can attack cards but cannot attack hero this turn.
    /// Usually set by Rush when the card is played.
    /// </summary>
    public bool CanAttackOnlyCardsThisTurn;

    public bool IsPlaced;

    public int InstanceId { get; set; }

    public int TimesTookDamage;
    public int TimesDealedDamage;

    // ---------------------------------------------------------------------
    // Definition-like helpers
    // ---------------------------------------------------------------------

    /// <summary>True if this card is a spell (resolves and never stays on the field).</summary>
    public bool IsSpell
    {
        get { return Class == CardClass.SPELL; }
    }

    /// <summary>True if this card is a creature that enters the battlefield.</summary>
    public bool IsEntity
    {
        get { return Class == CardClass.ENTITY; }
    }

    /// <summary>True if this card has the Provocation (taunt) keyword.</summary>
    public bool IsProvocation
    {
        get { return HasKeyword(KeywordType.Provocation); }
    }

    /// <summary>True if this card has any effect that triggers on play.</summary>
    public bool HasOnPlayEffects
    {
        get
        {
            return Effects != null &&
                   Effects.Exists(effect => effect != null && effect.Trigger == EffectTrigger.OnPlay);
        }
    }

    /// <summary>Returns whether this card currently has the given keyword.</summary>
    public bool HasKeyword(KeywordType keyword)
    {
        return Keywords != null && Keywords.Contains(keyword);
    }

    /// <summary>Adds a keyword if not already present (ignores <see cref="KeywordType.None"/>).</summary>
    public void AddKeyword(KeywordType keyword)
    {
        if (keyword == KeywordType.None)
            return;

        if (Keywords == null)
            Keywords = new List<KeywordType>();

        if (!Keywords.Contains(keyword))
            Keywords.Add(keyword);
    }

    /// <summary>Removes all keywords (e.g. on Silence).</summary>
    public void ClearKeywords()
    {
        if (Keywords == null)
            Keywords = new List<KeywordType>();
        else
            Keywords.Clear();
    }

    /// <summary>Resolves whether playing this card needs a target, and of what kind.</summary>
    public PlayTargetRequirement GetPlayTargetRequirement()
    {
        return PlayTargetRequirementResolver.GetRequirement(this);
    }

    /// <summary>Ensures <see cref="MaxHP"/> is set, defaulting it to <see cref="HP"/> when unspecified.</summary>
    public void NormalizeHealth()
    {
        if (MaxHP <= 0)
            MaxHP = HP;
    }

    // ---------------------------------------------------------------------
    // Legacy runtime helpers
    // ---------------------------------------------------------------------

    /// <summary>Restores health up to <see cref="MaxHP"/> (no-op for non-positive amounts).</summary>
    public void Heal(int amount)
    {
        NormalizeHealth();

        if (amount <= 0)
            return;

        HP = Math.Min(HP + amount, MaxHP);
    }

    /// <summary>
    /// Deals damage to this legacy card and returns the actual damage dealt.
    /// Shield prevents damage and returns 0.
    ///
    /// New gameplay logic should use EffectEngine/CardInstance instead.
    /// </summary>
    public int GetDamage(int dmg)
    {
        if (dmg <= 0)
            return 0;

        if (HasKeyword(KeywordType.Shield))
        {
            Keywords.Remove(KeywordType.Shield);
            return 0;
        }

        int actualDamage = Math.Min(dmg, HP);
        HP -= dmg;

        return actualDamage;
    }

    /// <summary>Assigns a fresh runtime instance id.</summary>
    public void AssignNewInstanceId()
    {
        InstanceId = Guid.NewGuid().GetHashCode();
    }

    /// <summary>True while HP is above zero.</summary>
    public bool IsAlive()
    {
        return HP > 0;
    }

    // ---------------------------------------------------------------------
    // Definition conversion
    // ---------------------------------------------------------------------

    /// <summary>Projects this card into an immutable <see cref="CardDefinition"/>.</summary>
    public CardDefinition ToDefinition()
    {
        return CardDefinition.FromCard(this);
    }

    /// <summary>Copies all definition fields from a <see cref="CardDefinition"/> onto this card.</summary>
    public void ApplyDefinition(CardDefinition definition)
    {
        if (definition == null)
            return;

        id = definition.id;

        Title = definition.Title;
        Description = definition.Description;
        LogoPath = definition.LogoPath;

        Class = definition.Class;

        Attack = definition.Attack;
        HP = definition.HP;
        MaxHP = definition.MaxHP;
        ManaCost = definition.ManaCost;

        IsCollectible = definition.IsCollectible;

        Effects = definition.Effects != null
            ? new List<CardEffect>(definition.Effects)
            : new List<CardEffect>();

        Keywords = definition.Keywords != null
            ? new List<KeywordType>(definition.Keywords)
            : new List<KeywordType>();

        NormalizeHealth();
    }

    // ---------------------------------------------------------------------
    // Copy
    // ---------------------------------------------------------------------

    /// <summary>Returns a deep copy (alias of <see cref="GetDeepCopy"/>).</summary>
    public Card GetCopy()
    {
        return GetDeepCopy();
    }

    /// <summary>Returns an independent deep copy including runtime state.</summary>
    public Card GetDeepCopy()
    {
        Card card = new Card();

        card.id = id;

        card.Title = Title;
        card.Description = Description;
        card.LogoPath = LogoPath;

        card.Class = Class;

        card.Attack = Attack;
        card.HP = HP;
        card.MaxHP = MaxHP;
        card.ManaCost = ManaCost;

        card.IsCollectible = IsCollectible;

        card.Effects = Effects != null
            ? new List<CardEffect>(Effects)
            : new List<CardEffect>();

        card.Keywords = Keywords != null
            ? new List<KeywordType>(Keywords)
            : new List<KeywordType>();

        card.CanAttack = CanAttack;
        card.CanAttackOnlyCardsThisTurn = CanAttackOnlyCardsThisTurn;
        card.IsPlaced = IsPlaced;

        card.InstanceId = InstanceId;

        card.TimesTookDamage = TimesTookDamage;
        card.TimesDealedDamage = TimesDealedDamage;

        return card;
    }
}
