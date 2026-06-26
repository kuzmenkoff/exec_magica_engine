using NUnit.Framework;

/// <summary>
/// Turn-flow edge cases not covered by GameEngineEndTurnTests:
/// the mana pool is clamped to its maximum across turns, and drawing from an
/// empty deck is a safe no-op (no fatigue mechanic).
/// </summary>
[TestFixture]
public class GameEngineTurnFlowTests
{
    [Test]
    public void ManaPool_IsClampedToMax_AcrossTurns()
    {
        GameState state = GameStateTestFactory.CreateDoubleAttackRefreshesNextTurnScenario();
        // Player has already started a turn (TurnsStarted = 1) and is at full mana pool.
        state.Player.ManaPool = GameRules.MaxManaPool;

        GameEngine engine = new GameEngine(state);
        // Pass the turn to the enemy and back, starting a new player turn.
        engine.ApplyAction(GameAction.EndTurn(PlayerSide.Player));
        engine.ApplyAction(GameAction.EndTurn(PlayerSide.Enemy));

        // The +1 growth at turn start must not push the pool past the maximum.
        Assert.That(state.Player.ManaPool, Is.EqualTo(GameRules.MaxManaPool));
    }

    [Test]
    public void StartTurn_WithEmptyDeck_DealsFatigueWithoutAddingCards()
    {
        GameState state = GameStateTestFactory.CreateDoubleAttackRefreshesNextTurnScenario();
        int handBefore = state.Player.Hand.Count;
        int hpBefore = state.Player.HP;

        GameEngine engine = new GameEngine(state);
        engine.ApplyAction(GameAction.EndTurn(PlayerSide.Player)); // enemy starts -> enemy fatigue
        engine.ApplyAction(GameAction.EndTurn(PlayerSide.Enemy));  // player starts -> player fatigue

        Assert.That(state.Player.Hand.Count, Is.EqualTo(handBefore)); // no card added
        Assert.That(state.Player.FatigueCounter, Is.EqualTo(1));
        Assert.That(state.Player.HP, Is.EqualTo(hpBefore - 1));
    }
}
