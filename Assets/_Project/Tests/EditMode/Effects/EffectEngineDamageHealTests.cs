using NUnit.Framework;

/// <summary>
/// Tests for EffectEngine damage and healing effects, exercised through
/// GameEngine.ApplyAction (by playing the spell that carries the effect).
/// </summary>
[TestFixture]
public class EffectEngineDamageHealTests
{
    [Test]
    public void DealDamage_KillsTargetedEnemyCard()
    {
        GameState state = GameStateTestFactory.CreateDealDamageEnemyCardScenario();
        CardInstance spell = state.Player.Hand[0];       // deals 2 damage
        CardInstance enemyMinion = state.Enemy.Field[0]; // HP 2

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, enemyMinion.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Enemy.Field, Has.No.Member(enemyMinion));
        Assert.That(state.Enemy.Graveyard, Has.Member(enemyMinion));
    }

    [Test]
    public void DealDamage_DamagesEnemyHero()
    {
        GameState state = GameStateTestFactory.CreateDealDamageEnemyHeroScenario();
        CardInstance spell = state.Player.Hand[0]; // deals 3 to hero
        int enemyStartHp = state.Enemy.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnHero(PlayerSide.Player, spell.InstanceId, PlayerSide.Enemy));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Enemy.HP, Is.EqualTo(enemyStartHp - 3));
    }

    [Test]
    public void Heal_RestoresAllyCardHealth()
    {
        GameState state = GameStateTestFactory.CreateHealAllyCardScenario();
        CardInstance spell = state.Player.Hand[0]; // heals 2
        CardInstance ally = state.Player.Field[0]; // HP 1, MaxHP 4
        int hpBefore = ally.HP;

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, ally.InstanceId));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(ally.HP, Is.EqualTo(hpBefore + 2));
    }

    [Test]
    public void Heal_DoesNotExceedMaxHealth()
    {
        GameState state = GameStateTestFactory.CreateHealAllyCardScenario();
        CardInstance spell = state.Player.Hand[0]; // heals 2
        CardInstance ally = state.Player.Field[0]; // MaxHP 4
        ally.HP = ally.MaxHP;                       // already full

        GameEngine engine = new GameEngine(state);
        engine.ApplyAction(
            GameAction.PlayCardOnCard(PlayerSide.Player, spell.InstanceId, ally.InstanceId));

        Assert.That(ally.HP, Is.EqualTo(ally.MaxHP)); // clamped, no overheal
    }

    [Test]
    public void Heal_RestoresAllyHeroHealth()
    {
        GameState state = GameStateTestFactory.CreateHealAllyHeroScenario();
        CardInstance spell = state.Player.Hand[0]; // heals 4
        int hpBefore = state.Player.HP;            // 24

        GameEngine engine = new GameEngine(state);
        GameStepResult result = engine.ApplyAction(
            GameAction.PlayCardOnHero(PlayerSide.Player, spell.InstanceId, PlayerSide.Player));

        Assert.That(result.Success, Is.True, result.ErrorMessage);
        Assert.That(state.Player.HP, Is.EqualTo(hpBefore + 4));
    }
}
