using NUnit.Framework;

/// <summary>
/// Tests for GameEngine.ApplyAction with AttackHero actions:
/// hero damage, lethal / game-over, Provocation blocking and Rush restriction.
/// </summary>
[TestFixture]
public class GameEngineAttackHeroTests
{
    [Test]
    public void AttackHero_DamagesHero()
    {
        GameState state = GameStateTestFactory.CreateAttackHeroScenario();
        CardInstance attacker = state.Player.Field[0]; // ATK 3
        int enemyStartHp = state.Enemy.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackHero(PlayerSide.Player, attacker.InstanceId, PlayerSide.Enemy));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Enemy.HP, Is.EqualTo(enemyStartHp - attacker.Attack));
        Assert.That(attacker.RemainingAttacksThisTurn, Is.EqualTo(0));
    }

    [Test]
    public void AttackHero_LethalDamage_EndsGameWithWinner()
    {
        GameState state = GameStateTestFactory.CreateAttackHeroScenario();
        CardInstance attacker = state.Player.Field[0]; // ATK 3
        state.Enemy.HP = attacker.Attack; // set up an exactly-lethal swing

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackHero(PlayerSide.Player, attacker.InstanceId, PlayerSide.Enemy));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Enemy.HP, Is.EqualTo(0));
        Assert.That(state.IsGameOver, Is.True);
        Assert.That(state.Winner, Is.EqualTo(PlayerSide.Player));
    }

    [Test]
    public void AttackHero_WhileEnemyHasProvocation_ReturnsFailure()
    {
        GameState state = GameStateTestFactory.CreateAttackBlockedByProvocationScenario();
        CardInstance attacker = state.Player.Field[0];
        int enemyStartHp = state.Enemy.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackHero(PlayerSide.Player, attacker.InstanceId, PlayerSide.Enemy));

        Assert.That(result.Success, Is.False);
        Assert.That(state.Enemy.HP, Is.EqualTo(enemyStartHp));        // no damage dealt
        Assert.That(attacker.RemainingAttacksThisTurn, Is.EqualTo(1)); // attack not consumed
    }

    [Test]
    public void AttackHero_WithRush_ReturnsFailure()
    {
        GameState state = GameStateTestFactory.CreateRushCannotAttackHeroScenario();
        CardInstance rusher = state.Player.Field[0]; // Rush: can attack cards only this turn
        int enemyStartHp = state.Enemy.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackHero(PlayerSide.Player, rusher.InstanceId, PlayerSide.Enemy));

        Assert.That(result.Success, Is.False);
        Assert.That(state.Enemy.HP, Is.EqualTo(enemyStartHp));
    }
}
