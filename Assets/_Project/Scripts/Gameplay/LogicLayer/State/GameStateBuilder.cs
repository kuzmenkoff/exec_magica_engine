using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// Builds initial pure GameState objects from deck definitions.
/// 
/// This class does not create Unity objects and does not depend on scenes,
/// transforms, prefabs, coroutines or UI.
/// </summary>
public static class GameStateBuilder
{
    /// <summary>
    /// Builds a ready-to-play <see cref="GameState"/> from two decks: creates both players,
    /// fills and shuffles decks, draws starting hands, gives the Coin to the second player,
    /// and starts the first turn.
    /// </summary>
    /// <param name="playerDeck">The Player side's deck (an <see cref="AllCards"/> <c>cards</c> list).</param>
    /// <param name="enemyDeck">The Enemy side's deck.</param>
    /// <param name="cardDatabase">Full card database — used for summons and the Coin lookup.</param>
    /// <param name="playerFirst">True if the Player side takes the first turn.</param>
    /// <param name="shuffleSeed">Optional seed for reproducible shuffles; null = non-deterministic.</param>
    public static GameState CreateInitialState(
        AllCards playerDeck,
        AllCards enemyDeck,
        AllCards cardDatabase,
        bool playerFirst,
        int? shuffleSeed = null)
    {
        GameState state = new GameState
        {
            TurnNumber = 1,
            ActionIndex = 0,
            ActiveSide = playerFirst ? PlayerSide.Player : PlayerSide.Enemy,
            IsGameOver = false,
            Winner = null,
            NextInstanceId = 1,

            CardDatabase = cardDatabase,

            Player = CreatePlayerState(PlayerSide.Player),
            Enemy = CreatePlayerState(PlayerSide.Enemy)
        };

        FillDeckFromAllCards(state, state.Player, playerDeck);
        FillDeckFromAllCards(state, state.Enemy, enemyDeck);

        ShuffleStartingDecks(state, shuffleSeed);

        DrawStartingHands(state, playerFirst);

        GiveCoinToSecondPlayer(state, cardDatabase, playerFirst);

        StartInitialTurn(state);

        return state;
    }

    private static PlayerState CreatePlayerState(PlayerSide side)
    {
        return new PlayerState
        {
            Side = side,

            HP = GameRules.StartingHeroHealth,
            MaxHP = GameRules.StartingHeroHealth,

            Mana = GameRules.StartingManaPool,
            ManaPool = GameRules.StartingManaPool,
            PendingTemporaryMana = 0,

            TurnsStarted = 0,

            Deck = new List<CardInstance>(),
            Hand = new List<CardInstance>(),
            Field = new List<CardInstance>(),
            Graveyard = new List<CardInstance>()
        };
    }

    private static void FillDeckFromAllCards(
        GameState state,
        PlayerState player,
        AllCards deck)
    {
        if (state == null || player == null || deck == null || deck.cards == null)
            return;

        foreach (Card definition in deck.cards)
        {
            if (definition == null)
                continue;

            CardInstance instance = CardInstanceFactory.Create(
                definition,
                state.GenerateInstanceId(),
                player.Side,
                GameZone.Deck
            );

            if (instance != null)
                player.Deck.Add(instance);
        }
    }

    private static void DrawStartingHands(
        GameState state,
        bool playerFirst)
    {
        int playerStartingHandSize = playerFirst
            ? GameRules.FirstPlayerStartingHandSize
            : GameRules.SecondPlayerStartingHandSize;

        int enemyStartingHandSize = playerFirst
            ? GameRules.SecondPlayerStartingHandSize
            : GameRules.FirstPlayerStartingHandSize;

        DrawCards(state, state.Player, playerStartingHandSize);
        DrawCards(state, state.Enemy, enemyStartingHandSize);
    }

    private static void DrawCards(GameState state, PlayerState player, int count)
    {
        if (state == null || player == null || count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            CardZoneService.DrawCard(
                state,
                player,
                null,
                null
            );
        }
    }

    private static void GiveCoinToSecondPlayer(
        GameState state,
        AllCards cardDatabase,
        bool playerFirst)
    {
        if (!GameRules.GiveCoinToSecondPlayer)
            return;

        if (state == null || cardDatabase == null)
            return;

        PlayerSide secondPlayerSide = playerFirst
            ? PlayerSide.Enemy
            : PlayerSide.Player;

        PlayerState secondPlayer = state.GetPlayerState(secondPlayerSide);

        if (secondPlayer == null)
            return;

        if (secondPlayer.Hand.Count >= GameRules.MaxCardsOnHand)
            return;

        Card coinDefinition = cardDatabase.FindCardDefinition(GameRules.CoinCardId);

        if (coinDefinition == null)
            return;

        CardInstance coin = CardInstanceFactory.Create(
            coinDefinition,
            state.GenerateInstanceId(),
            secondPlayerSide,
            GameZone.Hand
        );

        if (coin != null)
            secondPlayer.Hand.Add(coin);
    }

    private static void StartInitialTurn(GameState state)
    {
        if (state == null)
            return;

        PlayerState activePlayer = state.GetPlayerState(state.ActiveSide);

        if (activePlayer == null)
            return;

        activePlayer.TurnsStarted = 1;

        CardZoneService.DrawCard(
            state,
            activePlayer,
            null,
            null
        );
    }

    private static void ShuffleStartingDecks(GameState state, int? shuffleSeed)
    {
        if (state == null)
            return;

        Random random = shuffleSeed.HasValue
            ? new Random(shuffleSeed.Value)
            : new Random(Guid.NewGuid().GetHashCode());

        ShuffleDeck(state.Player.Deck, random);
        ShuffleDeck(state.Enemy.Deck, random);
    }

    private static void ShuffleDeck(List<CardInstance> deck, Random random)
    {
        if (deck == null || random == null)
            return;

        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);

            CardInstance temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }
}
