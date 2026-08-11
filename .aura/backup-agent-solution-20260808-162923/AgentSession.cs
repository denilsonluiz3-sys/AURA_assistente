using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using AURA.Core.Logging;

namespace AURA.AI
{
    /// <summary>
    /// Loop agêntico sobre o OpenRouterClient: envia a conversa com as
    /// ferramentas registradas, executa as chamadas de ferramenta solicitadas
    /// pelo modelo, anexa os resultados e repete até o modelo responder texto
    /// final (estilo opencode/agentes de terminal).
    /// </summary>
    public sealed class AgentSession
    {
        private const int MaxRounds = 20;

        private readonly OpenRouterClient _client;
        private readonly ILogger _logger;
        private readonly List<AgentTool> _tools;
        private readonly List<AgentMessage> _messages = new();
        private readonly string? _systemPrompt;

        public AgentSession(OpenRouterClient client, IEnumerable<AgentTool> tools,
            string? systemPrompt = null, ILogger? logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _tools = (tools ?? Enumerable.Empty<AgentTool>()).ToList();
            _systemPrompt = systemPrompt;
            _logger = logger ?? new ConsoleLogger();
        }

        /// <summary>Emitido a cada ferramenta executada (para atualizar a UI).</summary>
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

            int round = 0;
            while (round++ < MaxRounds)
            {
                AgentChatResponse response = await _client.ChatToolsAsync(
                    _messages,
                    _tools.Select(t => t.Definition).ToList(),
                    httpClient,
                    ct,
                    _systemPrompt).ConfigureAwait(false);

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
                        ToolCalls = response.ToolCalls
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
                return final;
            }

            throw new InvalidOperationException(
                "O agente atingiu o limite de " + MaxRounds + " passos de ferramentas.");
        }

        private async Task<string> ExecuteToolAsync(
            AgentToolCall call,
            CancellationToken ct)
        {
            AgentTool? tool = _tools.FirstOrDefault(
                t => t.Definition.Name == call.Name);

            if (tool == null)
            {
                return "ERRO: ferramenta desconhecida: " +
                       call.Name;
            }

            try
            {
                string normalized =
                    NormalizeToolArguments(
                        call.Name,
                        call.ArgumentsJson);

                _logger.Info(
                    "agent: argumentos normalizados='" +
                    normalized + "'");

                return await tool.ExecuteAsync(
                    normalized,
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "agent: falha na ferramenta '" +
                    call.Name +
                    "': " +
                    ex.Message);

                return "ERRO na ferramenta " +
                       call.Name +
                       ": " +
                       ex.Message;
            }
        }

        private static string NormalizeToolArguments(
            string toolName,
            string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return DefaultToolArguments(toolName);
            }

            string json = raw.Trim();

            // Remove markdown ```json ... ```
            if (json.StartsWith("```"))
            {
                int nl = json.IndexOf('\n');

                if (nl >= 0)
                {
                    json = json.Substring(nl + 1);
                }

                int fence = json.LastIndexOf("```");

                if (fence >= 0)
                {
                    json = json.Substring(0, fence);
                }

                json = json.Trim();
            }

