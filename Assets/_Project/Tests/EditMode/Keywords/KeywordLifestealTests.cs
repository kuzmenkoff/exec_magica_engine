using NUnit.Framework;

/// <summary>
/// Tests for the Lifesteal keyword: damage dealt by the bearer heals the owner's
/// hero by the same amount. Healing only happens for damage that is actually
/// dealt, so a target's Shield (which prevents the damage) also prevents the heal.
/// </summary>
[TestFixture]
public class KeywordLifestealTests
{
    [Test]
    public void Lifesteal_OnCardAttack_HealsOwnerHero()
    {
        GameState state = GameStateTestFactory.CreateLifestealAttackCardScenario();
        CardInstance attacker = state.Player.Field[0];   // ATK 3, HP 4, Lifesteal
        CardInstance defender = state.Enemy.Field[0];    // ATK 1, HP 5
        int ownerHpBefore = state.Player.HP;             // 24
        int defenderHpBefore = defender.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, defender.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        // Owner healed by the damage dealt to the defender (3).
        Assert.That(state.Player.HP, Is.EqualTo(ownerHpBefore + attacker.Attack));
        Assert.That(defender.HP, Is.EqualTo(defenderHpBefore - attacker.Attack));
    }

    [Test]
    public void Lifesteal_OnHeroAttack_HealsOwnerHero()
    {
        GameState state = GameStateTestFactory.CreateLifestealAttackHeroScenario();
        CardInstance attacker = state.Player.Field[0];   // ATK 4, Lifesteal
        int ownerHpBefore = state.Player.HP;             // 24
        int enemyHpBefore = state.Enemy.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackHero(PlayerSide.Player, attacker.InstanceId, PlayerSide.Enemy));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Enemy.HP, Is.EqualTo(enemyHpBefore - attacker.Attack));
        Assert.That(state.Player.HP, Is.EqualTo(ownerHpBefore + attacker.Attack));
    }

    [Test]
    public void Lifesteal_BlockedByShield_DoesNotHeal()
    {
        GameState state = GameStateTestFactory.CreateLifestealBlockedByShieldScenario();
        CardInstance attacker = state.Player.Field[0];   // ATK 3, HP 4, Lifesteal
        CardInstance defender = state.Enemy.Field[0];    // ATK 1, HP 5, Shield
        int ownerHpBefore = state.Player.HP;             // 24
        int defenderHpBefore = defender.HP;
        int attackerHpBefore = attacker.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, defender.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        // Shield prevented the damage, so no damage was dealt and no healing occurred.
        Assert.That(defender.HP, Is.EqualTo(defenderHpBefore));
        Assert.That(state.Player.HP, Is.EqualTo(ownerHpBefore));
        Assert.That(KeywordService.HasKeyword(defender, KeywordType.Shield), Is.False);
        // The counter-attack still lands on the Lifesteal attacker.
        Assert.That(attacker.HP, Is.EqualTo(attackerHpBefore - defender.Attack));
    }
}
