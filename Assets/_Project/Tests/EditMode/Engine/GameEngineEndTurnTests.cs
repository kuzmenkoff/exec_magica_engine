using NUnit.Framework;

/// <summary>
/// Tests for GameEngine.ApplyAction with EndTurn actions:
/// side switching, counters, mana growth/restore, card draw and rejections.
/// </summary>
[TestFixture]
public class GameEngineEndTurnTests
{
    [Test]
    public void EndTurn_SwitchesActiveSideAndAdvancesCounters()
    {
        GameState state = GameStateTestFactory.CreateAttackCardScenario(); // active: Player
        int turnNumberBefore = state.TurnNumber;
        int actionIndexBefore = state.ActionIndex;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(GameAction.EndTurn(PlayerSide.Player));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.ActiveSide, Is.EqualTo(PlayerSide.Enemy));
        Assert.That(state.TurnNumber, Is.EqualTo(turnNumberBefore + 1));
        Assert.That(state.ActionIndex, Is.EqualTo(actionIndexBefore + 1));
    }

    [Test]
    public void EndTurn_RestoresNextPlayerMana()
    {
        GameState state = GameStateTestFactory.CreateAttackCardScenario();
        state.Enemy.Mana = 2; // simulate spent mana

        GameEngine engine = new GameEngine(state);
        engine.ApplyAction(GameAction.EndTurn(PlayerSide.Player));

        // On their turn start the next player's mana is refilled from the pool.
        Assert.That(state.Enemy.Mana, Is.EqualTo(state.Enemy.ManaPool));
    }

    [Test]
    public void EndTurn_GrowsNextPlayerManaPool()
    {
        GameState state = GameStateTestFactory.CreateAttackCardScenario();
        state.Enemy.TurnsStarted = 1; // already had a personal turn
        state.Enemy.ManaPool = 3;

        GameEngine engine = new GameEngine(state);
        engine.ApplyAction(GameAction.EndTurn(PlayerSide.Player));

        Assert.That(state.Enemy.ManaPool, Is.EqualTo(3 + GameRules.ManaIncreasePerTurn));
    }

    [Test]
    public void EndTurn_DrawsCardForNextPlayer()
    {
        GameState state = GameStateTestFactory.CreateAttackCardScenario();
        CardInstance deckCard = new CardInstance
        {
            InstanceId = 9999,
            CardId = 1,
            Title = "Deck Card",
            Class = Card.CardClass.ENTITY,
            OwnerSide = PlayerSide.Enemy,
            Zone = GameZone.Deck
        };
        state.Enemy.Deck.Add(deckCard);

        GameEngine engine = new GameEngine(state);
        engine.ApplyAction(GameAction.EndTurn(PlayerSide.Player));

        Assert.That(state.Enemy.Hand, Has.Member(deckCard));
        Assert.That(state.Enemy.Deck, Has.No.Member(deckCard));
    }

    [Test]
    public void ApplyAction_WrongActorSide_ReturnsFailure()
    {
        GameState state = GameStateTestFactory.CreateAttackCardScenario(); // active: Player

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(GameAction.EndTurn(PlayerSide.Enemy));

        Assert.That(result.Success, Is.False);
        Assert.That(state.ActiveSide, Is.EqualTo(PlayerSide.Player)); // unchanged
    }

    [Test]
    public void ApplyAction_WhenGameOver_ReturnsFailure()
    {
        GameState state = GameStateTestFactory.CreateAttackCardScenario();
        state.IsGameOver = true;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(GameAction.EndTurn(PlayerSide.Player));

        Assert.That(result.Success, Is.False);
    }
}
