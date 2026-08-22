using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;
using AURA.Abstractions.Orchestration;
using AURA.Core.Events;
using AURA.Core.Logging;
using AURA.Core.Launchers;
using AURA.Core.Runtime;
using AURA.Memory;
using AURA.Abstractions;
using AURA.Abstractions.Execution;
using AURA.AI;
using AURA.Agents.Programs;

namespace AURA.Agents
{
    /// <summary>
    /// Loop Sense → Plan → Act → Verify com decisão local determinística.
    /// IA externa, quando configurada, existe somente como fallback opcional.
    /// </summary>
    public sealed class AuraOrchestrator : IOrchestrator
    {
        private readonly ILogger _logger;
        private readonly SolutionStore _memory;
        private readonly Runner _runner;
        private readonly SimulationRuntime _runtime;
        private readonly HttpClient _http;
        private readonly EventBus? _events;
        private readonly IIntentResolver _intentResolver;
        private readonly PolicyGuard _policyGuard;
        private readonly ToolResolver _toolResolver;
        private readonly IAiClient? _fallbackClient;
        private readonly bool _enableFallback;

        public AuraOrchestrator(
            ILogger logger,
            SolutionStore memory,
            Runner runner,
            SimulationRuntime runtime,
            IToolExecutor shell,
            IWebSearch webSearch,
            OpenRouterClient? aiClient = null,
            HttpClient? httpClient = null,
            EventBus? events = null,
            IIntentResolver? intentResolver = null,
            PolicyGuard? policyGuard = null,
            ToolResolver? toolResolver = null,
            IAiClient? fallbackClient = null,
            bool enableFallback = false)
        {
            _logger = logger ?? new ConsoleLogger();
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _webSearch = webSearch ?? throw new ArgumentNullException(nameof(webSearch));
            _aiClient = aiClient;
            _http = httpClient ?? CreateAntiDetectClient();
            _events = events;
            _intentResolver = intentResolver ?? new HeuristicIntentResolver();
            _policyGuard = policyGuard ?? new PolicyGuard();
            _toolResolver = toolResolver ?? CreateToolResolver();
            _fallbackClient = fallbackClient;
            _enableFallback = enableFallback && fallbackClient != null;
        }

