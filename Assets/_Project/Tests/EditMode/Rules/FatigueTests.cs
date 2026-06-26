using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Phase 1.1a: drawing from an empty deck deals escalating fatigue damage to the drawing
/// player's own hero (1, 2, 3, ...), can be lethal, and the counter is part of the state.
/// </summary>
[TestFixture]
public class FatigueTests
{
    private static GameState EmptyDeckState(int playerHp = 30)
    {
        return new GameState
        {
            TurnNumber = 1,
            ActiveSide = PlayerSide.Player,
            Player = new PlayerState
            {
                Side = PlayerSide.Player,
                HP = playerHp,
                MaxHP = 30,
                Deck = new List<CardInstance>(),
                Hand = new List<CardInstance>()
            },
            Enemy = new PlayerState
            {
                Side = PlayerSide.Enemy,
                HP = 30,
                MaxHP = 30,
                Deck = new List<CardInstance>(),
                Hand = new List<CardInstance>()
            }
        };
    }

    [Test]
    public void Fatigue_FirstEmptyDraw_DealsOneDamage()
    {
        GameState s = EmptyDeckState();
        var events = new List<GameEvent>();
        CardZoneService.DrawCard(s, s.Player, null, events);

        Assert.That(s.Player.FatigueCounter, Is.EqualTo(1));
        Assert.That(s.Player.HP, Is.EqualTo(29));
        Assert.That(s.Player.Hand, Is.Empty);
        Assert.That(events.Exists(e => e.Type == GameEventType.FatigueDamage), Is.True);
    }

    [Test]
    public void Fatigue_Escalates_SecondDrawDealsTwo()
    {
        GameState s = EmptyDeckState();
        CardZoneService.DrawCard(s, s.Player, null, null);   // -1 -> 29
        CardZoneService.DrawCard(s, s.Player, null, null);   // -2 -> 27
        Assert.That(s.Player.FatigueCounter, Is.EqualTo(2));
        Assert.That(s.Player.HP, Is.EqualTo(27));
    }

    [Test]
    public void Fatigue_CounterSurvivesDeepCopy()
    {
        GameState s = EmptyDeckState();
        CardZoneService.DrawCard(s, s.Player, null, null);   // counter 1
        GameState copy = s.GetDeepCopy();

        Assert.That(copy.Player.FatigueCounter, Is.EqualTo(1));
        CardZoneService.DrawCard(s, s.Player, null, null);   // original -> 2, copy unaffected
        Assert.That(copy.Player.FatigueCounter, Is.EqualTo(1));
    }

    [Test]
    public void Fatigue_CanBeLethal_EndsGameWithOpponentWinner()
    {
        GameState s = EmptyDeckState(playerHp: 1);
        CardZoneService.DrawCard(s, s.Player, null, null);   // -1 -> 0

        Assert.That(s.Player.HP, Is.EqualTo(0));
        Assert.That(s.IsGameOver, Is.True);
        Assert.That(s.Winner, Is.EqualTo(PlayerSide.Enemy));
    }
}
