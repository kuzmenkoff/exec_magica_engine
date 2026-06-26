/// <summary>
/// Defines what kind of target a card requires when played.
/// 
/// This is used by the action system to generate and validate PlayCard actions.
/// </summary>
public enum PlayTargetRequirement
{
    /// <summary>
    /// The card does not require a selected target.
    /// </summary>
    None,

    /// <summary>
    /// The card requires one allied card as a target.
    /// </summary>
    AllyCard,

    /// <summary>
    /// The card requires one enemy card as a target.
    /// </summary>
    EnemyCard,

    /// <summary>
    /// The card requires one allied character as a target:
    /// allied card or allied hero.
    /// </summary>
    AllyCharacter,

    /// <summary>
    /// The card requires one enemy character as a target:
    /// enemy card or enemy hero.
    /// </summary>
    EnemyCharacter
}
