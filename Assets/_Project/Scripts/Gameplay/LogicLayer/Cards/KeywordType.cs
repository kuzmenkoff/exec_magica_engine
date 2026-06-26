/// <summary>
/// Defines persistent card keywords.
/// 
/// Keywords are passive properties of cards, unlike effects,
/// which are triggered by specific events such as OnPlay or OnDeath.
/// </summary>
public enum KeywordType
{
    None,

    /// <summary>
    /// Enemy cards and hero cannot be attacked while this card is on the battlefield.
    /// </summary>
    Provocation,

    /// <summary>
    /// The next damage taken by this card is prevented.
    /// </summary>
    Shield,

    /// <summary>
    /// This card can attack immediately after being played.
    /// </summary>
    Charge,

    /// <summary>
    /// This card can attack enemy cards immediately after being played,
    /// but cannot attack the enemy hero on the same turn.
    //// </summary>
    Rush,

    /// <summary>
    /// This card can attack twice during one turn.
    /// </summary>
    DoubleAttack,

    /// <summary>
    /// When this card deals damage, its owner hero is healed.
    /// </summary>
    Lifesteal
}
