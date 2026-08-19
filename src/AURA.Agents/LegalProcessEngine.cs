using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions.Orchestration;
using AURA.Abstractions.Process;
using AURA.Core.Abstractions;
using AURA.Core.Events;
using AURA.Core.Logging;

namespace AURA.Agents
{
    /// <summary>
    /// Núcleo AURA como processo jurídico: percorre as fases pré-processual →
    /// conhecimento → decisão → recursal → execução → arquivamento. Em cada
    /// fase toma decisões if/else reutilizando a infraestrutura existente:
    /// MemoryAgent (conciliação/memória), orquestrador (pesquisa+execução),
    /// AutomationAgent (execução shell) e LLM opcional (sentença). Publica o
    /// estado via OrchestrationStepEvent para a interface acompanhar em tempo real.
    /// </summary>
    public sealed class LegalProcessEngine : IProcessOrchestrator
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyList<IAgent> _agents;
        private readonly IOrchestrator _orchestrator;
        private readonly EventBus? _events;
        private readonly Dictionary<string, ProcessState> _processes = new(StringComparer.OrdinalIgnoreCase);

        public LegalProcessEngine(ILogger logger, IEnumerable<IAgent> agents,
            IOrchestrator orchestrator, EventBus? events = null)
        {
            _logger = logger ?? new ConsoleLogger();
            _agents = (agents ?? Array.Empty<IAgent>()).ToList();
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _events = events;
        }

        public ProcessState GetCurrentState(string? processId = null)
        {
            if (processId != null && _processes.TryGetValue(processId, out ProcessState? state))
                return state;

            return _processes.Values.FirstOrDefault() ?? new ProcessState();
        }

        public async Task<string> HandleUserInputAsync(string userCommand, LlmHandler? llm = null,
            CancellationToken cancellationToken = default)
            => await RunAsync(userCommand, llm, cancellationToken).ConfigureAwait(false);

