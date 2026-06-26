using NUnit.Framework;

/// <summary>
/// Tests for EffectEngine Summon and DrawCards effects, plus OnDeath summon
/// chains, exercised through GameEngine.ApplyAction.
/// </summary>
[TestFixture]
public class EffectEngineSummonDrawTests
{
    [Test]
    public void Summon_OnPlay_AddsTokensToField()
    {
        GameState state = GameStateTestFactory.CreateSummonOnPlayScenario();
        CardInstance summoner = state.Player.Hand[0]; // summons 2 tokens on play

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            new GameAction(PlayerSide.Player, GameActionType.PlayCard, summoner.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Player.Field, Has.Member(summoner));
        Assert.That(state.Player.Field.Count, Is.EqualTo(3)); // summoner + 2 tokens
    }

    [Test]
    public void DrawCards_OnPlay_MovesCardFromDeckToHand()
    {
        GameState state = GameStateTestFactory.CreateDrawCardsOnPlayScenario();
        CardInstance drawMinion = state.Player.Hand[0]; // draws 1 card on play
        CardInstance deckCard = state.Player.Deck[0];

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            new GameAction(PlayerSide.Player, GameActionType.PlayCard, drawMinion.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Player.Hand, Has.Member(deckCard));
        Assert.That(state.Player.Deck, Has.No.Member(deckCard));
    }

    [Test]
    public void OnDeath_Summon_TriggersWhenMinionDiesFromSpell()
    {
        GameState state = GameStateTestFactory.CreateOnDeathSummonScenario();
        CardInstance spell = state.Player.Hand[0];       // deals 2 damage
        CardInstance deathMinion = state.Enemy.Field[0]; // HP 2, OnDeath summons 2 tokens

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, deathMinion.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Enemy.Graveyard, Has.Member(deathMinion));
        Assert.That(state.Enemy.Field.Count, Is.EqualTo(2)); // OnDeath summoned tokens
    }
}
