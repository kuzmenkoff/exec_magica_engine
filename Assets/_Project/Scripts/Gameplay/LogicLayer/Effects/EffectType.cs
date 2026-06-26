/// <summary>
/// Defines what a card effect does.
/// </summary>
public enum EffectType
{
    /// <summary>
    /// No effect.
    /// </summary>
    None,

    /// <summary>
    /// Deals damage to the selected target.
    /// </summary>
    DealDamage,

    /// <summary>
    /// Heals the selected target.
    /// </summary>
    Heal,

    /// <summary>
    /// Draws cards for the effect owner.
    /// </summary>
    DrawCards,

    /// <summary>
    /// Adds attack to the selected card.
    /// </summary>
    BuffAttack,

    /// <summary>
    /// Adds health to the selected card.
    /// </summary>
    BuffHealth,

    /// <summary>
    /// Increases both attack and health of target cards.
    /// Uses AttackValue and HealthValue from CardEffect.
    /// </summary>
    BuffStats,

    /// <summary>
    /// Reduces attack of the selected card.
    /// </summary>
    DebuffAttack,

    /// <summary>
    /// Removes all abilities from the selected card.
    /// </summary>
    Silence,

    /// <summary>
    /// Destroys the selected card or group of cards.
    /// </summary>
    Destroy,

    /// <summary>
    /// Summons a token card.
    /// </summary>
    Summon,

    /// <summary>
    /// Adds a keyword to the selected card.
    /// Example: Shield, Provocation, Lifesteal.
    /// </summary>
    AddKeyword,

    /// <summary>
    /// Gives temporary mana for the current turn only.
    /// This increases current Mana, but does not increase Manapool.
    /// Mana can go above current Manapool, but not above GameRules.MaxManaPool.
    /// </summary>
    GainTemporaryMana,

    /// <summary>
    /// Gives temporary mana to the selected hero at the start of their next turn.
    /// This does not increase Manapool.
    /// </summary>
    GainTemporaryManaNextTurn,
}
