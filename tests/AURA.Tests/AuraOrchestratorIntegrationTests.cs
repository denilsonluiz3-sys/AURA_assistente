using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AURA.Agents;
using AURA.AI.UniversalAI;
using AURA.Abstractions.Execution;
using AURA.Core;
using AURA.Core.Abstractions;
using AURA.Core.Launchers;
using AURA.Core.Logging;
using AURA.Core.Runtime;
using AURA.Memory;
using Xunit;

namespace AURA.Tests;

public sealed class AuraOrchestratorIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_EmptyCommand_ReturnsError()
    {
        using var fixture = new OrchestratorFixture();

        string result = await fixture.Orchestrator.ExecuteAsync("   ");

        Assert.Equal("Comando vazio.", result);
    }

    [Fact]
    public async Task ExecuteAsync_MemoryHit_ReturnsStoredResult()
    {
        using var fixture = new OrchestratorFixture();

        fixture.Memory.Record("pesquise e execute a tarefa", "orchestration", "resultado da memória", success: true);

        string result = await fixture.Orchestrator.ExecuteAsync("pesquise e execute a tarefa");

        Assert.StartsWith("💾 Memória:", result);
        Assert.Contains("resultado da memória", result);
    }

    [Fact]
    public async Task ExecuteAsync_NoMemoryHit_DelegatesToAgentSession()
    {
        using var fixture = new OrchestratorFixture(new FakeUniversalAiClient());

        string result = await fixture.Orchestrator.ExecuteAsync("comando totalmente novo que não existe na memória");

        Assert.StartsWith("❌ Erro ao processar:", result);
    }

    private sealed class OrchestratorFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "aura-tests-" + Guid.NewGuid().ToString("N"));
        private readonly SimulationRuntime _runtime;

        public SolutionStore Memory { get; }
        public AuraOrchestrator Orchestrator { get; }

        public OrchestratorFixture(IUniversalAiClient? aiClient = null)
        {
            Directory.CreateDirectory(_root);
            var logger = new ConsoleLogger();
            _runtime = new SimulationRuntime(
                logger,
                Path.Combine(_root, "cells"),
                new DirectoryCellBackend(),
                persist: false);

            Memory = new SolutionStore(logger, Path.Combine(_root, "memory"));
            var runner = new Runner(new ILauncher[] { });
            var shell = new FakeExecutor();
            var webSearch = new FakeWebSearch();

            Orchestrator = new AuraOrchestrator(
                logger,
                Memory,
                runner,
                _runtime,
                shell,
                webSearch,
                aiClient: aiClient,
                httpClient: null,
                events: null);
        }

        public void Dispose()
        {
            _runtime.Dispose();
            try { Directory.Delete(_root, recursive: true); } catch { }
        }
    }

    private sealed class FakeUniversalAiClient : IUniversalAiClient
    {
        public UniversalAiClientOptions Options { get; } = new();

        public Task<string> ChatAsync(string question, HttpClient? httpClient = null, string? systemPrompt = null, CancellationToken ct = default)
            => Task.FromResult("fake");

        public Task<AgentChatResponse> ChatToolsAsync(
            IReadOnlyList<AgentMessage> messages,
            IReadOnlyList<AgentToolDefinition> tools,
            HttpClient? httpClient = null,
            CancellationToken ct = default,
            string? systemPrompt = null)
            => Task.FromResult(new AgentChatResponse { Error = "fake", ErrorKind = AgentErrorKind.ProviderError });
    }

    private sealed class FakeExecutor : IToolExecutor
    {
        public string Name => "fake";
        public int Calls { get; private set; }

        public bool IsAvailable() => true;

        public Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new ExecutionResult { Success = true, StandardOutput = "ok" });
        }
    }

    private sealed class FakeWebSearch : IWebSearch
    {
        public string Response { get; set; } = "Resultado web";
        public int Calls { get; private set; }

        public Task<string> SearchAsync(string query, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(Response);
        }

        public Task<string> SearchWithRefinementAsync(string query, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(Response);
        }
    }
}
