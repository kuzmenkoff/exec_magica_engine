using NUnit.Framework;

/// <summary>
/// Tests that lock the frozen rule set (Phase 1.3 relies on these constants
/// staying fixed) and validate deck-size checking.
/// </summary>
[TestFixture]
public class GameRulesTests
{
    [Test]
    public void RuleConstants_AreFrozenToSpecification()
    {
        Assert.That(GameRules.MaxCardsOnField, Is.EqualTo(7));
        Assert.That(GameRules.MaxCardsOnHand, Is.EqualTo(10));
        Assert.That(GameRules.FirstPlayerStartingHandSize, Is.EqualTo(3));
        Assert.That(GameRules.SecondPlayerStartingHandSize, Is.EqualTo(4));
        Assert.That(GameRules.StartingHeroHealth, Is.EqualTo(30));
        Assert.That(GameRules.StartingManaPool, Is.EqualTo(1));
        Assert.That(GameRules.MaxManaPool, Is.EqualTo(10));
        Assert.That(GameRules.ManaIncreasePerTurn, Is.EqualTo(1));
        Assert.That(GameRules.MaxCopiesPerCardInDeck, Is.EqualTo(2));
        Assert.That(GameRules.DeckSize, Is.EqualTo(30));
    }

    [Test]
    public void IsValidDeckSize_TrueForExactly30()
    {
        Assert.That(GameRules.IsValidDeckSize(30), Is.True);
    }

    [Test]
    public void IsValidDeckSize_FalseForOtherCounts()
    {
        Assert.That(GameRules.IsValidDeckSize(29), Is.False);
        Assert.That(GameRules.IsValidDeckSize(31), Is.False);
        Assert.That(GameRules.IsValidDeckSize(0), Is.False);
    }
}
