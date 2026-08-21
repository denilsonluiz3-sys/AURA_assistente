using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Abstractions;
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
    public sealed class AuraOrchestrator : AURA.Abstractions.Orchestration.IOrchestrator
    {
        private readonly ILogger _logger;
        private readonly SolutionStore _memory;
        private readonly Runner _runner;
        private readonly SimulationRuntime _runtime;
        private readonly HttpClient _http;
        private readonly EventBus? _events;
        private readonly IToolExecutor _shell;
        private readonly OpenRouterClient? _aiClient;
        private readonly IWebSearch _webSearch;
        private readonly IIntentResolver _intentResolver;
        private readonly PolicyGuard _policyGuard;
        private readonly CellProgramRegistry _cellRegistry;
        private readonly CellProgramRunner _cellRunner;
        private readonly IAuraCellContextFactory? _contextFactory;

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
            CellProgramRegistry? cellRegistry = null,
            CellProgramRunner? cellRunner = null,
            IAuraCellContextFactory? contextFactory = null)
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
            _cellRegistry = cellRegistry ?? new CellProgramRegistry();
            _cellRunner = cellRunner ?? new CellProgramRunner(_logger);
            _contextFactory = contextFactory;
        }

        public async Task<string> ExecuteAsync(string userCommand, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userCommand)) return "Comando vazio.";

            userCommand = userCommand.Trim();
            string processId = "orchestration:" + Guid.NewGuid().ToString("N");
            _logger.Info("[ORQUESTRA] " + userCommand);
            Publish(processId, "Orquestração", "Assistente", "Executando", "Entendendo solicitação", 0.05);

            SolutionEntry hit = _memory.FindBestMatch(userCommand);
            if (hit != null)
            {
                Publish(processId, "Orquestração", "Memória", "Concluído", "Resultado recuperado da memória", 1);
                _logger.Info("[MEMÓRIA] hit " + hit.Id);
                return "💾 Memória:\n" + hit.ResultDetails;
            }

            var intent = _intentResolver.Resolve(userCommand);
            if (intent.Intent == "android" &&
                intent.Parameters.TryGetValue("action", out string? action) &&
                _cellRegistry.Resolve(action) is IAuraCellProgram program)
            {
                if (_contextFactory is null)
                    return "❌ Programa AURA indisponível: contexto nativo não registrado.";

                var auth = _policyGuard.Authorize(program.RequiredCapabilities, userCommand);
                if (auth.Decision == AuthorizationDecision.Blocked)
                    return "❌ " + auth.Message;
                if (auth.Decision == AuthorizationDecision.RequiresConfirmation)
                    return "⚠️ " + auth.Message;

                string cellId = "program-" + Guid.NewGuid().ToString("N");
                IAuraCellContext context = _contextFactory.Create(cellId, ct);
                CellProgramResult result = await _cellRunner.RunAsync(program, context, ct);
                if (!result.IsSuccess) return "❌ " + result.Error;

                string serialized = JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                Publish(processId, "Programa AURA", program.Name, "Concluído", "Programa executado", 1);
                return serialized;
            }

            Publish(processId, "Orquestração", "Planejamento", "Executando", "Criando plano de execução", 0.15);

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

            string systemPrompt =
                "Você é o orquestrador da AURA. Use as ferramentas para executar tarefas.\n" +
                "FLUXO: interpret_command → search_memory → web_search → extract_code → execute_code\n" +
                "Workspace: " + workspace;

            var session = new AgentSession(client, tools, systemPrompt, _logger);
            session.Step += step => OnAgentStep(processId, step);
            Publish(processId, "Orquestração", "IA", "Executando", "Processando com IA...", 0.3);

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

            if (!result.StartsWith("❌"))
            {
                _memory.Record(userCommand, "orchestration", result, success: true);
                _logger.Info("[MEMÓRIA] Registrado: " + userCommand);
            }

            return result;
        }

        Task<string> AURA.Abstractions.Orchestration.IOrchestrator.ExecuteAsync(string userCommand, CancellationToken cancellationToken, bool confirmed)
            => ExecuteAsync(userCommand, cancellationToken);

        private void OnAgentStep(string processId, AgentStep step)
        {
            string resultPreview = step.Result.Length > 100 ? step.Result.Substring(0, 100) + "..." : step.Result;
            Publish(processId, step.ToolName, "Ferramenta", "Executando", resultPreview, 0.5);
            _logger.Info("[AGENT] " + step.ToolName + ": " + step.Arguments);
        }

        private string AgentWorkspace()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string workspace = Path.Combine(home, "AURA", "workspace");
            try { Directory.CreateDirectory(workspace); } catch { }
            return workspace;
        }

        private void Publish(string id, string title, string target, string status, string message, double progress)
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
