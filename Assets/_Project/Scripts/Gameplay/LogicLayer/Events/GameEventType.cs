/// <summary>
/// Describes what happened inside the pure GameEngine.
/// Unity, logs, replays and datasets can consume these events.
/// </summary>
public enum GameEventType
{
    TurnEnded,
    TurnStarted,

    ManaPoolChanged,
    ManaRestored,
    ManaSpent,

    TemporaryManaGained,
    TemporaryManaNextTurnAdded,

    CardDrawn,
    CardDrawSkippedHandFull,
    CardDrawSkippedEmptyDeck,

    CardPlayed,
    CardMovedToField,
    CardMovedToGraveyard,

    CardSummoned,
    SummonSkippedFieldFull,
    SummonSkippedMissingCardDefinition,

    DamageDealt,
    DamagePrevented,
    ShieldBroken,
    HeroDamaged,
    FatigueDamage,
    CardDied,
    CardDestroyed,

    CardSilenced,

    CardHealed,
    HeroHealed,

    CardAttacked,
    HeroAttacked,
    AttackFailed,

    OnDeathResolved,

    CardStatsBuffed,
    CardStatsDebuffed,
    KeywordAdded,

    EffectResolved,

    GameEnded
}
