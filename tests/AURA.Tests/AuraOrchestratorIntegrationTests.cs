using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AURA.Agents;
using AURA.AI;
using AURA.Abstractions;
using AURA.Abstractions.Execution;
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

        Assert.Equal("resultado da memória", result);
    }

    [Fact]
    public async Task ExecuteAsync_SearchIntent_UsesLocalToolWithoutAi()
    {
        using var fixture = new OrchestratorFixture(new OpenRouterClient(new OpenRouterOptions { ApiKey = "" }));
        string result = await fixture.Orchestrator.ExecuteAsync("pesquise AURA kernel");

        Assert.Equal("Resultado web", result);
        Assert.Equal(1, fixture.WebSearch.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_AndroidIntent_UsesNativeCapabilityWithoutAi()
    {
        using var fixture = new OrchestratorFixture(new OpenRouterClient(new OpenRouterOptions { ApiKey = "" }), new FakeAndroid());
        string result = await fixture.Orchestrator.ExecuteAsync("quanto está a bateria?");

        Assert.Equal("battery-ok", result);
    }

    [Fact]
    public async Task ExecuteAsync_ExecuteIntent_RequiresConfirmation()
    {
        using var fixture = new OrchestratorFixture();
        string result = await fixture.Orchestrator.ExecuteAsync("execute teste");

        Assert.StartsWith("CONFIRMAÇÃO NECESSÁRIA:", result);
        Assert.Equal(0, fixture.Executor.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_ConfirmedExecute_UsesShellTool()
    {
        using var fixture = new OrchestratorFixture();
        string result = await fixture.Orchestrator.ExecuteAsync("execute teste", confirmed: true);

        Assert.Equal("ok", result);
        Assert.Equal(1, fixture.Executor.Calls);
    }

    [Fact]
    public async Task ExecuteAsync_NoLocalIntent_UsesAiFallback()
    {
        using var fixture = new OrchestratorFixture(new OpenRouterClient(new OpenRouterOptions { ApiKey = "" }));
        string result = await fixture.Orchestrator.ExecuteAsync("comando totalmente novo que não existe na memória");

        Assert.StartsWith("❌ Erro ao processar:", result);
    }

    private sealed class OrchestratorFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "aura-tests-" + Guid.NewGuid().ToString("N"));
        private readonly SimulationRuntime _runtime;

        public SolutionStore Memory { get; }
        public AuraOrchestrator Orchestrator { get; }
        public FakeExecutor Executor { get; }
        public FakeWebSearch WebSearch { get; }

        public OrchestratorFixture(OpenRouterClient? aiClient = null, IAndroidCapabilityService? android = null)
        {
            Directory.CreateDirectory(_root);
            var logger = new ConsoleLogger();
            _runtime = new SimulationRuntime(logger, Path.Combine(_root, "cells"), new DirectoryCellBackend(), persist: false);
            Memory = new SolutionStore(logger, Path.Combine(_root, "memory"));
            var runner = new Runner(new ILauncher[] { });
            Executor = new FakeExecutor();
            WebSearch = new FakeWebSearch();

            Orchestrator = new AuraOrchestrator(
                logger, Memory, runner, _runtime, Executor, WebSearch,
                aiClient: aiClient, httpClient: null, events: null, android: android);
        }

        public void Dispose()
        {
            _runtime.Dispose();
            try { Directory.Delete(_root, recursive: true); } catch { }
        }
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

    private sealed class FakeAndroid : IAndroidCapabilityService
    {
        public string GetBattery() => "battery-ok";
        public string GetLight() => "light";
        public string GetAccelerometer() => "accelerometer";
        public string GetGyroscope() => "gyroscope";
        public string GetMagnetometer() => "magnetometer";
        public string GetLocation() => "location";
        public string GetCameras() => "camera";
        public string GetAudio() => "audio";
        public string GetBluetooth() => "bluetooth";
        public string GetClipboard() => "clipboard";
        public string SetClipboard(string text) => "clipboard-set";
        public string Notify(string title, string body) => "notification";
        public string Vibrate(int ms) => "vibrate";
        public string GetNetwork() => "network";
        public string GetDevice() => "device";
        public string GetApps() => "apps";
        public string GetProperties() => "properties";
        public string GetMemory() => "memory";
        public string GetStorage() => "storage";
        public string GetAll() => "all";
    }
}
