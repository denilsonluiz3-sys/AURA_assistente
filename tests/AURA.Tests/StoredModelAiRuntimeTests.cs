using AURA.AI;
using AURA.AI.UniversalAI;
using Xunit;

namespace AURA.Tests;

public sealed class StoredModelAiRuntimeTests
{
    private sealed class FakeEngine : ILocalModelEngine
    {
        public string? ModelPath { get; private set; }
        public IReadOnlyList<AgentToolDefinition>? Tools { get; private set; }

        public Task<string> GenerateAsync(
            string modelPath,
            IReadOnlyList<AgentMessage> messages,
            IReadOnlyList<AgentToolDefinition> tools,
            CancellationToken ct = default)
        {
            ModelPath = modelPath;
            Tools = tools;
            return Task.FromResult("resposta do motor local");
        }
    }

    [Fact]
    public async Task UsesOnlyTheRegisteredModelPath()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-runtime-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalModelStore(root);
            await store.ImportAsync(
                new MemoryStream(new byte[] { 1, 2, 3 }),
                new LocalModelDescriptor { Id = "model-a", FileName = "model-a.gguf" });
            var engine = new FakeEngine();
            var runtime = new StoredModelAiRuntime(store, engine, "model-a");

            string answer = await runtime.CompleteAsync(
                new[] { new AgentMessage { Role = "user", Content = "olá" } },
                new[] { new AgentToolDefinition { Name = "read_file" } });

            Assert.Equal("resposta do motor local", answer);
            Assert.Equal(store.GetModelPath("model-a"), engine.ModelPath);
            Assert.Single(engine.Tools!);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task DoesNotInvokeEngineForUnknownModel()
    {
        string root = Path.Combine(Path.GetTempPath(), "aura-runtime-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalModelStore(root);
            var engine = new FakeEngine();
            var runtime = new StoredModelAiRuntime(store, engine, "missing");

            await Assert.ThrowsAsync<InvalidDataException>(() => runtime.CompleteAsync(
                Array.Empty<AgentMessage>(), Array.Empty<AgentToolDefinition>()));
            Assert.Null(engine.ModelPath);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
