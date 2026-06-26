/// <summary>
/// Central gameplay rule configuration.
/// This class should be used by GameManagerScr, AI, simulations and future pure game engine logic.
/// </summary>
public static class GameRules
{
    /// <summary>
    /// Maximum number of cards a player can have on the battlefield.
    /// </summary>
    public const int MaxCardsOnField = 7;

    /// <summary>
    /// Maximum number of cards a player can hold in hand.
    /// </summary>
    public const int MaxCardsOnHand = 10;

    /// <summary>
    /// Starting hand size for the first player.
    /// </summary>
    public const int FirstPlayerStartingHandSize = 3;

    /// <summary>
    /// Starting hand size for the second player.
    /// </summary>
    public const int SecondPlayerStartingHandSize = 4;

    /// <summary>
    /// Starting hero health.
    /// </summary>
    public const int StartingHeroHealth = 30;

    /// <summary>
    /// Starting mana crystals.
    /// </summary>
    public const int StartingManaPool = 1;

    /// <summary>
    /// Maximum mana crystals.
    /// </summary>
    public const int MaxManaPool = 10;

    /// <summary>
    /// How much mana pool increases at the start of a player's turn.
    /// </summary>
    public const int ManaIncreasePerTurn = 1;

    /// <summary>
    /// Number of copies of the same collectible card allowed in a deck.
    /// </summary>
    public const int MaxCopiesPerCardInDeck = 2;

    /// <summary>
    /// Deck size.
    /// </summary>
    public const int DeckSize = 30;

    /// <summary>
    /// Returns true if deck has the required number of cards.
    /// </summary>
    public static bool IsValidDeckSize(int cardCount)
    {
        return cardCount == DeckSize;
    }

    /// <summary>
    /// If enabled, the second player receives a Hearthstone-like Coin card.
    /// </summary>
    public static readonly bool GiveCoinToSecondPlayer = true;

    /// <summary>
    /// Card id of the Coin card in the card database.
    /// </summary>
    public const int CoinCardId = 100000;
}
