using System;
using System.IO;
using System.Threading.Tasks;
using AURA.Abstractions.Execution;
using AURA.Agents;
using AURA.Memory;
using Xunit;

namespace AURA.Tests
{
    public class ConcreteAgentsTests
    {
        private sealed class FakeLogger : AURA.Core.Logging.ILogger
        {
            public void Info(string msg) { }
            public void Warning(string msg) { }
            public void Error(string msg) { }
        }

        // ──── MemoryAgent ────────────────────────────────────────────────────

        [Fact]
        public async Task MemoryAgent_EmptyStore_ReturnsNoEntries()
        {
            string path = Path.Combine(Path.GetTempPath(), "aura-mem-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new MemoryStore(new FakeLogger(), path);
            var agent = new MemoryAgent(store, new FakeLogger());

            string answer = await agent.AskAsync("o que eu disse antes?");

            Assert.Contains("Nenhuma entrada", answer);
        }

        [Fact]
        public async Task MemoryAgent_WithEntries_ReturnsSummary()
        {
            string path = Path.Combine(Path.GetTempPath(), "aura-mem-" + Guid.NewGuid().ToString("N") + ".json");
            var store = new MemoryStore(new FakeLogger(), path);
            store.Append(MemoryEntry.Question("Olá AURA"));
            store.Append(MemoryEntry.Answer("Olá! Como posso ajudar?"));

            var agent = new MemoryAgent(store, new FakeLogger());
            string answer = await agent.AskAsync("histórico");

            Assert.Contains("user", answer);
            Assert.Contains("Olá AURA", answer);
        }

        [Fact]
        public void MemoryAgent_Metadata()
        {
            var store = new MemoryStore(new FakeLogger(),
                Path.Combine(Path.GetTempPath(), "mem-noop.json"));
            var agent = new MemoryAgent(store);

            Assert.Equal("memory", agent.Name);
            Assert.False(string.IsNullOrWhiteSpace(agent.Description));
        }

        // ──── AutomationAgent ────────────────────────────────────────────────

        private sealed class FakeShellExecutor : IToolExecutor
        {
            public string Name => "fake-shell";
            public bool IsAvailable() => true;

            public Task<ExecutionResult> ExecuteAsync(ExecutionRequest request,
                System.Threading.CancellationToken ct = default)
            {
                return Task.FromResult(new ExecutionResult
                {
                    ExitCode = 0,
                    StandardOutput = "output-de-" + request.Command,
                    StandardError = string.Empty,
                    Duration = TimeSpan.Zero
                });
            }
        }

        private sealed class UnavailableExecutor : IToolExecutor
        {
            public string Name => "unavailable";
            public bool IsAvailable() => false;
            public Task<ExecutionResult> ExecuteAsync(ExecutionRequest r,
                System.Threading.CancellationToken ct = default)
                => Task.FromResult(new ExecutionResult { ExitCode = 1 });
        }

        [Fact]
        public async Task AutomationAgent_RunsCommand_ReturnsOutput()
        {
            var agent = new AutomationAgent(new FakeShellExecutor(), new FakeLogger());
            string result = await agent.AskAsync("echo hello");

            Assert.Contains("output-de-echo hello", result);
        }

        [Fact]
        public async Task AutomationAgent_UnavailableShell_ReturnsError()
        {
            var agent = new AutomationAgent(new UnavailableExecutor(), new FakeLogger());
            string result = await agent.AskAsync("ls");

            Assert.Contains("não disponível", result);
        }

        [Fact]
        public void AutomationAgent_Metadata()
        {
            var agent = new AutomationAgent(new FakeShellExecutor());
            Assert.Equal("automation", agent.Name);
            Assert.False(string.IsNullOrWhiteSpace(agent.Description));
        }

        // ──── AIAgent ────────────────────────────────────────────────────────

        [Fact]
        public void AIAgent_Metadata()
        {
            var manager = new AgentManager(new FakeLogger(), Array.Empty<AgentInfo>());
            // SimulationRuntime requires a logger; use a temp dir to avoid side effects
            var logger = new FakeLogger();
            var runtime = new AURA.Core.Runtime.SimulationRuntime(logger);
            var agent = new AIAgent(manager, runtime, "aichat", logger);

            Assert.Equal("ai:aichat", agent.Name);
            Assert.False(string.IsNullOrWhiteSpace(agent.Description));

            runtime.Dispose();
        }

        [Fact]
        public async Task AIAgent_UnknownAssistant_ReturnsErrorMessage()
        {
            var manager = new AgentManager(new FakeLogger(), Array.Empty<AgentInfo>());
            var logger = new FakeLogger();
            var runtime = new AURA.Core.Runtime.SimulationRuntime(logger);
            var agent = new AIAgent(manager, runtime, "nao-existe", logger);

            string result = await agent.AskAsync("hello");

            Assert.Contains("Erro ao consultar", result);
            runtime.Dispose();
        }
    }
}
