/// <summary>
/// Describes what happened inside the pure GameEngine.
/// Unity, logs, replays and datasets can consume these events.
/// </summary>
public enum GameEventType
{
    /// <summary>The active player's turn ended.</summary>
    TurnEnded,
    /// <summary>A new turn began for the active player.</summary>
    TurnStarted,

    /// <summary>The maximum mana pool changed.</summary>
    ManaPoolChanged,
    /// <summary>Current mana was refilled at turn start.</summary>
    ManaRestored,
    /// <summary>Mana was paid for a card or effect.</summary>
    ManaSpent,

    /// <summary>Temporary mana was granted for the current turn.</summary>
    TemporaryManaGained,
    /// <summary>Temporary mana was queued for the next turn.</summary>
    TemporaryManaNextTurnAdded,

    /// <summary>A card moved from deck to hand.</summary>
    CardDrawn,
    /// <summary>A draw was skipped because the hand was full.</summary>
    CardDrawSkippedHandFull,
    /// <summary>A draw was skipped because the deck was empty (triggers fatigue).</summary>
    CardDrawSkippedEmptyDeck,

    /// <summary>A card was played from hand.</summary>
    CardPlayed,
    /// <summary>A card entered the battlefield.</summary>
    CardMovedToField,
    /// <summary>A card moved to the graveyard.</summary>
    CardMovedToGraveyard,

    /// <summary>A token or card was summoned onto the field.</summary>
    CardSummoned,
    /// <summary>A summon was skipped because the field was full.</summary>
    SummonSkippedFieldFull,
    /// <summary>A summon was skipped because the token definition was missing.</summary>
    SummonSkippedMissingCardDefinition,

    /// <summary>Damage was dealt to a card.</summary>
    DamageDealt,
    /// <summary>Incoming damage was prevented (e.g. by Shield).</summary>
    DamagePrevented,
    /// <summary>A Shield absorbed damage and was consumed.</summary>
    ShieldBroken,
    /// <summary>A hero took damage.</summary>
    HeroDamaged,
    /// <summary>A hero took fatigue (empty-deck) damage.</summary>
    FatigueDamage,
    /// <summary>A card's health reached 0 in combat.</summary>
    CardDied,
    /// <summary>A card was removed by a destroy effect (not combat).</summary>
    CardDestroyed,

    /// <summary>A card lost all abilities and keywords.</summary>
    CardSilenced,

    /// <summary>A card was healed.</summary>
    CardHealed,
    /// <summary>A hero was healed.</summary>
    HeroHealed,

    /// <summary>A card attacked another card.</summary>
    CardAttacked,
    /// <summary>A hero was attacked by a card.</summary>
    HeroAttacked,
    /// <summary>An attack was rejected as illegal.</summary>
    AttackFailed,

    /// <summary>A card's OnDeath effects finished resolving.</summary>
    OnDeathResolved,

    /// <summary>A card gained attack and/or health.</summary>
    CardStatsBuffed,
    /// <summary>A card lost attack and/or health.</summary>
    CardStatsDebuffed,
    /// <summary>A keyword was granted to a card.</summary>
    KeywordAdded,

    /// <summary>A card effect finished resolving.</summary>
    EffectResolved,

    /// <summary>The match ended.</summary>
    GameEnded
}
