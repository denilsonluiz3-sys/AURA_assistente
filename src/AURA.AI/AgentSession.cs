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

    public AgentSession(IUniversalAiClient client, IEnumerable<AgentTool> tools, string? systemPrompt = null, ILogger? logger = null, MemoryStore? memory = null, int maxRounds = 12)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _tools = (tools ?? Array.Empty<AgentTool>()).ToList();
        _systemPrompt = systemPrompt;
        _logger = logger ?? new ConsoleLogger();
        _memory = memory;
        _maxRounds = Math.Max(1, maxRounds);
    }

    public event Action<AgentStep>? Step;
    public IReadOnlyList<AgentMessage> Messages => _messages;

    public async Task<string> RunAsync(string userText, HttpClient? httpClient = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userText)) throw new ArgumentException("A instrução não pode ser vazia.", nameof(userText));
        _messages.Add(new AgentMessage { Role = "user", Content = userText });
        _memory?.Append(MemoryEntry.Question(userText));

        for (var round = 0; round < _maxRounds; round++)
        {
            TrimHistory();
            var response = await _client.ChatToolsAsync(new List<AgentMessage>(_messages), _tools.Select(t => t.Definition).ToList(), httpClient, ct, BuildSystemPrompt()).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(response.Error)) throw new AgentLlmException(response.Error, response.ErrorKind);

            if (response.ToolCalls is { Count: > 0 })
            {
                _messages.Add(new AgentMessage { Role = "assistant", Content = response.Content, ToolCalls = response.ToolCalls, ReasoningDetailsJson = response.ReasoningDetailsJson });
                foreach (var call in response.ToolCalls)
                {
                    ct.ThrowIfCancellationRequested();
                    var tool = _tools.FirstOrDefault(t => string.Equals(t.Definition.Name, call.Name, StringComparison.OrdinalIgnoreCase));
                    string result;
                    if (tool == null) result = "ERRO: ferramenta não encontrada: " + call.Name;
                    else
                    {
                        try { result = await tool.ExecuteAsync(call.ArgumentsJson, ct).ConfigureAwait(false); }
                        catch (Exception ex) { result = "ERRO: " + ex.Message; }
                    }
                    _messages.Add(new AgentMessage { Role = "tool", ToolCallId = call.Id, Content = result });
                    Step?.Invoke(new AgentStep(call.Name, call.ArgumentsJson, result, !result.StartsWith("ERRO:", StringComparison.OrdinalIgnoreCase)));
                }
                continue;
            }

            var answer = response.Content ?? string.Empty;
            _messages.Add(new AgentMessage { Role = "assistant", Content = answer });
            _memory?.Append(MemoryEntry.Answer(answer));
            return answer;
        }
        throw new AgentLlmException("O agente atingiu o limite de rodadas.", AgentErrorKind.Unknown);
    }

    private string BuildSystemPrompt()
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(_systemPrompt)) sb.Append(_systemPrompt.Trim());
        if (_tools.Count > 0)
        {
            sb.Append("\n\nFERRAMENTAS REGISTRADAS:\n");
            foreach (var t in _tools) sb.Append("- ").Append(t.Definition.Name).Append(": ").Append(t.Definition.Description).Append('\n');
        }
        if (_memory != null)
        {
            try
            {
                sb.Append("\nMemória persistente disponível em ").Append(_memory.Path).Append(".\n");
                foreach (var e in _memory.Read(tail: 8).Where(x => x.Kind == MemoryKind.Turn))
                    sb.Append("- [").Append(e.Role ?? "?").Append("] ").Append((e.Text ?? string.Empty).Replace('\n', ' ').Take(180).Aggregate(new StringBuilder(), (x, c) => x.Append(c))).Append('\n');
            }
            catch (Exception ex) { _logger.Warning("agent: memória indisponível: " + ex.Message); }
        }
        return sb.ToString();
    }

    private void TrimHistory()
    {
        if (_messages.Count <= MaxHistoryMessages) return;
        _messages.RemoveRange(0, _messages.Count - MaxHistoryMessages);
    }
}
