using AURA.AI;
using AURA.AI.UniversalAI;
using AURA.Core.Logging;
using Xunit;

namespace AURA.Tests;

public sealed class AgentSessionResumeTests
{
    [Fact]
    public async Task RoundLimit_PausesAndPersistsState_ThenResumeCompletes()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-run-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            AgentSession.ClearSharedHistory();
            var store = new AgentRunStore(new FakeLogger(), root);
            var first = new AgentSession(new ToolLoopClient(), Array.Empty<AgentTool>(), logger: new FakeLogger(), runStore: store, maxRounds: 2);

            string paused = await first.RunAsync("execute uma tarefa longa");

            Assert.Contains("pausada", paused, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(first.RunId);
            AgentRunState? saved = store.Load(first.RunId!);
            Assert.NotNull(saved);
            Assert.Equal(AgentRunStatus.Paused, saved!.Status);
            Assert.NotEmpty(saved.Messages);

            var resumed = new AgentSession(new FinalAnswerClient(), Array.Empty<AgentTool>(), logger: new FakeLogger(), runStore: store, maxRounds: 2);
            string answer = await resumed.ResumeLastAsync();

            Assert.Equal("concluído após retomada", answer);
            AgentRunState? completed = store.Load(first.RunId!);
            Assert.NotNull(completed);
            Assert.Equal(AgentRunStatus.Completed, completed!.Status);
        }
        finally
        {
            AgentSession.ClearSharedHistory();
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private sealed class ToolLoopClient : IUniversalAiClient
    {
        public Task<string> ChatAsync(string question, HttpClient? httpClient = null, string? systemPrompt = null, CancellationToken ct = default)
            => Task.FromResult("unused");

        public Task<AgentChatResponse> ChatToolsAsync(IReadOnlyList<AgentMessage> messages, IReadOnlyList<AgentToolDefinition> tools, HttpClient? httpClient = null, CancellationToken ct = default, string? systemPrompt = null)
            => Task.FromResult(new AgentChatResponse
            {
                ToolCalls = new List<AgentToolCall>
                {
                    new() { Id = Guid.NewGuid().ToString("N"), Name = "missing_tool", ArgumentsJson = "{}" }
                }
            });
    }

    private sealed class FinalAnswerClient : IUniversalAiClient
    {
        public Task<string> ChatAsync(string question, HttpClient? httpClient = null, string? systemPrompt = null, CancellationToken ct = default)
            => Task.FromResult("concluído após retomada");

        public Task<AgentChatResponse> ChatToolsAsync(IReadOnlyList<AgentMessage> messages, IReadOnlyList<AgentToolDefinition> tools, HttpClient? httpClient = null, CancellationToken ct = default, string? systemPrompt = null)
            => Task.FromResult(new AgentChatResponse { Content = "concluído após retomada" });
    }

    private sealed class FakeLogger : ILogger
    {
        public void Info(string message) { }
        public void Warning(string message) { }
        public void Error(string message) { }
    }
}
