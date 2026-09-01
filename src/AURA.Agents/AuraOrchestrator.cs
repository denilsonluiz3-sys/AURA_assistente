using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions.Execution;
using AURA.Abstractions.Orchestration;
using AURA.AI.UniversalAI;
using AURA.Core.Abstractions;
using AURA.Core.Events;
using AURA.Core.Launchers;
using AURA.Core.Logging;
using AURA.Core.Runtime;
using AURA.Memory;

namespace AURA.Agents
{
    /// <summary>
    /// Orquestrador local determinístico com memória, política, busca e execução.
    /// A IA externa é opcional e usada somente como fallback quando habilitada.
    /// </summary>
    public sealed class AuraOrchestrator : IOrchestrator
    {
        private const int MaxSteps = 5;

        private readonly ILogger _logger;
        private readonly SolutionStore _memory;
        private readonly Runner _runner;
        private readonly SimulationRuntime _runtime;
        private readonly IToolExecutor _shell;
        private readonly IWebSearch _webSearch;
        private readonly IUniversalAiClient? _aiClient;
        private readonly EventBus? _events;
        private readonly IIntentResolver _intentResolver;
        private readonly PolicyGuard _policyGuard;
        private readonly ToolResolver _toolResolver;

        public AuraOrchestrator(
            ILogger logger,
            SolutionStore memory,
            Runner runner,
            SimulationRuntime runtime,
            IToolExecutor shell,
            IWebSearch webSearch,
            IUniversalAiClient? aiClient = null,
            HttpClient? httpClient = null,
            EventBus? events = null,
            IIntentResolver? intentResolver = null,
            PolicyGuard? policyGuard = null,
            ToolResolver? toolResolver = null,
            bool enableFallback = false)
        {
            _logger = logger ?? new ConsoleLogger();
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _webSearch = webSearch ?? throw new ArgumentNullException(nameof(webSearch));
            _aiClient = aiClient;
            _events = events;
            _intentResolver = intentResolver ?? new HeuristicIntentResolver();
            _policyGuard = policyGuard ?? new PolicyGuard();
            _toolResolver = toolResolver ?? CreateToolResolver();
            EnableFallback = enableFallback;
            HttpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        private HttpClient HttpClient { get; }
        private bool EnableFallback { get; }

        public async Task<string> ExecuteAsync(
            string userCommand,
            CancellationToken ct = default,
            bool confirmed = false)
        {
            if (string.IsNullOrWhiteSpace(userCommand))
                return "Comando vazio.";

            userCommand = userCommand.Trim();
            string normalized = userCommand.ToLowerInvariant();
            string processId = "orchestration:" + Guid.NewGuid().ToString("N");

            Publish(processId, "Orquestração", "Assistente", "Executando", "Entendendo solicitação", 0.05);
            _logger.Info("[ORQUESTRA] " + normalized);

            IntentResult intent = _intentResolver.Resolve(normalized);
            AuthorizationResult auth = _policyGuard.Authorize(intent.Intent, userCommand);

            if (auth.Decision == AuthorizationDecision.Blocked)
                return "❌ Comando não autorizado: " + userCommand;

            if (auth.Decision == AuthorizationDecision.RequiresConfirmation && !confirmed)
            {
                Publish(processId, "Política", "PolicyGuard", "Aguardando", auth.Message, 0.15);
                return "⚠️ " + auth.Message + " Responda explicitamente para confirmar a execução.";
            }

            SolutionEntry? hit = _memory.FindBestMatch(userCommand);
            if (hit != null)
            {
                Publish(processId, "Orquestração", "Memória", "Concluído", "Resultado recuperado da memória", 1);
                _logger.Info("[MEMÓRIA] hit " + hit.Id);
                return "💾 Memória:\n" + hit.ResultDetails;
            }

            for (int step = 1; step <= MaxSteps; step++)
            {
                ct.ThrowIfCancellationRequested();
                Publish(
                    processId,
                    "Orquestração",
                    "Planejamento",
                    "Executando",
                    "Passo " + step + "/" + MaxSteps + " — " + intent.Intent,
                    Math.Min(0.1 + step * 0.08, 0.3));

                ToolResult result;
                try
                {
                    ITool tool = _toolResolver.Resolve(intent.Intent);
                    result = await tool.ExecuteAsync(userCommand, intent.Parameters, ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.Error("[TOOL] " + ex.Message);
                    result = new ToolResult(false, "❌ Ferramenta falhou: " + ex.Message);
                }

                if (result.Success)
                {
                    _memory.Record(userCommand, intent.Intent, result.Output, success: true);
                    Publish(processId, "Orquestração", "Assistente", "Concluído", "Resultado revisado e entregue", 1);
                    return result.Output;
                }

                if (EnableFallback && _aiClient != null)
                {
                    try
                    {
                        string fallbackResult = await _aiClient.ChatAsync(userCommand, HttpClient, null, ct).ConfigureAwait(false);
                        string output = "🤖 IA fallback:\n" + fallbackResult;
                        _memory.Record(userCommand, "ai_fallback", output, success: true);
                        Publish(processId, "Orquestração", "IA", "Concluído", "Fallback entregue", 1);
                        return output;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.Error("[FALLBACK] " + ex.Message);
                        result = new ToolResult(false, result.Output + "\n\n❌ Fallback falhou: " + ex.Message);
                    }
                }

                Publish(processId, "Orquestração", "Assistente", "Falhou", result.Output, 1);
                return result.Output;
            }

            return "Limite de passos. Seja mais específico.";
        }

        private ToolResolver CreateToolResolver()
        {
            return new ToolResolver(new ITool[]
            {
                new DelegateTool("search", async (command, parameters, ct) =>
                {
                    string query = parameters.TryGetValue("query", out string? value) && !string.IsNullOrWhiteSpace(value)
                        ? value
                        : command;

                    Publish(
                        "tool:" + Guid.NewGuid().ToString("N"),
                        "Pesquisa",
                        "Browser",
                        "Pesquisando",
                        "Buscando e refinando informações",
                        0.35);

                    string result = await SearchWithRefinementAsync(query, ct).ConfigureAwait(false);
                    bool ok = !string.IsNullOrWhiteSpace(result) &&
                              !result.StartsWith("Falha na busca:", StringComparison.OrdinalIgnoreCase);
                    return new ToolResult(ok, result);
                }),
                new DelegateTool("execute", ExecuteExistingRunnerAsync),
                new DelegateTool("conversar", async (command, _, ct) =>
                {
                    if (EnableFallback && _aiClient != null)
                    {
                        try
                        {
                            return new ToolResult(
                                true,
                                await _aiClient.ChatAsync(command, HttpClient, null, ct).ConfigureAwait(false));
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _logger.Error("[CHAT] " + ex.Message);
                            return new ToolResult(false, "❌ IA indisponível: " + ex.Message);
                        }
                    }

                    string result = await SearchWithRefinementAsync(command, ct).ConfigureAwait(false);
                    return new ToolResult(!string.IsNullOrWhiteSpace(result), result);
                })
            });
        }

        private async Task<ToolResult> ExecuteExistingRunnerAsync(
            string command,
            Dictionary<string, string> parameters,
            CancellationToken ct)
        {
            string? path = ExtractFilePath(command);
            if (path == null || !_runner.CanRun(path))
                return new ToolResult(false, "❌ Arquivo não encontrado ou não suportado: " + (path ?? command));

            string id = "tool:" + Guid.NewGuid().ToString("N");
            Publish(id, "Execução", "Cells", "Executando", "Executando " + path, 0.55);

            try
            {
                Cell cell = await _runner.RunAsync(_runtime, null, path).ConfigureAwait(false);
                Publish(id, "Verificação", "Cells", "Revisando", "Validando célula " + cell.Id, 0.8);
                await Task.Delay(800, ct).ConfigureAwait(false);
                string log = _runtime.ReadCellLog(cell.Id, 40);
                return new ToolResult(true, "✅ Célula " + cell.Id + " [" + cell.State + "]\n" + log);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                string err = "❌ Execução: " + ex.Message;
                _memory.Record(command, "run_fail", err, success: false);
                return new ToolResult(false, err);
            }
        }

        public Task<string> SearchWithRefinementAsync(string query, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Task.FromResult("Nenhuma consulta informada.");

            return _webSearch.SearchWithRefinementAsync(query.Trim(), ct);
        }

        private void Publish(
            string id,
            string title,
            string target,
            string status,
            string message,
            double progress)
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

        private static string? ExtractFilePath(string text)
        {
            Match match = Regex.Match(
                text,
                @"(/[^\s]+?\.(py|sh|jar|dll|js|bash))",
                RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value;

            match = Regex.Match(
                text,
                @"([\w\./\\-]+\.(py|sh|jar|dll|js|bash))",
                RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
