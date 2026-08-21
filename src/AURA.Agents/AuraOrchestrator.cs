using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;
using AURA.Abstractions.Execution;
using AURA.Abstractions.Orchestration;
using AURA.AI;
using AURA.Core.Abstractions;
using AURA.Core.Events;
using AURA.Core.Launchers;
using AURA.Core.Logging;
using AURA.Core.Runtime;
using AURA.Memory;

namespace AURA.Agents;

/// <summary>
/// Kernel/orquestrador da AURA: memória → intenção → política → ferramenta → resultado.
/// O caminho determinístico local é executado antes do fallback de IA.
/// </summary>
public sealed class AuraOrchestrator : IKernel
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
    private readonly ToolResolver _toolResolver;

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
        IAndroidCapabilityService? android = null)
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
        _intentResolver = new HeuristicIntentResolver();
        _policyGuard = new PolicyGuard();

        var tools = new List<ITool>
        {
            new KernelSearchTool(_webSearch),
            new KernelShellTool(_shell),
            new KernelFileTool(AgentWorkspace()),
            new KernelConversationTool()
        };
        if (android != null)
            tools.Add(new AndroidKernelTool(android));

        _toolResolver = new ToolResolver(tools);
    }

    public async Task<string> ExecuteAsync(string userCommand, CancellationToken ct = default, bool confirmed = false)
    {
        if (string.IsNullOrWhiteSpace(userCommand))
            return "Comando vazio.";

        userCommand = userCommand.Trim();
        string processId = "kernel:" + Guid.NewGuid().ToString("N");
        _logger.Info("[KERNEL] " + userCommand);
        Publish(processId, "Kernel", "AURA", "Executando", "Entendendo solicitação", 0.05);

        SolutionEntry hit = _memory.FindBestMatch(userCommand);
        if (hit != null)
        {
            Publish(processId, "Memória", "SolutionStore", "Concluído", "Resultado recuperado", 1);
            return hit.ResultDetails;
        }

        IntentResult intent = _intentResolver.Resolve(Normalize(userCommand));
        _logger.Info($"[KERNEL] intent={intent.Intent} confidence={intent.Confidence:0.00}");

        if (intent.Intent != "conversar" && intent.Confidence >= 0.70)
        {
            string toolIntent = intent.Intent;
            var parameters = new Dictionary<string, string>(intent.Parameters, StringComparer.OrdinalIgnoreCase);

            if (toolIntent == "execute")
            {
                toolIntent = "shell";
                if (!parameters.ContainsKey("query")) parameters["query"] = userCommand;
            }
            else if (toolIntent == "create_file" || toolIntent == "list_files")
            {
                toolIntent = "file";
                parameters["action"] = toolIntent == "file" && intent.Intent == "create_file" ? "write" : "list";
                if (parameters.TryGetValue("query", out var path) && !parameters.ContainsKey("path")) parameters["path"] = path;
            }

            AuthorizationResult authorization = _policyGuard.Authorize(intent.Intent, userCommand);
            if (authorization.Decision == AuthorizationDecision.Blocked)
                return "Ação bloqueada: " + authorization.Message;

            if (authorization.Decision == AuthorizationDecision.RequiresConfirmation && !confirmed)
                return "CONFIRMAÇÃO NECESSÁRIA: " + authorization.Message;

            ITool tool = _toolResolver.Resolve(toolIntent);
            Publish(processId, "Kernel", tool.Intent, "Executando", "Executando capacidade local", 0.50);

            ToolResult result = await tool.ExecuteAsync(userCommand, parameters, ct);
            if (result.Success)
            {
                _memory.Record(userCommand, intent.Intent, result.Output, success: true);
                Publish(processId, "Kernel", tool.Intent, "Concluído", "Resultado entregue", 1);
                return result.Output;
            }

            Publish(processId, "Kernel", tool.Intent, "Falhou", result.Output, 1);
            return "❌ " + result.Output;
        }

        return await ExecuteAiFallbackAsync(userCommand, processId, ct);
    }

    private async Task<string> ExecuteAiFallbackAsync(string userCommand, string processId, CancellationToken ct)
    {
        var tools = new List<AgentTool>
        {
            new InterpretCommandTool(),
            new SearchMemoryTool(_memory),
            new ListDirTool(AgentWorkspace()),
            new ReadFileTool(AgentWorkspace()),
            new WriteFileTool(AgentWorkspace()),
            new EditFileTool(AgentWorkspace()),
            new ShellAgentTool(AgentWorkspace(), _shell),
            new WebFetchTool(),
            new WebSearchTool(_webSearch),
            new CodeExtractorTool(_webSearch, _aiClient),
            new CodeExecutorTool(_shell, AgentWorkspace())
        };

        string apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? "";
        var client = _aiClient ?? new OpenRouterClient(new OpenRouterOptions
        {
            ApiKey = apiKey,
            Model = string.IsNullOrEmpty(apiKey) ? "openrouter/free" : "qwen/qwen-plus",
            MaxTokens = 2000,
            TimeoutSeconds = 90,
            AppReference = "AURA-Kernel"
        });

        string systemPrompt =
            "Você é o fallback conversacional da AURA. O Kernel já tentou resolver a solicitação localmente. " +
            "Use ferramentas apenas quando a intenção não puder ser resolvida deterministicamente. Workspace: " + AgentWorkspace();

        var session = new AgentSession(client, tools, systemPrompt, _logger);
        session.Step += step =>
        {
            string preview = step.Result.Length > 100 ? step.Result.Substring(0, 100) + "..." : step.Result;
            Publish(processId, step.ToolName, "Fallback IA", "Executando", preview, 0.5);
        };

        Publish(processId, "Kernel", "Fallback IA", "Executando", "Solicitação não determinística", 0.3);
        try
        {
            string result = await session.RunAsync(userCommand, _http, ct);
            if (!result.StartsWith("❌", StringComparison.Ordinal))
                _memory.Record(userCommand, "ai_fallback", result, success: true);
            Publish(processId, "Kernel", "Fallback IA", "Concluído", "Resultado entregue", 1);
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error("[KERNEL] Fallback IA: " + ex.Message);
            Publish(processId, "Kernel", "Fallback IA", "Falhou", ex.Message, 1);
            return "❌ Erro ao processar: " + ex.Message;
        }
    }

    private static string Normalize(string value) =>
        value.Trim().ToLowerInvariant();

    private string AgentWorkspace()
    {
        string workspace = AgentWorkspace.ActiveRoot;
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
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "AURA/1.0 Android");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9,en;q=0.8");
        return client;
    }
}
