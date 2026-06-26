using NUnit.Framework;

/// <summary>
/// Tests for GameEngine.ApplyAction with AttackCard actions:
/// mutual damage, attack consumption, Provocation blocking, exhausted attackers.
/// </summary>
[TestFixture]
public class GameEngineAttackCardTests
{
    [Test]
    public void AttackCard_DealsMutualDamage()
    {
        GameState state = GameStateTestFactory.CreateAttackCardScenario();
        CardInstance attacker = state.Player.Field[0]; // ATK 3, HP 3
        CardInstance defender = state.Enemy.Field[0];  // ATK 2, HP 2
        int attackerStartHp = attacker.HP;
        int defenderPower = defender.Attack;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, defender.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        // Attacker survives the counter-attack (3 HP - 2 ATK = 1).
        Assert.That(attacker.HP, Is.EqualTo(attackerStartHp - defenderPower));
        // Defender (2 HP) took 3 damage, died and moved to graveyard.
        Assert.That(state.Enemy.Field, Has.No.Member(defender));
        Assert.That(state.Enemy.Graveyard, Has.Member(defender));
    }

    [Test]
    public void AttackCard_ConsumesAttack()
    {
        GameState state = GameStateTestFactory.CreateAttackCardScenario();
        CardInstance attacker = state.Player.Field[0];
        CardInstance defender = state.Enemy.Field[0];

        GameEngine engine = new GameEngine(state);
        engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, defender.InstanceId));

        Assert.That(attacker.RemainingAttacksThisTurn, Is.EqualTo(0));
        Assert.That(attacker.CanAttack, Is.False);
    }

    [Test]
    public void AttackCard_BlockedByProvocation_ReturnsFailure()
    {
        GameState state = GameStateTestFactory.CreateAttackBlockedByProvocationScenario();
        CardInstance attacker = state.Player.Field[0];
        CardInstance normalDefender = state.Enemy.Field[0]; // no Provocation
        // Enemy.Field[1] has Provocation, so hitting the normal minion is illegal.
        int defenderHpBefore = normalDefender.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, normalDefender.InstanceId));

        Assert.That(result.Success, Is.False);
        Assert.That(normalDefender.HP, Is.EqualTo(defenderHpBefore)); // no damage dealt
        Assert.That(attacker.RemainingAttacksThisTurn, Is.EqualTo(1)); // attack not consumed
    }

    [Test]
    public void AttackCard_WithExhaustedAttacker_ReturnsFailure()
    {
        GameState state = GameStateTestFactory.CreateCannotAttackTwiceScenario();
        CardInstance attacker = state.Player.Field[0]; // RemainingAttacks = 1
        CardInstance defender = state.Enemy.Field[0];
        GameEngine engine = new GameEngine(state);

        GameStepResult first = engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, defender.InstanceId));
        GameStepResult second = engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, defender.InstanceId));

        Assert.That(first.Success, Is.True, first.ErrorMessage);
        Assert.That(second.Success, Is.False); // no attacks left
    }
}