            try
            {
                using JsonDocument doc =
                    JsonDocument.Parse(json);

                JsonElement root = doc.RootElement;

                // Qwen às vezes devolve:
                // [{"path":"teste.txt"}]
                if (root.ValueKind ==
                    JsonValueKind.Array &&
                    root.GetArrayLength() > 0)
                {
                    root = root[0];
                }

                if (root.ValueKind !=
                    JsonValueKind.Object)
                {
                    return DefaultToolArguments(toolName);
                }

                var output =
                    new Dictionary<string, object?>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (JsonProperty property
                    in root.EnumerateObject())
                {
                    JsonElement value =
                        property.Value;

                    if (value.ValueKind ==
                        JsonValueKind.String)
                    {
                        output[property.Name] =
                            value.GetString();

                        continue;
                    }

                    if (value.ValueKind ==
                        JsonValueKind.Number)
                    {
                        output[property.Name] =
                            value.ToString();

                        continue;
                    }

                    if (value.ValueKind ==
                            JsonValueKind.True ||
                        value.ValueKind ==
                            JsonValueKind.False)
                    {
                        output[property.Name] =
                            value.GetBoolean();

                        continue;
                    }

                    // Corrige:
                    //
                    // "path": {
                    //   "type": "string",
                    //   "description": "."
                    // }
                    //
                    if (value.ValueKind ==
                        JsonValueKind.Object)
                    {
                        if (value.TryGetProperty(
                                "value",
                                out JsonElement val) &&
                            val.ValueKind ==
                                JsonValueKind.String)
                        {
                            output[property.Name] =
                                val.GetString();

                            continue;
                        }

                        if (value.TryGetProperty(
                                "description",
                                out JsonElement desc) &&
                            desc.ValueKind ==
                                JsonValueKind.String)
                        {
                            // O modelo pode devolver o schema da
                            // propriedade em vez do argumento real.
                            //
                            // Exemplo:
                            // "path": {
                            //   "type": "string",
                            //   "description": "Caminho relativo..."
                            // }
                            //
                            // A descrição NÃO deve virar o valor
                            // do argumento.

                            if (value.TryGetProperty(
                                    "type",
                                    out JsonElement type) &&
                                type.ValueKind ==
                                    JsonValueKind.String)
                            {
                                string typeName =
                                    type.GetString() ?? "";

                                if (typeName.Equals(
                                        "string",
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    // list_dir sem valor real =
                                    // raiz do workspace.
                                    if (property.Name.Equals(
                                            "path",
                                            StringComparison.OrdinalIgnoreCase) &&
                                        toolName.Equals(
                                            "list_dir",
                                            StringComparison.OrdinalIgnoreCase))
                                    {
                                        output[property.Name] = ".";
                                    }
                                    else
                                    {
                                        output[property.Name] = "";
                                    }

                                    continue;
                                }
                            }

                            // Não usar a descrição de um schema
                            // como argumento da ferramenta.
                            output[property.Name] = "";
                            continue;
                        }

                        if (value.TryGetProperty(
                                "default",
                                out JsonElement def) &&
                            def.ValueKind ==
                                JsonValueKind.String)
                        {
                            output[property.Name] =
                                def.GetString();

                            continue;
                        }

                        output[property.Name] = "";
                        continue;
                    }
                }

                // list_dir
                if (toolName.Equals(
                        "list_dir",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!output.TryGetValue(
                            "path",
                            out object? path) ||
                        string.IsNullOrWhiteSpace(
                            Convert.ToString(path)))
                    {
                        output["path"] = ".";
                    }
                }

                // read_file
                if (toolName.Equals(
                        "read_file",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!output.ContainsKey("path"))
                    {
                        output["path"] = "";
                    }
                }

                // run_shell
                if (toolName.Equals(
                        "run_shell",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (output.TryGetValue(
                            "command",
                            out object? command))
                    {
                        string cmd =
                            Convert.ToString(command) ??
                            "";

                        output["command"] =
                            CleanModelValue(cmd);
                    }
                }

                // write_file
                if (toolName.Equals(
                        "write_file",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!output.ContainsKey("path"))
                    {
                        output["path"] = "";
                    }

                    if (!output.ContainsKey("content"))
                    {
                        output["content"] = "";
                    }
                }

                // edit_file
                if (toolName.Equals(
                        "edit_file",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!output.ContainsKey("path"))
                    {
                        output["path"] = "";
                    }

                    if (!output.ContainsKey("old_text"))
                    {
                        output["old_text"] = "";
                    }

                    if (!output.ContainsKey("new_text"))
                    {
                        output["new_text"] = "";
                    }
                }

                return JsonSerializer.Serialize(output);
            }
            catch (JsonException)
            {
                // Tenta extrair JSON escondido em texto.
                int begin =
                    json.IndexOf('{');

                int finish =
                    json.LastIndexOf('}');

                if (begin >= 0 &&
                    finish > begin)
                {
                    string extracted =
                        json.Substring(
                            begin,
                            finish - begin + 1);

                    return NormalizeToolArguments(
                        toolName,
                        extracted);
                }

                return DefaultToolArguments(toolName);
            }
        }

        private static string DefaultToolArguments(
            string toolName)
        {
            if (toolName.Equals(
                    "list_dir",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "{\"path\":\".\"}";
            }

            if (toolName.Equals(
                    "read_file",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "{\"path\":\"\"}";
            }

            if (toolName.Equals(
                    "run_shell",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "{\"command\":\"\"}";
            }

            if (toolName.Equals(
                    "write_file",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "{\"path\":\"\",\"content\":\"\"}";
            }

            if (toolName.Equals(
                    "edit_file",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "{\"path\":\"\",\"old_text\":\"\",\"new_text\":\"\"}";
            }

            return "{}";
        }

        private static string CleanModelValue(
            string value)
        {
            string result =
                value.Trim();

            if (result.Length >= 2)
            {
                char first =
                    result[0];

                char last =
                    result[result.Length - 1];

                if ((first == '\'' &&
                     last == '\'') ||
                    (first == '"' &&
                     last == '"'))
                {
                    result =
                        result.Substring(
                            1,
                            result.Length - 2);
                }
            }

            return result.Trim();
        }

    }
}