        public async Task<string> ExecuteAsync(string userCommand, CancellationToken ct = default, bool confirmed = false)
        {
            if (string.IsNullOrWhiteSpace(userCommand)) return "Comando vazio.";

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

            SolutionEntry hit = _memory.FindBestMatch(userCommand);
            if (hit != null)
            {
                Publish(processId, "Orquestração", "Memória", "Concluído", "Resultado recuperado da memória", 1);
                _logger.Info("[MEMÓRIA] hit " + hit.Id);
                return "💾 Memória:\n" + hit.ResultDetails;
            }

            for (int step = 1; step <= MaxSteps; step++)
            {
                ct.ThrowIfCancellationRequested();
                Publish(processId, "Orquestração", "Planejamento", "Executando", "Passo " + step + "/" + MaxSteps + " — " + intent.Intent, Math.Min(0.1 + step * 0.08, 0.3));

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

                if (_enableFallback && _fallbackClient != null)
                {
                    try
                    {
                        string fallbackResult = await _fallbackClient.ChatAsync(userCommand, ct).ConfigureAwait(false);
                        return "🤖 IA fallback:\n" + fallbackResult;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.Error("[FALLBACK] " + ex.Message);
                        return result.Output + "\n\n❌ Fallback falhou: " + ex.Message;
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
                    Publish("tool:" + Guid.NewGuid().ToString("N"), "Pesquisa", "Browser", "Pesquisando", "Buscando e refinando informações", 0.35);
                    string result = await SearchWithRefinementAsync(query, ct).ConfigureAwait(false);
                    return new ToolResult(!string.IsNullOrWhiteSpace(result) && !result.StartsWith("Falha na busca:", StringComparison.OrdinalIgnoreCase), result);
                }),
                new DelegateTool("execute", ExecuteExistingRunnerAsync),
                new DelegateTool("conversar", async (command, _, ct) =>
                {
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
            string path = ExtractFilePath(command);
            if (path == null || !_runner.CanRun(path))
                return new ToolResult(false, "❌ Arquivo não encontrado ou não suportado: " + (path ?? command));

            Publish("tool:" + Guid.NewGuid().ToString("N"), "Execução", "Cells", "Executando", "Executando " + path, 0.55);
            try
            {
                Cell cell = await _runner.RunAsync(_runtime, null, path).ConfigureAwait(false);
                Publish("tool:" + Guid.NewGuid().ToString("N"), "Verificação", "Cells", "Revisando", "Validando célula " + cell.Id, 0.8);
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

            string workspace = AgentWorkspace();
            var tools = new List<AgentTool>
            {
                new InterpretCommandTool(),
                new SearchMemoryTool(_memory),
                new ListDirTool(workspace),
                new ReadFileTool(workspace),
                new WriteFileTool(workspace),
                new EditFileTool(workspace),
                new ShellAgentTool(workspace, _shell),
                new WebFetchTool(),
                new WebSearchTool(_webSearch),
                new CodeExtractorTool(_webSearch, _aiClient),
                new CodeExecutorTool(_shell, workspace)
            };

            string apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? "";
            var client = _aiClient ?? new OpenRouterClient(new OpenRouterOptions
            {
                ApiKey = apiKey,
                Model = string.IsNullOrEmpty(apiKey) ? "openrouter/free" : "qwen/qwen-plus",
                MaxTokens = 2000,
                TimeoutSeconds = 90,
                AppReference = "AURA-Orchestrator"
            });

        public async Task<string> SearchWithRefinementAsync(string query, CancellationToken ct = default)
        {
            string current = query;
            for (int i = 0; i <= 2; i++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await Task.Delay(Random.Shared.Next(400, 1200), ct).ConfigureAwait(false);
                    List<(string Title, string Url)> results = await SearchDuckDuckGoLiteAsync(current, ct).ConfigureAwait(false);
                    if (results.Count > 0)
                        return FormatResults(results);
                    current = RefineQuery(query, i);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.Warning("[SEARCH] " + ex.Message);
                    if (i == 2) return "Falha na busca: " + ex.Message;
                }
            }
            return "Nenhum resultado após refinamentos.";
        }

        private async Task<List<(string Title, string Url)>> SearchDuckDuckGoLiteAsync(string query, CancellationToken ct)
        {
            string url = "https://lite.duckduckgo.com/lite/?q=" + Uri.EscapeDataString(query);
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Referer", "https://lite.duckduckgo.com/");
            using HttpResponseMessage resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            string result;
            try
            {
                result = await session.RunAsync(userCommand, _http, ct);
                Publish(processId, "Orquestração", "Assistente", "Concluído", "Resultado entregue", 1);
            }
            catch (Exception ex)
            {
                _logger.Error("[ORQUESTRA] Erro: " + ex.Message);
                Publish(processId, "Orquestração", "Assistente", "Falhou", ex.Message, 1);
                result = "❌ Erro ao processar: " + ex.Message;
            }
            return list;
        }

        private static string FormatResults(List<(string Title, string Url)> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Resultados da Web:");
            foreach ((string Title, string Url) r in results)
                sb.AppendLine("- **" + r.Title + "**: " + r.Url);
            return sb.ToString();
        }

            if (!result.StartsWith("❌"))
            {
                _memory.Record(userCommand, "orchestration", result, success: true);
                _logger.Info("[MEMÓRIA] Registrado: " + userCommand);
            }

        private static string? ExtractFilePath(string t)
        {
            Match m = Regex.Match(t, @"(/[^\s]+?\.(py|sh|jar|dll|js|bash))", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;
            m = Regex.Match(t, @"([\w\./\\-]+\.(py|sh|jar|dll|js|bash))", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static HttpClient CreateAntiDetectClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/120.0.0.0 Mobile Safari/537.36");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9,en;q=0.8");
            return client;
        }
    }
}
