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
    private const int MaxHistoryMessages = 16;

    /// <summary>
    /// Histórico entre recriações de sessão (AgentPage zera _session a cada run).
    /// </summary>
    private static readonly List<AgentMessage> SharedHistory = new();
    private static readonly object SharedGate = new();

    /// <summary>
    /// Token do run atual (UI stop sem precisar passar CT em todo o AgentPage legado).
    /// </summary>
    private static CancellationTokenSource? AmbientCts;
    private static readonly object AmbientGate = new();

    public AgentSession(IUniversalAiClient client, IEnumerable<AgentTool> tools, string? systemPrompt = null, ILogger? logger = null, MemoryStore? memory = null, int maxRounds = 12)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _tools = (tools ?? Array.Empty<AgentTool>()).ToList();
        _systemPrompt = systemPrompt;
        _logger = logger ?? new ConsoleLogger();
        _memory = memory;
        _maxRounds = Math.Max(1, maxRounds);

        lock (SharedGate)
        {
            if (SharedHistory.Count > 0)
                _messages.AddRange(SharedHistory);
        }
    }

    public event Action<AgentStep>? Step;
    public IReadOnlyList<AgentMessage> Messages => _messages;

    public static void ClearSharedHistory()
    {
        lock (SharedGate) SharedHistory.Clear();
    }

    /// <summary>Inicia um token de cancelamento para o próximo RunAsync (chamado pela UI antes do run).</summary>
    public static CancellationTokenSource BeginAmbientRun()
    {
        lock (AmbientGate)
        {
            try { AmbientCts?.Cancel(); } catch { /* ignore */ }
            try { AmbientCts?.Dispose(); } catch { /* ignore */ }
            AmbientCts = new CancellationTokenSource();
            return AmbientCts;
        }
    }

    /// <summary>Cancela o run em andamento (botão ■).</summary>
    public static void CancelAmbientRun()
    {
        lock (AmbientGate)
        {
            try { AmbientCts?.Cancel(); } catch { /* ignore */ }
        }
    }

    public async Task<string> RunAsync(string userText, HttpClient? httpClient = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userText)) throw new ArgumentException("A instrução não pode ser vazia.", nameof(userText));

        CancellationToken ambient;
        lock (AmbientGate)
            ambient = AmbientCts?.Token ?? CancellationToken.None;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, ambient);
        var token = linked.Token;

        token.ThrowIfCancellationRequested();

        _messages.Add(new AgentMessage { Role = "user", Content = userText });
        _memory?.Append(MemoryEntry.Question(userText));

        try
        {
            for (var round = 0; round < _maxRounds; round++)
            {
                token.ThrowIfCancellationRequested();
                TrimHistory();

                var response = await _client.ChatToolsAsync(
                    new List<AgentMessage>(_messages),
                    _tools.Select(t => t.Definition).ToList(),
                    httpClient,
                    token,
                    BuildSystemPrompt()).ConfigureAwait(false);

                token.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(response.Error))
                    throw new AgentLlmException(response.Error, response.ErrorKind);

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
                        var tool = _tools.FirstOrDefault(t =>
                            string.Equals(t.Definition.Name, call.Name, StringComparison.OrdinalIgnoreCase));
                        string result;
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

                        _messages.Add(new AgentMessage { Role = "tool", ToolCallId = call.Id, Content = result });
                        Step?.Invoke(new AgentStep(call.Name, call.ArgumentsJson, result,
                            !result.StartsWith("ERRO:", StringComparison.OrdinalIgnoreCase)));
                    }

                    PersistShared();
                    continue;
                }

                var answer = response.Content ?? string.Empty;
                _messages.Add(new AgentMessage { Role = "assistant", Content = answer });
                _memory?.Append(MemoryEntry.Answer(answer));
                PersistShared();
                return answer;
            }

            throw new AgentLlmException("O agente atingiu o limite de rodadas.", AgentErrorKind.Unknown);
        }
        catch (OperationCanceledException)
        {
            // Não grava resposta parcial como se fosse sucesso
            _logger.Info("agent: run cancelado pelo usuário");
            return "⏹ Execução interrompida.";
        }
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
