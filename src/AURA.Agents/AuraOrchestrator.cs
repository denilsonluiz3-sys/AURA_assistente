using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Abstractions;
using AURA.Core.Events;
using AURA.Core.Logging;
using AURA.Core.Launchers;
using AURA.Core.Runtime;
using AURA.Memory;
using AURA.Abstractions.Execution;
using AURA.AI;

namespace AURA.Agents
{
    public sealed class AuraOrchestrator
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

        public AuraOrchestrator(
            ILogger logger,
            SolutionStore memory,
            Runner runner,
            SimulationRuntime runtime,
            IToolExecutor shell,
            IWebSearch webSearch,
            OpenRouterClient? aiClient = null,
            HttpClient? httpClient = null,
            EventBus? events = null)
        {
            _logger = logger ?? new ConsoleLogger();
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _webSearch = webSearch ?? throw new ArgumentNullException(nameof(webSearch));
            _aiClient = aiClient;
            _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            _events = events;
        }

        public async Task<string> ExecuteAsync(string userCommand, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userCommand))
                return "Comando vazio.";

            userCommand = userCommand.Trim();
            _logger.Info("[ORQUESTRA] " + userCommand);

            SolutionEntry hit = _memory.FindBestMatch(userCommand);
            if (hit != null)
            {
                _logger.Info("[MEMÓRIA] hit " + hit.Id);
                return "💾 Memória:\n" + hit.ResultDetails;
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
                TimeoutSeconds = 90
            });

            string systemPrompt = 
                "Você é o orquestrador da AURA. Use as ferramentas para executar tarefas.\n" +
                "FLUXO: interpret_command → search_memory → web_search → extract_code → execute_code\n" +
                "Workspace: " + workspace;

            var session = new AgentSession(client, tools, systemPrompt, _logger);
            string result = await session.RunAsync(userCommand, _http, ct);

            if (!result.StartsWith("❌"))
            {
                _memory.Record(userCommand, "orchestration", result, success: true);
            }

            return result;
        }

        private string AgentWorkspace()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string workspace = Path.Combine(home, "AURA", "workspace");
            try { Directory.CreateDirectory(workspace); } catch { }
            return workspace;
        }
    }
}
