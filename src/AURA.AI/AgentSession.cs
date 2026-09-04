using System.Text;
using AURA.AI.UniversalAI;
using AURA.Core.Logging;
using AURA.Memory;

namespace AURA.AI;

/// <summary>Loop agêntico independente de qualquer provider.</summary>
public sealed class AgentSession
{
    private readonly IUniversalAiClient _client;
    private readonly ILogger _logger;
    private readonly List<AgentTool> _tools;
    private readonly List<AgentMessage> _messages = new();
    private readonly string? _systemPrompt;
    private readonly MemoryStore? _memory;
    private readonly int _maxRounds;
    private readonly AgentRunStore _runStore;
    private AgentRunState? _runState;
    private const int MaxHistoryMessages = 16;

    private static readonly List<AgentMessage> SharedHistory = new();
    private static readonly object SharedGate = new();
    private static CancellationTokenSource? AmbientCts;
    private static readonly object AmbientGate = new();

    public AgentSession(IUniversalAiClient client, IEnumerable<AgentTool> tools, string? systemPrompt = null, ILogger? logger = null, MemoryStore? memory = null, int maxRounds = 12, AgentRunStore? runStore = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _tools = (tools ?? Array.Empty<AgentTool>()).ToList();
        _systemPrompt = systemPrompt;
        _logger = logger ?? new ConsoleLogger();
        _memory = memory;
        _maxRounds = Math.Max(1, maxRounds);
        _runStore = runStore ?? new AgentRunStore(_logger);

        lock (SharedGate)
        {
            if (SharedHistory.Count > 0)
                _messages.AddRange(SharedHistory);
        }
    }

    public event Action<AgentStep>? Step;
    public IReadOnlyList<AgentMessage> Messages => _messages;
    public string? RunId => _runState?.RunId;

    public static void ClearSharedHistory()
    {
        lock (SharedGate) SharedHistory.Clear();
    }

    public static CancellationTokenSource BeginAmbientRun()
    {
        lock (AmbientGate)
        {
            try { AmbientCts?.Cancel(); } catch { }
            try { AmbientCts?.Dispose(); } catch { }
            AmbientCts = new CancellationTokenSource();
            return AmbientCts;
        }
    }

    public static void CancelAmbientRun()
    {
        lock (AmbientGate)
        {
            try { AmbientCts?.Cancel(); } catch { }
        }
    }

