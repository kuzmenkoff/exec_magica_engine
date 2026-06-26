using NUnit.Framework;

/// <summary>
/// Checks that GameState.GetDeepCopy() returns a fully independent clone.
/// Critical for MCTS: rollouts mutate copies thousands of times and should NOT
/// corrupt the original state.
/// </summary>
[TestFixture]
public class GameStateDeepCopyTests
{
    [Test]
    public void GetDeepCopy_ReturnsDifferentObjectGraph()
    {
        GameState original = GameStateTestFactory.CreateAttackCardScenario();

        GameState copy = original.GetDeepCopy();

        Assert.That(copy, Is.Not.SameAs(original));
        Assert.That(copy.Player, Is.Not.SameAs(original.Player));
        Assert.That(copy.Enemy, Is.Not.SameAs(original.Enemy));
        Assert.That(copy.Player.Field, Is.Not.SameAs(original.Player.Field));
        Assert.That(copy.Player.Field[0], Is.Not.SameAs(original.Player.Field[0]));
        Assert.That(copy.Enemy.Field[0], Is.Not.SameAs(original.Enemy.Field[0]));
    }

    [Test]
    public void GetDeepCopy_MutatingCopy_DoesNotAffectOriginal()
    {
        GameState original = GameStateTestFactory.CreateAttackCardScenario();
        int originalHp = original.Player.HP;
        int originalCardHp = original.Player.Field[0].HP;
        int originalFieldCount = original.Player.Field.Count;

        GameState copy = original.GetDeepCopy();
        copy.Player.HP -= 5;
        copy.Player.Field[0].HP -= 1;
        copy.Player.Field[0].Keywords.Add(KeywordType.Charge);
        copy.Player.Field.Add(copy.Enemy.Field[0]);

        Assert.That(original.Player.HP, Is.EqualTo(originalHp));
        Assert.That(original.Player.Field[0].HP, Is.EqualTo(originalCardHp));
        Assert.That(original.Player.Field[0].Keywords, Has.No.Member(KeywordType.Charge));
        Assert.That(original.Player.Field.Count, Is.EqualTo(originalFieldCount));
    }

    [Test]
    public void GetDeepCopy_MutatingOriginal_DoesNotAffectCopy()
    {
        GameState original = GameStateTestFactory.CreateAttackCardScenario();
        GameState copy = original.GetDeepCopy();
        int copyEnemyHp = copy.Enemy.HP;
        int copyDefenderAttack = copy.Enemy.Field[0].Attack;

        original.Enemy.HP -= 7;
        original.Enemy.Field[0].Attack += 3;

        Assert.That(copy.Enemy.HP, Is.EqualTo(copyEnemyHp));
        Assert.That(copy.Enemy.Field[0].Attack, Is.EqualTo(copyDefenderAttack));
    }

    [Test]
    public void GetDeepCopy_EffectListIsIndependent()
    {
        GameState original = GameStateTestFactory.CreateOnDeathSummonScenario();
        int originalEffectCount = original.Enemy.Field[0].Effects.Count;
        Assume.That(originalEffectCount, Is.GreaterThan(0));

        GameState copy = original.GetDeepCopy();
        copy.Enemy.Field[0].Effects.Clear();

        Assert.That(original.Enemy.Field[0].Effects.Count, Is.EqualTo(originalEffectCount));
    }

    [Test]
    public void GetDeepCopy_CardDatabaseIsSharedByReference()
    {
        GameState original = GameStateTestFactory.CreateAttackCardScenario();

        GameState copy = original.GetDeepCopy();

        Assert.That(copy.CardDatabase, Is.SameAs(original.CardDatabase));
    }
}
