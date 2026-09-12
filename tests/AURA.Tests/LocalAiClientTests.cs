using AURA.AI;
using AURA.AI.UniversalAI;
using Xunit;

namespace AURA.Tests;

public sealed class LocalAiClientTests
{
    private sealed class FakeRuntime : ILocalAiRuntime
    {
        private readonly string _response;
        public IReadOnlyList<AgentMessage>? Messages { get; private set; }
        public IReadOnlyList<AgentToolDefinition>? Tools { get; private set; }

        public FakeRuntime(string response) => _response = response;

        public Task<string> CompleteAsync(
            IReadOnlyList<AgentMessage> messages,
            IReadOnlyList<AgentToolDefinition> tools,
            CancellationToken ct = default)
        {
            Messages = messages;
            Tools = tools;
            return Task.FromResult(_response);
        }
    }

    [Fact]
    public async Task ConvertsLocalToolCallToUniversalResponse()
    {
        var runtime = new FakeRuntime(
            "{\"type\":\"tool_call\",\"name\":\"create_task\",\"arguments\":{\"title\":\"Teste\"}}");
        var client = new LocalAiClient(runtime);

        var response = await client.ChatToolsAsync(
            new[] { new AgentMessage { Role = "user", Content = "crie uma tarefa" } },
            new[] { new AgentToolDefinition { Name = "create_task" } });

        Assert.Null(response.Error);
        Assert.NotNull(response.ToolCalls);
        Assert.Single(response.ToolCalls!);
        Assert.Equal("create_task", response.ToolCalls[0].Name);
        Assert.Equal("local", client.Options.Provider);
        Assert.Single(runtime.Tools!);
    }

    [Fact]
    public async Task PreservesLocalFinalText()
    {
        var runtime = new FakeRuntime("resposta offline");
        var client = new LocalAiClient(runtime);

        var response = await client.ChatToolsAsync(
            new[] { new AgentMessage { Role = "user", Content = "olá" } },
            Array.Empty<AgentToolDefinition>());

        Assert.Equal("resposta offline", response.Content);
        Assert.Null(response.ToolCalls);
    }

    [Fact]
    public async Task AddsSystemPromptOnlyWhenMissing()
    {
        var runtime = new FakeRuntime("ok");
        var client = new LocalAiClient(runtime);

        await client.ChatToolsAsync(
            new[] { new AgentMessage { Role = "user", Content = "olá" } },
            Array.Empty<AgentToolDefinition>(),
            systemPrompt: "responda em português");

        Assert.Equal("system", runtime.Messages![0].Role);
        Assert.Equal("responda em português", runtime.Messages[0].Content);
    }
}
