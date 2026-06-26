/// <summary>
/// Defines what target or group of targets an effect should affect.
/// </summary>
public enum EffectTarget
{
    /// <summary>
    /// No target is required.
    /// </summary>
    None,

    /// <summary>
    /// The card that owns this effect.
    /// </summary>
    SelfCard,

    /// <summary>
    /// The owner hero.
    /// </summary>
    SelfHero,

    /// <summary>
    /// The opponent hero.
    /// </summary>
    EnemyHero,

    /// <summary>
    /// One allied card selected by the player or AI.
    /// </summary>
    SelectedAllyCard,

    /// <summary>
    /// One enemy card selected by the player or AI.
    /// </summary>
    SelectedEnemyCard,

    /// <summary>
    /// One allied character selected by the player or AI.
    /// This can be an allied card or the allied hero.
    /// </summary>
    SelectedAllyCharacter,

    /// <summary>
    /// One enemy character selected by the player or AI.
    /// This can be an enemy card or the enemy hero.
    /// </summary>
    SelectedEnemyCharacter,

    /// <summary>
    /// All allied cards on the battlefield.
    /// </summary>
    AllAllyCards,

    /// <summary>
    /// All allied cards except the card that owns this effect.
    /// </summary>
    OtherAllyCards,

    /// <summary>
    /// All enemy cards on the battlefield.
    /// </summary>
    AllEnemyCards,

    /// <summary>
    /// All cards on both battlefields.
    /// </summary>
    AllCards
}
