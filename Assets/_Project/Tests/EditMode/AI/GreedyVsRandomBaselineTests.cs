using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Acceptance gate for Phase 2.1: the heuristic Greedy policy must beat Random by a wide
/// margin. Uses a synthetic vanilla-minion mirror so the result reflects policy quality,
/// not card luck. Sides and starting player are alternated and all seeds are fixed, so the
/// run is deterministic. The thesis-grade number (win rate + CI on the real frozen decks)
/// comes later from the Phase 3.1 batch runner; this is the engineering gate.
/// </summary>
[TestFixture]
public class GreedyVsRandomBaselineTests
{
    private static readonly (int id, int atk, int hp, int cost)[] Curve =
    {
        (9001, 1, 2, 1), (9002, 2, 1, 1), (9003, 2, 3, 2), (9004, 3, 2, 2),
        (9005, 3, 4, 3), (9006, 4, 3, 3), (9007, 4, 5, 4), (9008, 5, 4, 4),
        (9009, 5, 6, 5), (9010, 6, 5, 5), (9011, 6, 7, 6), (9012, 7, 6, 6),
        (9013, 7, 8, 7), (9014, 8, 8, 8), (9015, 4, 4, 3),
    };

    private static Card Minion(int id, int atk, int hp, int cost)
    {
        return new Card
        {
            id = id,
            Title = "VM" + id,
            Class = Card.CardClass.ENTITY,
            Attack = atk,
            HP = hp,
            MaxHP = hp,
            ManaCost = cost,
            IsCollectible = true,
            Keywords = new List<KeywordType>(),
            Effects = new List<CardEffect>()
        };
    }

    private static AllCards BuildDeck()
    {
        AllCards deck = new AllCards { cards = new List<Card>() };
        foreach (var c in Curve)
        {
            deck.cards.Add(Minion(c.id, c.atk, c.hp, c.cost)); // 2 copies each -> 30 cards
            deck.cards.Add(Minion(c.id, c.atk, c.hp, c.cost));
        }
        return deck;
    }

    private static AllCards BuildDatabase()
    {
        AllCards db = new AllCards { cards = new List<Card>() };
        foreach (var c in Curve)
            db.cards.Add(Minion(c.id, c.atk, c.hp, c.cost));
        return db;
    }

    [Test]
    public void Greedy_BeatsRandom_AtLeast70Percent()
    {
        const int games = 100;
        AllCards database = BuildDatabase();

        int greedyWins = 0;
        int decisive = 0;

        for (int i = 0; i < games; i++)
        {
            bool greedyIsPlayer = (i % 2) == 0;          // alternate which side Greedy plays
            bool playerFirst = ((i / 2) % 2) == 0;        // alternate starting player independently

            GameState initial = GameStateBuilder.CreateInitialState(
                BuildDeck(), BuildDeck(), database, playerFirst, shuffleSeed: 1000 + i);

            IGameActionPolicy greedy = new GreedyActionPolicy(seed: 7000 + i);
            IGameActionPolicy random = new RandomActionPolicy(seed: 8000 + i);

            IGameActionPolicy playerPolicy = greedyIsPlayer ? greedy : random;
            IGameActionPolicy enemyPolicy = greedyIsPlayer ? random : greedy;

            GameState final = GameSimulationRunner.SimulatePlayout(
                initial, playerPolicy, enemyPolicy, maxActions: 400);

            if (!final.IsGameOver || !final.Winner.HasValue)
                continue; // stall (MaxActionsReached) -> not decisive

            decisive++;
            PlayerSide greedySide = greedyIsPlayer ? PlayerSide.Player : PlayerSide.Enemy;
            if (final.Winner.Value == greedySide)
                greedyWins++;
        }

        Assert.That(decisive, Is.GreaterThan(games / 2),
            $"Too many non-decisive games: only {decisive}/{games} ended with a winner");

        double winRate = (double)greedyWins / decisive;
        TestContext.WriteLine(
            $"Greedy win rate: {winRate:P1}  ({greedyWins}/{decisive} decisive, {games} total games)");
        Assert.That(winRate, Is.GreaterThan(0.70),
            $"Greedy win rate {winRate:P1} over {decisive} decisive games (need > 70%)");
    }
}