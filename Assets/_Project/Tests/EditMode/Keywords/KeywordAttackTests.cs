using NUnit.Framework;

/// <summary>
/// Tests for attack-capability keywords:
/// DoubleAttack (two attacks per turn, refreshed each turn), Charge (may attack
/// the enemy hero the same turn) and Rush (may attack enemy cards the same turn).
/// </summary>
[TestFixture]
public class KeywordAttackTests
{
    [Test]
    public void DoubleAttack_AllowsTwoAttacks_ThirdIsRejected()
    {
        GameState state = GameStateTestFactory.CreateDoubleAttackCanAttackTwiceScenario();
        CardInstance attacker = state.Player.Field[0];   // DoubleAttack, 2 attacks
        CardInstance targetA = state.Enemy.Field[0];
        CardInstance targetB = state.Enemy.Field[1];

        GameEngine engine = new GameEngine(state);
        GameStepResult first = engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, targetA.InstanceId));
        GameStepResult second = engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, targetB.InstanceId));
        GameStepResult third = engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, targetA.InstanceId));

        Assert.That(first.Success, Is.True, first.ErrorMessage);
        Assert.That(second.Success, Is.True, second.ErrorMessage);
        Assert.That(third.Success, Is.False); // both attacks already spent
        Assert.That(attacker.RemainingAttacksThisTurn, Is.EqualTo(0));
    }

    [Test]
    public void DoubleAttack_RefreshesToTwoNextTurn()
    {
        GameState state = GameStateTestFactory.CreateDoubleAttackRefreshesNextTurnScenario();
        CardInstance attacker = state.Player.Field[0]; // exhausted: 0 attacks left

        GameEngine engine = new GameEngine(state);
        // End the player's turn, then the enemy's turn, to start the player's next turn.
        engine.ApplyAction(GameAction.EndTurn(PlayerSide.Player));
        engine.ApplyAction(GameAction.EndTurn(PlayerSide.Enemy));

        Assert.That(attacker.RemainingAttacksThisTurn, Is.EqualTo(2));
        Assert.That(attacker.CanAttack, Is.True);
    }

    [Test]
    public void Charge_CanAttackHeroSameTurn()
    {
        GameState state = GameStateTestFactory.CreateChargeCanAttackHeroScenario();
        CardInstance attacker = state.Player.Field[0]; // Charge, ATK 4
        int enemyHpBefore = state.Enemy.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackHero(PlayerSide.Player, attacker.InstanceId, PlayerSide.Enemy));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Enemy.HP, Is.EqualTo(enemyHpBefore - attacker.Attack));
    }

    [Test]
    public void Rush_CanAttackCardSameTurn()
    {
        GameState state = GameStateTestFactory.CreateRushCanAttackCardScenario();
        CardInstance attacker = state.Player.Field[0]; // Rush, ATK 3
        CardInstance defender = state.Enemy.Field[0];  // HP 3

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, defender.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        // 3 damage to a 3-HP minion kills it.
        Assert.That(state.Enemy.Field, Has.No.Member(defender));
        Assert.That(state.Enemy.Graveyard, Has.Member(defender));
    }
}
