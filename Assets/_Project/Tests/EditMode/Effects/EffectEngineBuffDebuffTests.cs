using NUnit.Framework;

/// <summary>
/// Tests for EffectEngine buff, debuff and keyword-granting effects, exercised
/// through GameEngine.ApplyAction (by playing the card that carries the effect).
/// </summary>
[TestFixture]
public class EffectEngineBuffDebuffTests
{
    [Test]
    public void BuffAttack_IncreasesAllyAttack()
    {
        GameState state = GameStateTestFactory.CreateBuffAttackSelectedAllyCardScenario();
        CardInstance spell = state.Player.Hand[0]; // +2 attack
        CardInstance ally = state.Player.Field[0];
        int attackBefore = ally.Attack;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, ally.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(ally.Attack, Is.EqualTo(attackBefore + 2));
    }

    [Test]
    public void BuffHealth_IncreasesAllyHealthAndMaxHealth()
    {
        GameState state = GameStateTestFactory.CreateBuffHealthSelectedAllyCardScenario();
        CardInstance spell = state.Player.Hand[0]; // +2 health
        CardInstance ally = state.Player.Field[0];
        int hpBefore = ally.HP;
        int maxHpBefore = ally.MaxHP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, ally.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(ally.MaxHP, Is.EqualTo(maxHpBefore + 2));
        Assert.That(ally.HP, Is.EqualTo(hpBefore + 2));
    }

    [Test]
    public void BuffStats_IncreasesAllyAttackAndHealth()
    {
        GameState state = GameStateTestFactory.CreateBuffSelectedAllyCardScenario();
        CardInstance buffer = state.Player.Hand[0]; // +1/+1 entity
        CardInstance ally = state.Player.Field[0];
        int attackBefore = ally.Attack;
        int maxHpBefore = ally.MaxHP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, buffer.InstanceId, ally.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(ally.Attack, Is.EqualTo(attackBefore + 1));
        Assert.That(ally.MaxHP, Is.EqualTo(maxHpBefore + 1));
    }

    [Test]
    public void DebuffAttack_ReducesEnemyAttack()
    {
        GameState state = GameStateTestFactory.CreateDebuffAttackSelectedEnemyCardScenario();
        CardInstance spell = state.Player.Hand[0]; // -2 attack
        CardInstance enemy = state.Enemy.Field[0];
        int attackBefore = enemy.Attack;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, enemy.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(enemy.Attack, Is.EqualTo(attackBefore - 2));
    }

    [Test]
    public void DebuffAttack_DoesNotGoBelowZero()
    {
        GameState state = GameStateTestFactory.CreateDebuffAttackSelectedEnemyCardScenario();
        CardInstance spell = state.Player.Hand[0]; // -2 attack
        CardInstance enemy = state.Enemy.Field[0];
        enemy.Attack = 1; // smaller than the debuff amount

        GameEngine engine = new GameEngine(state);
        engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, enemy.InstanceId));

        Assert.That(enemy.Attack, Is.EqualTo(0)); // clamped at zero
    }

    [Test]
    public void AddKeyword_GrantsKeywordToAlly()
    {
        GameState state = GameStateTestFactory.CreateAddKeywordSelectedAllyCardScenario();
        CardInstance giver = state.Player.Hand[0]; // grants Provocation
        CardInstance ally = state.Player.Field[0];

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, giver.InstanceId, ally.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(KeywordService.HasKeyword(ally, KeywordType.Provocation), Is.True);
    }
}
