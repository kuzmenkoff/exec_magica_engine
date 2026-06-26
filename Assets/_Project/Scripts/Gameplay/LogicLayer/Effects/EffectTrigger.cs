/// <summary>
/// Defines when a card effect should be triggered.
/// </summary>
public enum EffectTrigger
{
    /// <summary>
    /// Effect is triggered when the card is played.
    /// For spells, this is the default trigger.
    /// </summary>
    OnPlay,

    /// <summary>
    /// Effect is triggered when the card dies.
    /// </summary>
    OnDeath,

    /// <summary>
    /// Effect is triggered at the beginning of the owner's turn.
    /// </summary>
    OnTurnStart,

    /// <summary>
    /// Effect is triggered after this card deals damage.
    /// </summary>
    OnDamageDeal,

    /// <summary>
    /// Effect is triggered after this card takes damage.
    /// </summary>
    OnDamageTake
}