    public Task<string> RunAsync(string userText, HttpClient? httpClient = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userText)) throw new ArgumentException("A instrução não pode ser vazia.", nameof(userText));

        if (IsResumeInstruction(userText))
            return ResumeLastAsync(httpClient, ct);

        var state = new AgentRunState
        {
            RunId = Guid.NewGuid().ToString("N"),
            Status = AgentRunStatus.Running,
            Goal = userText,
            Round = 0,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _runState = state;
        _messages.Add(new AgentMessage { Role = "user", Content = userText });
        _memory?.Append(MemoryEntry.Question(userText));
        Checkpoint();
        return ExecuteLoopAsync(httpClient, ct);
    }

    public Task<string> ResumeLastAsync(HttpClient? httpClient = null, CancellationToken ct = default)
    {
        var state = _runStore.LoadLatestResumable();
        if (state == null)
            return Task.FromResult("Não há nenhuma execução pausada para retomar.");

        _runState = state;
        _messages.Clear();
        _messages.AddRange(state.Messages ?? new List<AgentMessage>());
        PersistShared();
        state.Status = AgentRunStatus.Running;
        state.LastError = null;
        state.Round = 0;
        Checkpoint();
        return ExecuteLoopAsync(httpClient, ct);
    }

    public Task<string> ResumeAsync(string runId, HttpClient? httpClient = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(runId)) return Task.FromResult("RunId obrigatório.");
        var state = _runStore.Load(runId);
        if (state == null) return Task.FromResult("Execução não encontrada: " + runId);
        if (state.Status != AgentRunStatus.Paused) return Task.FromResult("A execução não está pausada: " + state.Status);

        _runState = state;
        _messages.Clear();
        _messages.AddRange(state.Messages ?? new List<AgentMessage>());
        PersistShared();
        state.Status = AgentRunStatus.Running;
        state.LastError = null;
        state.Round = 0;
        Checkpoint();
        return ExecuteLoopAsync(httpClient, ct);
    }

    private async Task<string> ExecuteLoopAsync(HttpClient? httpClient, CancellationToken ct)
    {
        CancellationToken ambient;
        lock (AmbientGate)
            ambient = AmbientCts?.Token ?? CancellationToken.None;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, ambient);
        var token = linked.Token;

        var executedSignatures = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            for (var round = 0; round < _maxRounds; round++)
            {
                token.ThrowIfCancellationRequested();
                if (_runState != null)
                {
                    _runState.Round = round;
                    Checkpoint();
                }

                TrimHistory();
                var response = await _client.ChatToolsAsync(
                    new List<AgentMessage>(_messages),
                    _tools.Select(t => t.Definition).ToList(),
                    httpClient,
                    token,
                    BuildSystemPrompt()).ConfigureAwait(false);

                token.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(response.Error))
                {
                    if (_runState != null)
                    {
                        _runState.Status = AgentRunStatus.Failed;
                        _runState.LastError = response.Error;
                        Checkpoint();
                    }
                    throw new AgentLlmException(response.Error, response.ErrorKind);
                }

                if (response.ToolCalls is { Count: > 0 })
                {
                    _messages.Add(new AgentMessage
                    {
                        Role = "assistant",
                        Content = response.Content,
                        ToolCalls = response.ToolCalls,
                        ReasoningDetailsJson = response.ReasoningDetailsJson
                    });

                    foreach (var call in response.ToolCalls)
                    {
                        token.ThrowIfCancellationRequested();
                        string signature = (call.Name ?? "") + "|" + (call.ArgumentsJson ?? "");
                        string result;

                        if (IsEmptyOrInvalidArguments(call.Name, call.ArgumentsJson))
                        {
                            result = "ERRO: argumentos inválidos ou vazios para " + call.Name + ". Informe path/conteúdo válidos.";
                        }
                        else if (!executedSignatures.Add(signature))
                        {
                            result = "ERRO: chamada duplicada ignorada (" + call.Name + "). Não repita a mesma ação com os mesmos argumentos.";
                        }
                        else
                        {
                            var tool = _tools.FirstOrDefault(t =>
                                string.Equals(t.Definition.Name, call.Name, StringComparison.OrdinalIgnoreCase));
                            if (tool == null)
                                result = "ERRO: ferramenta não encontrada: " + call.Name;
                            else
                            {
                                try
                                {
                                    result = await tool.ExecuteAsync(call.ArgumentsJson, token).ConfigureAwait(false);
                                }
                                catch (OperationCanceledException)
                                {
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    result = "ERRO: " + ex.Message;
                                }
                            }
                        }

                        _messages.Add(new AgentMessage { Role = "tool", ToolCallId = call.Id, Content = result });
                        Step?.Invoke(new AgentStep(call.Name, call.ArgumentsJson, result,
                            !result.StartsWith("ERRO:", StringComparison.OrdinalIgnoreCase)));
                        PersistShared();
                        if (_runState != null)
                        {
                            _runState.Round = round + 1;
                            Checkpoint();
                        }
                    }

                    continue;
                }

                var answer = response.Content ?? string.Empty;
                _messages.Add(new AgentMessage { Role = "assistant", Content = answer });
                _memory?.Append(MemoryEntry.Answer(answer));
                PersistShared();
                if (_runState != null)
                {
                    _runState.Status = AgentRunStatus.Completed;
                    _runState.Round = round + 1;
                    _runState.LastError = null;
                    Checkpoint();
                }
                return answer;
            }

            if (_runState != null)
            {
                _runState.Status = AgentRunStatus.Paused;
                _runState.LastError = "Limite de rodadas atingido; execução preservada para retomada.";
                Checkpoint();
            }
            return "Execução pausada no limite de rodadas. Estado salvo; use a retomada para continuar.";
        }
        catch (OperationCanceledException)
        {
            if (_runState != null)
            {
                _runState.Status = AgentRunStatus.Paused;
                _runState.LastError = "Execução interrompida; checkpoint preservado para retomada.";
                Checkpoint();
            }
            _logger.Info("agent: run pausado por cancelamento");
            return "Execução interrompida e pausada. Estado salvo; use a retomada para continuar.";
        }
    }

    private static bool IsEmptyOrInvalidArguments(string? name, string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(name)) return true;
        string n = name.Trim().ToLowerInvariant();
        string args = (argumentsJson ?? "").Trim();
        if (args.Length == 0 || args == "{}" || args == "null")
        {
            if (n is "write_file" or "edit_file" or "read_file" or "list_dir" or "run_shell" or "shell")
                return true;
        }
        return false;
    }

    private static bool IsResumeInstruction(string text)
    {
        string normalized = text.Trim().ToLowerInvariant();
        return normalized is "continue" or "continua" or "continuar" or "prosseguir"
            || normalized.StartsWith("continue de onde parou", StringComparison.Ordinal)
            || normalized.StartsWith("continuar de onde parou", StringComparison.Ordinal)
            || normalized.StartsWith("retome de onde parou", StringComparison.Ordinal);
    }

    private void Checkpoint()
    {
        if (_runState == null) return;
        _runState.Messages = new List<AgentMessage>(_messages);
        _runStore.Save(_runState);
    }

    private void PersistShared()
    {
        lock (SharedGate)
        {
            SharedHistory.Clear();
            SharedHistory.AddRange(_messages);
        }
    }

    private string BuildSystemPrompt()
    {
        var sb = new StringBuilder();
        sb.Append(DefaultAgentSystemPrompt.Merge(_systemPrompt));
        if (_tools.Count > 0)
        {
            sb.Append("\n\nFERRAMENTAS REGISTRADAS:\n");
            foreach (var t in _tools)
                sb.Append("- ").Append(t.Definition.Name).Append(": ").Append(t.Definition.Description).Append('\n');
        }

        if (_memory != null)
        {
            try
            {
                sb.Append("\nMemória persistente disponível em ").Append(_memory.Path).Append(".\n");
                foreach (var e in _memory.Read(tail: 8).Where(x => x.Kind == MemoryKind.Turn))
                    sb.Append("- [").Append(e.Role ?? "?").Append("] ")
                        .Append((e.Text ?? string.Empty).Replace('\n', ' ').Take(180)
                            .Aggregate(new StringBuilder(), (x, c) => x.Append(c))).Append('\n');
            }
            catch (Exception ex)
            {
                _logger.Warning("agent: memória indisponível: " + ex.Message);
            }
        }

        return sb.ToString();
    }

    private void TrimHistory()
    {
        if (_messages.Count <= MaxHistoryMessages) return;
        _messages.RemoveRange(0, _messages.Count - MaxHistoryMessages);
    }
}
