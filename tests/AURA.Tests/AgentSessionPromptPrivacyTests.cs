using AURA.AI;
using AURA.AI.UniversalAI;
using AURA.Core.Logging;
using AURA.Memory;
using Xunit;

namespace AURA.Tests;

public sealed class AgentSessionPromptPrivacyTests
{
    private sealed class CapturingClient : IUniversalAiClient
    {
        public UniversalAiClientOptions Options { get; } = new();
        public string? LastSystemPrompt { get; private set; }

        public Task<string> ChatAsync(string question, HttpClient? httpClient = null, string? systemPrompt = null, CancellationToken ct = default)
            => Task.FromResult("ok");

        public Task<AgentChatResponse> ChatToolsAsync(
            IReadOnlyList<AgentMessage> messages,
            IReadOnlyList<AgentToolDefinition> tools,
            HttpClient? httpClient = null,
            CancellationToken ct = default,
            string? systemPrompt = null)
        {
            LastSystemPrompt = systemPrompt;
            return Task.FromResult(new AgentChatResponse { Content = "ok" });
        }
    }

    private sealed class FakeLogger : ILogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }

    [Fact]
    public async Task SystemPrompt_DoesNotExposeLocalMemoryPath()
    {
        string path = Path.Combine(Path.GetTempPath(), "aura-memory-" + Guid.NewGuid().ToString("N"), "memory.json");
        try
        {
            AgentSession.ClearSharedHistory();
            var memory = new MemoryStore(new FakeLogger(), path);
            var client = new CapturingClient();
            var session = new AgentSession(client, Array.Empty<AgentTool>(), memory: memory, logger: new FakeLogger());

            await session.RunAsync("oi");

            Assert.NotNull(client.LastSystemPrompt);
            Assert.DoesNotContain(path, client.LastSystemPrompt, StringComparison.Ordinal);
            Assert.Contains("Memória persistente disponível localmente.", client.LastSystemPrompt, StringComparison.Ordinal);
        }
        finally
        {
            AgentSession.ClearSharedHistory();
            try { Directory.Delete(Path.GetDirectoryName(path)!, recursive: true); } catch { }
        }
    }
}