        public async Task<string> RunAsync(string userCommand, LlmHandler? llm = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userCommand))
                return "Comando vazio.";

            userCommand = userCommand.Trim();
            var state = new ProcessState { Command = userCommand };
            _processes[state.Id] = state;

            _logger.Info("[PROCESSO] " + state.Id + " · " + userCommand);

            Publish(state.Id, "Processo " + state.Id[..8], "Pré-processual",
                "Executando", "Classificando pedido e tentando conciliação", 0.05);

            // ── Fase pré-processual: conciliação/liminar via memória ──────────
            await TryConciliationAsync(state, userCommand, cancellationToken).ConfigureAwait(false);
            if (state.Agreement)
                return Finalize(state, "Acordo alcançado na fase pré-processual.");

            // ── Fase de conhecimento: instrução (conhecimento local + pesquisa) ─
            Publish(state.Id, "Processo " + state.Id[..8], "Conhecimento",
                "Executando", "Instruindo o processo (conhecimento e pesquisa)", 0.3);
            string? knowledge = await TryKnowledgeAsync(state, userCommand, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(knowledge))
            {
                knowledge = await _orchestrator.ExecuteAsync(userCommand, cancellationToken).ConfigureAwait(false);
            }
            state.AddDecision("instrução: " + knowledge.Length + " caracteres");

            // ── Fase decisória: sentença ──────────────────────────────────────
            Publish(state.Id, "Processo " + state.Id[..8], "Decisão",
                "Executando", "Proferindo sentença", 0.6);
            string verdictText = await RenderVerdictAsync(state, knowledge, llm, cancellationToken)
                .ConfigureAwait(false);

            // ── Fase recursal: retry isolado em falha ─────────────────────────
            if (LooksLikeFailure(verdictText))
            {
                Publish(state.Id, "Processo " + state.Id[..8], "Recursal",
                    "Executando", "Resultado insatisfatório — nova tentativa isolada", 0.75);
                _logger.Warning("[PROCESSO] recurso: " + verdictText);
                verdictText = await _orchestrator.ExecuteAsync(userCommand, cancellationToken).ConfigureAwait(false);
                state.IsFinal = !LooksLikeFailure(verdictText);
            }
            else
            {
                state.IsFinal = true;
            }

            // ── Fase executiva: execução prática da tarefa ────────────────────
            string execution = await TryExecutionAsync(state, userCommand, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(execution))
                state.AddDecision("execução: " + execution.Length + " caracteres");

            // ── Arquivamento: composição final ────────────────────────────────
            return Finalize(state, ComposeFinal(state, verdictText, execution));
        }

        private async Task TryConciliationAsync(ProcessState state, string userCommand,
            CancellationToken ct)
        {
            try
            {
                IAgent? memory = _agents.FirstOrDefault(a =>
                    a.Name.StartsWith("memory", StringComparison.OrdinalIgnoreCase));
                if (memory == null)
                    return;

                string recall = await memory.AskAsync(userCommand, ct).ConfigureAwait(false);
                if (LooksLikeFailure(recall))
                    return;

                state.Agreement = true;
                state.AddDecision("conciliação: resposta da memória aproveitada");
            }
            catch (Exception ex)
            {
                _logger.Warning("[PROCESSO] conciliação: " + ex.Message);
            }
        }

        private async Task<string?> TryKnowledgeAsync(ProcessState state, string userCommand,
            CancellationToken ct)
        {
            try
            {
                IAgent? knowledge = _agents.FirstOrDefault(a =>
                    a.Name.StartsWith("knowledge", StringComparison.OrdinalIgnoreCase));
                if (knowledge == null)
                    return null;

                string answer = await knowledge.AskAsync(userCommand, ct).ConfigureAwait(false);
                if (LooksLikeFailure(answer))
                    return null;

                state.AddDecision("conhecimento: base local consultada");
                _logger.Info("[PROCESSO] conhecimento local: " + answer.Length + " caracteres");
                return answer;
            }
            catch (Exception ex)
            {
                _logger.Warning("[PROCESSO] conhecimento: " + ex.Message);
                return null;
            }
        }

        private async Task<string> RenderVerdictAsync(ProcessState state, string knowledge,
            LlmHandler? llm, CancellationToken ct)
        {
            if (llm != null)
            {
                string prompt = "Sentencie o pedido abaixo como um processo jurídico "
                    + "(fato, direito, dispositivo).\nPedido: " + state.Command
                    + "\n\nInstrução coletada:\n" + knowledge;
                try
                {
                    string verdict = await llm(prompt, ct).ConfigureAwait(false);
                    if (!LooksLikeFailure(verdict))
                        return verdict;
                }
                catch (Exception ex)
                {
                    _logger.Warning("[PROCESSO] llm: " + ex.Message);
                }
            }

            state.Verdict = LooksLikeFailure(knowledge)
                ? new Verdict { Kind = VerdictKind.Improcedente, Reason = knowledge }
                : new Verdict { Kind = VerdictKind.Procedente, Reason = "Instrução suficiente coletada" };
            state.AddDecision("sentença: " + state.Verdict.Kind);

            return "Sentença: " + state.Verdict.Kind + "\n\n" + knowledge;
        }

        private async Task<string> TryExecutionAsync(ProcessState state, string userCommand,
            CancellationToken ct)
        {
            try
            {
                if (!LooksLikeExecution(userCommand))
                    return string.Empty;

                IAgent? automation = _agents.FirstOrDefault(a =>
                    a.Name.StartsWith("automation", StringComparison.OrdinalIgnoreCase));
                if (automation == null)
                    return string.Empty;

                string result = await automation.AskAsync(userCommand, ct).ConfigureAwait(false);
                if (LooksLikeFailure(result) && result.Contains("não disponível", StringComparison.OrdinalIgnoreCase))
                    return string.Empty;

                state.Seized = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.Warning("[PROCESSO] execução: " + ex.Message);
                return string.Empty;
            }
        }

        private string Finalize(ProcessState state, string result)
        {
            state.Phase = LegalPhase.Archived;
            state.IsTerminated = true;
            Publish(state.Id, "Processo " + state.Id[..8], "Arquivamento",
                "Concluído", "Processo finalizado e arquivado", 1);
            return result;
        }

        private static string ComposeFinal(ProcessState state, string verdict, string execution)
        {
            var sb = new StringBuilder();
            sb.AppendLine("⚖️ Processo " + state.Id[..8]);
            sb.AppendLine("Fases percorridas: " + string.Join(" → ", state.History.Select(h => h.Split(':')[0])));
            sb.AppendLine();
            if (!string.IsNullOrWhiteSpace(verdict))
                sb.AppendLine(verdict);
            if (!string.IsNullOrWhiteSpace(execution))
            {
                sb.AppendLine();
                sb.AppendLine("Execução:");
                sb.AppendLine(execution);
            }
            return sb.ToString().TrimEnd();
        }

        private static bool LooksLikeExecution(string text)
        {
            string t = text.ToLowerInvariant();
            return t.Contains("execute") || t.Contains("rode") || t.Contains("rodar")
                || t.Contains("crie") || t.Contains("run ") || t.Contains("crie uma célula")
                || t.EndsWith(".py", StringComparison.OrdinalIgnoreCase)
                || t.EndsWith(".sh", StringComparison.OrdinalIgnoreCase)
                || t.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)
                || t.EndsWith(".js", StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeFailure(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return true;
            string t = text.ToLowerInvariant();
            return t.Contains("falha") || t.Contains("erro") || t.Contains("não disponível")
                || t.Contains("nenhum resultado") || t.Contains("limite de passos");
        }

        private void Publish(string id, string title, string target, string status,
            string message, double progress)
        {
            _events?.Publish(new OrchestrationStepEvent
            {
                Id = id,
                Title = title,
                Target = target,
                Status = status,
                Message = message,
                Progress = Math.Clamp(progress, 0, 1),
                OccurredAt = DateTime.UtcNow
            });
        }
    }
}