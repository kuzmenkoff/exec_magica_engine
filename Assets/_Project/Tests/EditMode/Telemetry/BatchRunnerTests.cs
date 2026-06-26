using System.Linq;
using NUnit.Framework;

[TestFixture]
public class BatchRunnerTests
{
    [Test]
    public void Run_GreedyVsRandom_ProducesWellFormedSummary()
    {
        AllCards db = SyntheticDecks.Database();
        BatchConfig cfg = new BatchConfig { Games = 30, BaseSeed = 1, MaxActions = 400, AlternateStart = true, LogEvents = false };

        BatchResult r = BatchRunner.Run(
            _ => SyntheticDecks.Deck(), _ => SyntheticDecks.Deck(), db,
            seed => new GreedyActionPolicy(seed),
            seed => new RandomActionPolicy(seed + 100000),
            new ModelInfo { ModelId = "Greedy" }, new ModelInfo { ModelId = "Random" },
            "Synthetic", "Synthetic", cfg);

        BatchSummary s = r.Summary;
        TestContext.WriteLine(
            $"Greedy(P) win rate {s.PlayerWinRate:P1}  CI[{s.PlayerWinRateCiLow:P1}, {s.PlayerWinRateCiHigh:P1}]  " +
            $"P{s.PlayerWins}/E{s.EnemyWins}/D{s.Draws}/ND{s.NonDecisive}  avgTurns {s.AvgTurns:0.0}");
        foreach (var kv in s.EndReasonCounts) TestContext.WriteLine($"  {kv.Key}: {kv.Value}");

        Assert.That(r.Sessions.Count, Is.EqualTo(30));
        Assert.That(s.PlayerWins + s.EnemyWins + s.Draws + s.NonDecisive, Is.EqualTo(30));
        Assert.That(s.EndReasonCounts.Values.Sum(), Is.EqualTo(30));
        Assert.That(s.PlayerWinRateCiLow, Is.LessThanOrEqualTo(s.PlayerWinRate));
        Assert.That(s.PlayerWinRate, Is.LessThanOrEqualTo(s.PlayerWinRateCiHigh));
        foreach (SessionRecord sess in r.Sessions)
        {
            Assert.That(sess.Outcome.Reason, Is.Not.Null.And.Not.Empty);
            Assert.That(sess.PerSideMetrics.ContainsKey("Player"), Is.True);
        }
        Assert.That(s.PlayerWinRate, Is.GreaterThan(0.6)); // loose sanity: Greedy >> Random
    }
}
