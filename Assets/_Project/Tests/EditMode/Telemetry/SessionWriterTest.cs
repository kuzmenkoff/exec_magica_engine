using System.IO;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class SessionWriterTests
{
    [Test]
    public void WriteRun_CreatesSummarySessionsAndIndex()
    {
        AllCards db = SyntheticDecks.Database();
        BatchConfig cfg = new BatchConfig { Games = 6, BaseSeed = 1, MaxActions = 400, AlternateStart = true, LogEvents = false };

        BatchResult r = BatchRunner.Run(
            _ => SyntheticDecks.Deck(), _ => SyntheticDecks.Deck(), db,
            seed => new GreedyActionPolicy(seed),
            seed => new RandomActionPolicy(seed + 100000),
            new ModelInfo { ModelId = "Greedy" }, new ModelInfo { ModelId = "Random" },
            "Synthetic", "Synthetic", cfg);

        string root = Path.Combine(Path.GetTempPath(), "execmagica_test_" + System.Guid.NewGuid().ToString("N"));
        try
        {
            string folder = SessionWriter.WriteRun(root, r);

            Assert.That(File.Exists(Path.Combine(folder, "summary.json")), Is.True);
            string[] sessionLines = File.ReadAllLines(Path.Combine(folder, "sessions.jsonl"));
            Assert.That(sessionLines.Length, Is.EqualTo(6));

            string[] indexLines = File.ReadAllLines(Path.Combine(root, "index.jsonl"));
            Assert.That(indexLines.Length, Is.EqualTo(1));

            Assert.That(indexLines[0], Does.Contain("\"PlayerModel\":\"Greedy\""));
            Assert.That(indexLines[0], Does.Contain("\"EnemyModel\":\"Random\""));
            Assert.That(indexLines[0], Does.Contain("\"Games\":6"));
            TestContext.WriteLine($"Run folder: {folder}");
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
