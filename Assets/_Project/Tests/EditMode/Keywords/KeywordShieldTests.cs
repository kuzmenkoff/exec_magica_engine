using NUnit.Framework;

/// <summary>
/// Tests for the Shield keyword: the first instance of damage to the bearer is
/// fully prevented and the keyword is consumed. Covers both combat damage and
/// spell damage paths through EffectEngine.DealDamageToCard.
/// </summary>
[TestFixture]
public class KeywordShieldTests
{
    [Test]
    public void Shield_BlocksAttackDamage_PreventsHpLossAndIsConsumed()
    {
        GameState state = GameStateTestFactory.CreateShieldBlocksAttackDamageScenario();
        CardInstance attacker = state.Player.Field[0];        // ATK 3, HP 3
        CardInstance defender = state.Enemy.Field[0];         // ATK 2, HP 4, Shield
        int defenderHpBefore = defender.HP;
        int attackerHpBefore = attacker.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.AttackCard(PlayerSide.Player, attacker.InstanceId, defender.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        // Shield absorbed the whole attack: HP unchanged, keyword removed.
        Assert.That(defender.HP, Is.EqualTo(defenderHpBefore));
        Assert.That(KeywordService.HasKeyword(defender, KeywordType.Shield), Is.False);
        // The counter-attack is not shielded, so the attacker still takes damage.
        Assert.That(attacker.HP, Is.EqualTo(attackerHpBefore - defender.Attack));
    }

    [Test]
    public void Shield_BlocksSpellDamage_PreventsHpLossAndIsConsumed()
    {
        GameState state = GameStateTestFactory.CreateShieldBlocksSpellDamageScenario();
        CardInstance spell = state.Player.Hand[0];            // deals 3 to a selected enemy card
        CardInstance shieldedEnemy = state.Enemy.Field[0];    // HP 4, Shield
        int hpBefore = shieldedEnemy.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, shieldedEnemy.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        // Shield prevented the spell damage entirely and was consumed.
        Assert.That(shieldedEnemy.HP, Is.EqualTo(hpBefore));
        Assert.That(KeywordService.HasKeyword(shieldedEnemy, KeywordType.Shield), Is.False);
    }
}
