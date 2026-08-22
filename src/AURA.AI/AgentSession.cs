using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Logging;
using AURA.Memory;

namespace AURA.AI
{
    /// <summary>
    /// Loop agêntico sobre o OpenRouterClient: envia a conversa com as
    /// ferramentas registradas, executa as chamadas de ferramenta solicitadas
    /// pelo modelo, anexa os resultados e repete até o modelo responder texto
    /// final. Com MemoryStore, cada turno é persistido em memory.json.
    /// </summary>
    public sealed class AgentSession
    {
        private const int MaxRounds = 8;

        private readonly OpenRouterClient _client;
        private readonly ILogger _logger;
        private readonly List<AgentTool> _tools;
        private readonly List<AgentMessage> _messages = new();

        private const int MaxHistoryMessages = 16;
        private readonly string? _systemPrompt;
        private readonly MemoryStore? _memory;

        public AgentSession(OpenRouterClient client, IEnumerable<AgentTool> tools,
            string? systemPrompt = null, ILogger? logger = null, MemoryStore? memory = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tools = (tools ?? Enumerable.Empty<AgentTool>()).ToList();
            _systemPrompt = systemPrompt;
            _logger = logger ?? new ConsoleLogger();
            _memory = memory;
        }

        private void TrimHistory()
        {
            if (_messages.Count <= MaxHistoryMessages)
                return;

            _messages.RemoveRange(0, _messages.Count - MaxHistoryMessages);
        }

        /// <summary>System prompt + resumo das últimas entradas persistidas.</summary>
        private string BuildSystemPrompt()
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(_systemPrompt))
                sb.Append(_systemPrompt.Trim());

            if (_memory == null)
                return sb.ToString();

            try
            {
                var entries = _memory.Read(tail: 12);
                sb.Append("\n\n## Memória persistente\n");
                sb.Append("Você TEM memória persistente em ");
                sb.Append(_memory.Path);
                sb.Append(". Cada pergunta/resposta é gravada automaticamente. ");
                sb.Append("Nunca diga que não tem memória. ");
                sb.Append("Para criar notas extras use write_file em memory-notes.md no workspace.\n");

                if (entries.Count == 0)
                {
                    sb.Append("(Ainda não há turnos gravados nesta instalação.)\n");
                }
                else
                {
                    sb.Append("Últimos turnos gravados:\n");
                    foreach (var e in entries)
                    {
                        if (e.Kind != MemoryKind.Turn)
                            continue;
                        string role = string.IsNullOrWhiteSpace(e.Role) ? "?" : e.Role;
                        string text = e.Text ?? string.Empty;
                        if (text.Length > 180)
                            text = text.Substring(0, 180) + "…";
                        sb.Append("- [");
                        sb.Append(role);
                        sb.Append("] ");
                        sb.Append(text);
                        sb.Append('\n');
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("agent: não foi possível ler memória: " + ex.Message);
            }

            return sb.ToString();
        }

        public event Action<AgentStep>? Step;

        public IReadOnlyList<AgentMessage> Messages => _messages;

        public async Task<string> RunAsync(string userText,
            HttpClient? httpClient = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                throw new ArgumentException("A instrução não pode ser vazia.", nameof(userText));
            }

            _messages.Add(new AgentMessage { Role = "user", Content = userText });
            _memory?.Append(MemoryEntry.Question(userText));

            string systemPrompt = BuildSystemPrompt();

            int round = 0;
            while (round++ < MaxRounds)
            {
                TrimHistory();

                AgentChatResponse response = await _client.ChatToolsAsync(
                    _messages,
                    _tools.Select(t => t.Definition).ToList(),
                    httpClient,
                    ct,
                    systemPrompt).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(response.Error))
                {
                    throw new InvalidOperationException(response.Error);
                }

                if (response.ToolCalls is { Count: > 0 })
                {
                    _messages.Add(new AgentMessage
                    {
                        Role = "assistant",
                        Content = null,
                        ToolCalls = response.ToolCalls,
                        ReasoningDetails = response.ReasoningDetails
                    });

                    foreach (AgentToolCall call in response.ToolCalls)
                    {
                        ct.ThrowIfCancellationRequested();
                        string result = await ExecuteToolAsync(call, ct).ConfigureAwait(false);
                        _messages.Add(new AgentMessage
                        {
                            Role = "tool",
                            ToolCallId = call.Id,
                            Content = result
                        });
                        Step?.Invoke(new AgentStep(call.Name, call.ArgumentsJson, result));
                        _logger.Info("agent: ferramenta='" + call.Name + "'");
                    }

                    continue;
                }

                string final = response.Content ?? "(resposta vazia)";
                _messages.Add(new AgentMessage { Role = "assistant", Content = final });
                _memory?.Append(MemoryEntry.Answer(final));
                return final;
            }

            throw new InvalidOperationException(
                "O agente atingiu o limite de " + MaxRounds + " passos de ferramentas.");
        }

        private async Task<string> ExecuteToolAsync(AgentToolCall call, CancellationToken ct)
        {
            AgentTool? tool = _tools.FirstOrDefault(t =>
                t.Definition.Name == call.Name);
            if (tool == null)
            {
                return "ERRO: ferramenta desconhecida: " + call.Name;
            }

            try
            {
                return await tool.ExecuteAsync(call.ArgumentsJson, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error("agent: falha na ferramenta '" + call.Name + "': " + ex.Message);
                return "ERRO na ferramenta " + call.Name + ": " + ex.Message;
            }
        }
    }
}
