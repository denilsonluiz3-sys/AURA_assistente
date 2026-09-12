using AURA.AI;
using AURA.Core.Logging;
using Xunit;

namespace AURA.Tests;

public sealed class AgentRunStoreCleanupTests
{
    private sealed class FakeLogger : ILogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }

    [Fact]
    public void Cleanup_RemovesOldTerminalAndCorruptFiles_ButKeepsPausedRuns()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-run-cleanup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new AgentRunStore(new FakeLogger(), root, TimeSpan.FromDays(1), maxRetainedTerminalRuns: 20);
            store.Save(new AgentRunState { RunId = "paused", Status = AgentRunStatus.Paused });
            File.SetLastWriteTimeUtc(Path.Combine(root, "paused.json"), DateTime.UtcNow.AddDays(-3));

            string corrupt = Path.Combine(root, "corrupt.json");
            File.WriteAllText(corrupt, "{not-json");
            File.SetLastWriteTimeUtc(corrupt, DateTime.UtcNow.AddDays(-3));

            string temp = Path.Combine(root, "abandoned.tmp");
            File.WriteAllText(temp, "partial");
            File.SetLastWriteTimeUtc(temp, DateTime.UtcNow.AddDays(-3));

            int removed = store.Cleanup();

            Assert.Equal(2, removed);
            Assert.True(File.Exists(Path.Combine(root, "paused.json")));
            Assert.False(File.Exists(corrupt));
            Assert.False(File.Exists(temp));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Save_PrunesOldestTerminalRuns_WithoutRemovingRecentRuns()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-run-limit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new AgentRunStore(new FakeLogger(), root, TimeSpan.FromDays(30), maxRetainedTerminalRuns: 2);
            store.Save(new AgentRunState { RunId = "one", Status = AgentRunStatus.Completed });
            File.SetLastWriteTimeUtc(Path.Combine(root, "one.json"), DateTime.UtcNow.AddDays(-3));
            store.Save(new AgentRunState { RunId = "two", Status = AgentRunStatus.Failed });
            File.SetLastWriteTimeUtc(Path.Combine(root, "two.json"), DateTime.UtcNow.AddDays(-2));
            store.Save(new AgentRunState { RunId = "three", Status = AgentRunStatus.Cancelled });

            Assert.False(File.Exists(Path.Combine(root, "one.json")));
            Assert.True(File.Exists(Path.Combine(root, "two.json")));
            Assert.True(File.Exists(Path.Combine(root, "three.json")));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }
}
