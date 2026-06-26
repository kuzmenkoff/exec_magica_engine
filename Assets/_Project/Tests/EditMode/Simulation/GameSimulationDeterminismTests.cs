using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Tier 6 — full-playout determinism.
/// A seeded random playout must be reproducible (same seed -> same result),
/// and the runner must not mutate the caller's initial state (it plays on an
/// internal deep copy). Both are prerequisites for reproducible AI-vs-AI batches
/// (Phase 3) and for MCTS reusing a root state across rollouts.
/// </summary>
[TestFixture]
public class GameSimulationDeterminismTests
{
    // Non-zero seed: RandomActionPolicy treats seed 0 as "unseeded" (non-deterministic).
    private const int Seed = 12345;
    private const int MaxActions = 200;

    private static int[] InstanceIds(List<CardInstance> zone)
    {
        return zone.Select(card => card.InstanceId).ToArray();
    }

    [Test]
    public void SimulateRandomPlayout_SameSeed_ProducesIdenticalScalarOutcome()
    {
        GameState state = GameStateTestFactory.CreateDoubleAttackCanAttackTwiceScenario();

        GameState a = GameSimulationRunner.SimulateRandomPlayout(state, MaxActions, Seed);
        GameState b = GameSimulationRunner.SimulateRandomPlayout(state, MaxActions, Seed);

        Assert.That(a.IsGameOver, Is.EqualTo(b.IsGameOver));
        Assert.That(a.Winner, Is.EqualTo(b.Winner));
        Assert.That(a.TurnNumber, Is.EqualTo(b.TurnNumber));
        Assert.That(a.ActionIndex, Is.EqualTo(b.ActionIndex));
        Assert.That(a.ActiveSide, Is.EqualTo(b.ActiveSide));
        Assert.That(a.Player.HP, Is.EqualTo(b.Player.HP));
        Assert.That(a.Enemy.HP, Is.EqualTo(b.Enemy.HP));
        // Guard against a trivially-empty playout: actions were actually applied.
        Assert.That(a.ActionIndex, Is.GreaterThan(0));
    }

    [Test]
    public void SimulateRandomPlayout_SameSeed_ProducesIdenticalBoardComposition()
    {
        GameState state = GameStateTestFactory.CreateDoubleAttackCanAttackTwiceScenario();

        GameState a = GameSimulationRunner.SimulateRandomPlayout(state, MaxActions, Seed);
        GameState b = GameSimulationRunner.SimulateRandomPlayout(state, MaxActions, Seed);

        CollectionAssert.AreEqual(InstanceIds(a.Player.Field), InstanceIds(b.Player.Field));
        CollectionAssert.AreEqual(InstanceIds(a.Enemy.Field), InstanceIds(b.Enemy.Field));
        CollectionAssert.AreEqual(InstanceIds(a.Player.Hand), InstanceIds(b.Player.Hand));
        CollectionAssert.AreEqual(InstanceIds(a.Enemy.Hand), InstanceIds(b.Enemy.Hand));
        CollectionAssert.AreEqual(InstanceIds(a.Player.Graveyard), InstanceIds(b.Player.Graveyard));
        CollectionAssert.AreEqual(InstanceIds(a.Enemy.Graveyard), InstanceIds(b.Enemy.Graveyard));
    }

    [Test]
    public void SimulateRandomPlayout_DoesNotMutateInitialState()
    {
        GameState state = GameStateTestFactory.CreateDoubleAttackCanAttackTwiceScenario();
        int playerHpBefore = state.Player.HP;
        int enemyHpBefore = state.Enemy.HP;
        int playerFieldBefore = state.Player.Field.Count;
        int enemyFieldBefore = state.Enemy.Field.Count;
        int actionIndexBefore = state.ActionIndex;
        int turnBefore = state.TurnNumber;
        bool gameOverBefore = state.IsGameOver;

        GameSimulationRunner.SimulateRandomPlayout(state, MaxActions, Seed);

        // The runner plays on an internal deep copy; the caller's state is untouched.
        Assert.That(state.Player.HP, Is.EqualTo(playerHpBefore));
        Assert.That(state.Enemy.HP, Is.EqualTo(enemyHpBefore));
        Assert.That(state.Player.Field.Count, Is.EqualTo(playerFieldBefore));
        Assert.That(state.Enemy.Field.Count, Is.EqualTo(enemyFieldBefore));
        Assert.That(state.ActionIndex, Is.EqualTo(actionIndexBefore));
        Assert.That(state.TurnNumber, Is.EqualTo(turnBefore));
        Assert.That(state.IsGameOver, Is.EqualTo(gameOverBefore));
    }
}
