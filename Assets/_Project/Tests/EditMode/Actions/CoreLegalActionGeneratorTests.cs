using System.Linq;
using NUnit.Framework;

/// <summary>
/// Tests for CoreLegalActionGenerator.GetLegalActions: the set of actions offered
/// to a decision policy. Correctness here is foundational for every AI model
/// (Random/Greedy/MCTS) that picks a move from this list.
/// </summary>
[TestFixture]
public class CoreLegalActionGeneratorTests
{
    [Test]
    public void GetLegalActions_AlwaysIncludesExactlyOneEndTurn()
    {
        GameState state = GameStateTestFactory.CreateAttackCardScenario();

        var actions = CoreLegalActionGenerator.GetLegalActions(state);

        Assert.That(actions.Count(a => a.Type == GameActionType.EndTurn), Is.EqualTo(1));
    }

    [Test]
    public void GetLegalActions_WhenGameOver_ReturnsEmpty()
    {
        GameState state = GameStateTestFactory.CreateAttackCardScenario();
        state.IsGameOver = true;

        var actions = CoreLegalActionGenerator.GetLegalActions(state);

        Assert.That(actions, Is.Empty);
    }

    [Test]
    public void GetLegalActions_ReadyAttacker_OffersAttackCardAndAttackHero()
    {
        GameState state = GameStateTestFactory.CreateAttackCardScenario();
        CardInstance attacker = state.Player.Field[0];
        CardInstance defender = state.Enemy.Field[0];

        var actions = CoreLegalActionGenerator.GetLegalActions(state);

        Assert.That(actions.Any(a =>
            a.Type == GameActionType.AttackCard &&
            a.SourceInstanceId == attacker.InstanceId &&
            a.TargetInstanceId == defender.InstanceId), Is.True, "AttackCard missing");

        Assert.That(actions.Any(a =>
            a.Type == GameActionType.AttackHero &&
            a.SourceInstanceId == attacker.InstanceId &&
            a.TargetHeroSide == PlayerSide.Enemy), Is.True, "AttackHero missing");
    }

    [Test]
    public void GetLegalActions_EnemyProvocation_RestrictsToTauntAndBlocksHero()
    {
        GameState state = GameStateTestFactory.CreateAttackBlockedByProvocationScenario();
        CardInstance attacker = state.Player.Field[0];
        CardInstance normalDefender = state.Enemy.Field[0];     // no Provocation
        CardInstance provocationDefender = state.Enemy.Field[1]; // Provocation

        var actions = CoreLegalActionGenerator.GetLegalActions(state);

        // The only legal attack target is the Provocation minion.
        Assert.That(actions.Any(a =>
            a.Type == GameActionType.AttackCard &&
            a.TargetInstanceId == provocationDefender.InstanceId), Is.True, "Taunt target missing");
        Assert.That(actions.Any(a =>
            a.Type == GameActionType.AttackCard &&
            a.TargetInstanceId == normalDefender.InstanceId), Is.False, "Non-taunt target offered");
        // Hero is unreachable while Provocation stands.
        Assert.That(actions.Any(a => a.Type == GameActionType.AttackHero), Is.False, "Hero attack offered");
    }

    [Test]
    public void GetLegalActions_RushAttacker_OffersCardAttackButNotHero()
    {
        GameState state = GameStateTestFactory.CreateRushCanAttackCardScenario();
        CardInstance attacker = state.Player.Field[0]; // Rush: CanAttackOnlyCardsThisTurn = true
        CardInstance defender = state.Enemy.Field[0];

        var actions = CoreLegalActionGenerator.GetLegalActions(state);

        Assert.That(actions.Any(a =>
            a.Type == GameActionType.AttackCard &&
            a.SourceInstanceId == attacker.InstanceId &&
            a.TargetInstanceId == defender.InstanceId), Is.True, "Card attack missing");
        Assert.That(actions.Any(a =>
            a.Type == GameActionType.AttackHero &&
            a.SourceInstanceId == attacker.InstanceId), Is.False, "Rush should not reach the hero");
    }

    [Test]
    public void GetLegalActions_TargetedSpell_OffersPlayOnEnemyCardNotHero()
    {
        GameState state = GameStateTestFactory.CreateShieldBlocksSpellDamageScenario();
        CardInstance spell = state.Player.Hand[0];        // targets a selected enemy card
        CardInstance enemyCard = state.Enemy.Field[0];

        var actions = CoreLegalActionGenerator.GetLegalActions(state);

        Assert.That(actions.Any(a =>
            a.Type == GameActionType.PlayCard &&
            a.SourceInstanceId == spell.InstanceId &&
            a.TargetType == PlayTargetType.Card &&
            a.TargetInstanceId == enemyCard.InstanceId), Is.True, "Spell-on-card play missing");
        // An EnemyCard requirement must not offer the hero as a target.
        Assert.That(actions.Any(a =>
            a.Type == GameActionType.PlayCard &&
            a.SourceInstanceId == spell.InstanceId &&
            a.TargetType == PlayTargetType.Hero), Is.False, "Spell should not target the hero");
    }

    [Test]
    public void GetLegalActions_UnaffordableCard_IsNotOffered()
    {
        GameState state = GameStateTestFactory.CreateShieldBlocksSpellDamageScenario();
        CardInstance spell = state.Player.Hand[0];
        state.Player.Mana = 0; // cannot pay for the spell

        var actions = CoreLegalActionGenerator.GetLegalActions(state);

        Assert.That(actions.Any(a =>
            a.Type == GameActionType.PlayCard &&
            a.SourceInstanceId == spell.InstanceId), Is.False, "Unaffordable card was offered");
    }

    [Test]
    public void GetLegalActions_ExhaustedBoardAndEmptyHand_OnlyEndTurn()
    {
        GameState state = GameStateTestFactory.CreateDoubleAttackRefreshesNextTurnScenario();
        // Attacker is exhausted (0 attacks), hand is empty, enemy field is empty.

        var actions = CoreLegalActionGenerator.GetLegalActions(state);

        Assert.That(actions.Count, Is.EqualTo(1));
        Assert.That(actions[0].Type, Is.EqualTo(GameActionType.EndTurn));
    }
}
