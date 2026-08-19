using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions.Orchestration;
using AURA.Abstractions.Process;
using AURA.Agents;
using AURA.Core.Abstractions;
using AURA.Core.Events;
using AURA.Core.Knowledge;
using Xunit;

namespace AURA.Tests
{
    public class LegalProcessEngineTests
    {
        private sealed class FakeLogger : AURA.Core.Logging.ILogger
        {
            public void Info(string msg) { }
            public void Warning(string msg) { }
            public void Error(string msg) { }
        }

        private sealed class FakeOrchestrator : IOrchestrator
        {
            public string Result = "Resultado da instrução pesquisada.";
            public int Calls;

            public Task<string> ExecuteAsync(string userCommand, CancellationToken ct = default)
            {
                Calls++;
                return Task.FromResult(Result);
            }
        }

        private sealed class FakeAgent : IAgent
        {
            private readonly string _name;
            private readonly string _answer;

            public FakeAgent(string name, string answer)
            {
                _name = name;
                _answer = answer;
            }

            public string Name => _name;
            public string Description => "fake " + _name;
            public void Start() { }
            public void Stop() { }
            public Task<string> AskAsync(string question, CancellationToken ct = default)
                => Task.FromResult(_answer);
        }

        private static LegalProcessEngine Create(
            FakeOrchestrator orchestrator,
            List<IAgent>? agents = null,
            EventBus? events = null)
        {
            return new LegalProcessEngine(new FakeLogger(), agents ?? new List<IAgent>(),
                orchestrator, events);
        }

        [Fact]
        public void EmptyCommand_ReturnsError()
        {
            var engine = Create(new FakeOrchestrator());
            Assert.Equal("Comando vazio.", engine.RunAsync("   ").Result);
        }

        [Fact]
        public async Task RunsPhases_ReturnsComposedVerdict()
        {
            var orch = new FakeOrchestrator();
            var events = new EventBus();
            var engine = Create(orch, events: events);

            string result = await engine.RunAsync("pesquise e execute a tarefa");

            Assert.Contains("Processo", result);
            Assert.Contains("Instrução suficiente", result);
            Assert.True(orch.Calls >= 1);
        }

        [Fact]
        public void MemoryAgreement_SkipsOrchestration()
        {
            var orch = new FakeOrchestrator();
            var agents = new List<IAgent> { new FakeAgent("memory", "resposta da memória") };
            var engine = Create(orch, agents);

            string result = engine.RunAsync("o que eu fiz ontem?").Result;

            Assert.Contains("Acordo alcançado", result);
            Assert.Equal(0, orch.Calls);
        }

        [Fact]
        public async Task LlmVerdict_UsedWhenProvided()
        {
            var orch = new FakeOrchestrator();
            var engine = Create(orch);

            string result = await engine.RunAsync("analise o caso",
                llm: (prompt, ct) => Task.FromResult("SENTENÇA: procedente em parte."));

            Assert.Contains("SENTENÇA: procedente", result);
        }

        [Fact]
        public async Task Failure_RetriesIsolatedOnce()
        {
            var orch = new FakeOrchestrator { Result = "Falha na busca: timeout" };
            var engine = Create(orch);

            string result = await engine.RunAsync("pesquise algo");

            Assert.Equal(2, orch.Calls);
            Assert.Contains("Processo", result);
        }

        [Fact]
        public void GetCurrentState_ReturnsLastProcess()
        {
            var orch = new FakeOrchestrator();
            var engine = Create(orch);
            engine.RunAsync("execute uma tarefa");

            var state = engine.GetCurrentState();

            Assert.Equal(LegalPhase.Archived, state.Phase);
            Assert.True(state.IsTerminated);
        }

        [Fact]
        public void PublishesProcessEvents()
        {
            var orch = new FakeOrchestrator();
            var events = new EventBus();
            int count = 0;
            events.Subscribe<OrchestrationStepEvent>(_ => count++);
            var engine = Create(orch, events: events);

            engine.RunAsync("execute uma tarefa");

            Assert.True(count >= 4);
        }

        // ──── KnowledgeManager ────────────────────────────────────────────────

        [Fact]
        public void KnowledgeManager_SeededDefaults()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "aura-knowledge-" + Guid.NewGuid().ToString("N"));
            var km = new KnowledgeManager(dir, new FakeLogger());
            Assert.Equal("knowledge", km.Name);
            Assert.False(string.IsNullOrWhiteSpace(km.Description));
        }

        [Fact]
        public async Task KnowledgeManager_ReturnsSeededAnswer()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "aura-knowledge-" + Guid.NewGuid().ToString("N"));
            var km = new KnowledgeManager(dir, new FakeLogger());

            string answer = await km.GetKnowledgeAsync("cobrança");

            Assert.Contains("Notificação", answer);
        }

        [Fact]
        public async Task KnowledgeManager_Unknown_ReturnsEmpty()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "aura-knowledge-" + Guid.NewGuid().ToString("N"));
            var km = new KnowledgeManager(dir, new FakeLogger());

            string answer = await km.GetKnowledgeAsync("zzz-termo-inexistente-999");

            Assert.Equal(string.Empty, answer);
        }

        [Fact]
        public async Task Engine_UsesKnowledgeAgent_BeforeOrchestrator()
        {
            var orch = new FakeOrchestrator();
            var agents = new List<IAgent> { new FakeAgent("knowledge", "prazo de 15 dias úteis") };
            var engine = Create(orch, agents);

            string result = await engine.RunAsync("qual o prazo de contestação?");

            Assert.Contains("prazo de 15 dias", result);
            Assert.Equal(0, orch.Calls);
        }
    }
}