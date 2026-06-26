using NUnit.Framework;

/// <summary>
/// Tests for EffectEngine Destroy and Silence effects, including their
/// interaction with OnDeath triggers, exercised through GameEngine.ApplyAction.
/// </summary>
[TestFixture]
public class EffectEngineDestroySilenceTests
{
    [Test]
    public void Destroy_KillsTargetedEnemyCard()
    {
        GameState state = GameStateTestFactory.CreateDestroyEnemyCardScenario();
        CardInstance spell = state.Player.Hand[0];
        CardInstance enemy = state.Enemy.Field[0];

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, enemy.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Enemy.Field, Has.No.Member(enemy));
        Assert.That(state.Enemy.Graveyard, Has.Member(enemy));
    }

    [Test]
    public void Destroy_TriggersOnDeathSummon()
    {
        GameState state = GameStateTestFactory.CreateDestroyEnemyCardWithOnDeathScenario();
        CardInstance spell = state.Player.Hand[0];
        CardInstance egg = state.Enemy.Field[0]; // dies into 2 summoned tokens

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, egg.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Enemy.Graveyard, Has.Member(egg));
        Assert.That(state.Enemy.Field.Count, Is.EqualTo(2)); // OnDeath summoned tokens
    }

    [Test]
    public void Silence_RemovesKeywordsAndEffects()
    {
        GameState state = GameStateTestFactory.CreateSilenceEnemyCardScenario();
        CardInstance spell = state.Player.Hand[0];
        CardInstance enemy = state.Enemy.Field[0]; // has Provocation, Rush and an OnDeath effect

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, enemy.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(enemy.Keywords, Is.Empty);
        Assert.That(enemy.Effects, Is.Empty);
    }

    [Test]
    public void Silence_RemovesOnDeathSoDestroyDoesNotSummon()
    {
        GameState state = GameStateTestFactory.CreateSilenceEnemyCardRemovesOnDeathScenario();
        CardInstance silence = state.Player.Hand[0];
        CardInstance destroy = state.Player.Hand[1];
        CardInstance egg = state.Enemy.Field[0];

        GameEngine engine = new GameEngine(state);
        GameStepResult silenceResult = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, silence.InstanceId, egg.InstanceId));
        GameStepResult destroyResult = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, destroy.InstanceId, egg.InstanceId));

        Assert.That(silenceResult.Success, Is.True, silenceResult.ErrorMessage);
        Assert.That(destroyResult.Success, Is.True, destroyResult.ErrorMessage);
        Assert.That(state.Enemy.Graveyard, Has.Member(egg));
        Assert.That(state.Enemy.Field, Is.Empty); // OnDeath was silenced: nothing summoned
    }
}
