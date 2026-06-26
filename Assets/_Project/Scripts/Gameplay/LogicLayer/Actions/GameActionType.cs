/// <summary>
/// Represents all possible high-level action types that can happen during a turn.
/// This enum does not execute any logic by itself.
/// It only describes what kind of action the player or AI wants to perform.
/// </summary>
public enum GameActionType
{
    /// <summary>
    /// Play a card from hand.
    /// This can be either a creature card or a spell card.
    /// If the card is targeted, the target is stored inside GameAction.
    /// </summary>
    PlayCard,

    /// <summary>
    /// Attack an enemy card with one of your placed cards.
    /// </summary>
    AttackCard,

    /// <summary>
    /// Attack the enemy hero directly with one of your placed cards.
    /// </summary>
    AttackHero,

    /// <summary>
    /// Finish the current player's turn.
    /// </summary>
    EndTurn
}
