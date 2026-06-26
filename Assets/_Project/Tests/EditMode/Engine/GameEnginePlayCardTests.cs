using NUnit.Framework;

/// <summary>
/// Tests for GameEngine.ApplyAction with PlayCard actions:
/// entity placement, mana spending, spell-to-graveyard, full field,
/// insufficient mana and missing source card.
/// </summary>
[TestFixture]
public class GameEnginePlayCardTests
{
    [Test]
    public void PlayEntity_MovesCardFromHandToField()
    {
        GameState state = GameStateTestFactory.CreateSummonOnPlayScenario();
        CardInstance summoner = state.Player.Hand[0];

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            new GameAction(PlayerSide.Player, GameActionType.PlayCard, summoner.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Player.Field, Has.Member(summoner));
        Assert.That(state.Player.Hand, Has.No.Member(summoner));
    }

    [Test]
    public void PlayCard_SpendsMana()
    {
        GameState state = GameStateTestFactory.CreateSummonOnPlayScenario();
        CardInstance summoner = state.Player.Hand[0];
        int manaBefore = state.Player.Mana;
        int cost = summoner.ManaCost;

        GameEngine engine = new GameEngine(state);
        engine.ApplyAction(
            new GameAction(PlayerSide.Player, GameActionType.PlayCard, summoner.InstanceId));

        Assert.That(state.Player.Mana, Is.EqualTo(manaBefore - cost));
    }

    [Test]
    public void PlaySpell_MovesToGraveyard()
    {
        GameState state = GameStateTestFactory.CreateDealDamageEnemyCardScenario();
        CardInstance spell = state.Player.Hand[0];
        CardInstance enemyMinion = state.Enemy.Field[0];

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, enemyMinion.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Player.Graveyard, Has.Member(spell));
        Assert.That(state.Player.Hand, Has.No.Member(spell));
    }

    [Test]
    public void PlayCard_NotEnoughMana_ReturnsFailure()
    {
        GameState state = GameStateTestFactory.CreateSummonOnPlayScenario();
        CardInstance summoner = state.Player.Hand[0];
        state.Player.Mana = 0; // cannot afford the card

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            new GameAction(PlayerSide.Player, GameActionType.PlayCard, summoner.InstanceId));

        Assert.That(result.Success, Is.False);
        Assert.That(state.Player.Hand, Has.Member(summoner)); // still in hand
        Assert.That(state.Player.Mana, Is.EqualTo(0));        // nothing spent
    }

    [Test]
    public void PlayEntity_FieldFull_ReturnsFailure()
    {
        GameState state = GameStateTestFactory.CreateSummonOnPlayScenario();
        CardInstance summoner = state.Player.Hand[0];

        for (int i = 0; i < GameRules.MaxCardsOnField; i++)
        {
            state.Player.Field.Add(new CardInstance
            {
                InstanceId = 1000 + i,
                Class = Card.CardClass.ENTITY,
                OwnerSide = PlayerSide.Player,
                Zone = GameZone.Field,
                IsPlaced = true
            });
        }

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            new GameAction(PlayerSide.Player, GameActionType.PlayCard, summoner.InstanceId));

        Assert.That(result.Success, Is.False);
        Assert.That(state.Player.Hand, Has.Member(summoner));
    }

    [Test]
    public void PlayCard_NotInHand_ReturnsFailure()
    {
        GameState state = GameStateTestFactory.CreateSummonOnPlayScenario();

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            new GameAction(PlayerSide.Player, GameActionType.PlayCard, 123456)); // unknown id

        Assert.That(result.Success, Is.False);
    }
}
