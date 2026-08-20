using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AURA.Agents;
using AURA.Core.Logging;
using AURA.Core.Runtime;
using AURA.Memory;
using Xunit;

namespace AURA.Tests;

public sealed class AuraOrchestratorIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldRequireConfirmation_ForExecuteIntent()
    {
        using var fixture = new OrchestratorFixture();

        string result = await fixture.Orchestrator.ExecuteAsync("execute teste.sh");

        Assert.StartsWith("⚠️", result);
        Assert.Contains("confirm", result, StringComparison.OrdinalIgnoreCase);
        Assert.False(fixture.Tool.Executed);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteInjectedTool_WhenConfirmed()
    {
        using var fixture = new OrchestratorFixture();

        string result = await fixture.Orchestrator.ExecuteAsync("execute teste.sh", confirmed: true);

        Assert.Equal("executed", result);
        Assert.True(fixture.Tool.Executed);
    }

    private sealed class OrchestratorFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "aura-tests-" + Guid.NewGuid().ToString("N"));
        private readonly SimulationRuntime _runtime;

        public FakeTool Tool { get; } = new();
        public AuraOrchestrator Orchestrator { get; }

        public OrchestratorFixture()
        {
            Directory.CreateDirectory(_root);
            var logger = new ConsoleLogger();
            _runtime = new SimulationRuntime(
                logger,
                Path.Combine(_root, "cells"),
                new DirectoryCellBackend(),
                persist: false);

            var memory = new SolutionStore(logger, Path.Combine(_root, "memory"));
            var tools = new ToolResolver(new ITool[] { Tool });

            Orchestrator = new AuraOrchestrator(
                logger,
                memory,
                new AURA.Core.Launchers.Runner(),
                _runtime,
                toolResolver: tools);
        }

        public void Dispose()
        {
            _runtime.Dispose();
            try { Directory.Delete(_root, recursive: true); } catch { }
        }
    }

    private sealed class FakeTool : ITool
    {
        public bool Executed { get; private set; }
        public string Intent => "execute";

        public Task<ToolResult> ExecuteAsync(
            string command,
            Dictionary<string, string> parameters,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            Executed = true;
            return Task.FromResult(new ToolResult(true, "executed"));
        }
    }
}
