using System.Text;
using System.Text.Json;

namespace AURA.AI.UniversalAI;

/// <summary>
/// Constrói um prompt estável para runtimes locais que recebem texto bruto.
/// O motor nativo continua responsável pelo template específico do modelo.
/// </summary>
public static class LocalChatPromptBuilder
{
    public static string Build(
        IReadOnlyList<AgentMessage> messages,
        IReadOnlyList<AgentToolDefinition> tools)
    {
        var builder = new StringBuilder();
        builder.AppendLine("AURA LOCAL CHAT");
        builder.AppendLine("Responda em português quando possível.");

        if (tools.Count > 0)
        {
            builder.AppendLine("FERRAMENTAS DISPONÍVEIS:");
            foreach (var tool in tools)
            {
                builder.Append("- ").Append(tool.Name).Append(": ").AppendLine(tool.Description);
                builder.Append("  schema: ").AppendLine(JsonSerializer.Serialize(new
                {
                    type = "object",
                    properties = tool.Parameters.ToDictionary(
                        x => x.Key,
                        x => new { type = x.Value.Type, description = x.Value.Description }),
                    required = tool.Required
                }));
            }

            builder.AppendLine("Para executar uma ferramenta, responda somente com JSON no formato:");
            builder.AppendLine("{\"type\":\"tool_call\",\"name\":\"nome\",\"arguments\":{}} ");
            builder.AppendLine("Não invente ferramentas e não execute ações fora das ferramentas disponíveis.");
        }

        foreach (var message in messages)
        {
            string role = string.IsNullOrWhiteSpace(message.Role) ? "user" : message.Role.Trim().ToUpperInvariant();
            builder.Append('[').Append(role).AppendLine("]");
            if (!string.IsNullOrWhiteSpace(message.Content))
                builder.AppendLine(message.Content.Trim());

            if (message.ToolCalls is { Count: > 0 })
            {
                foreach (var call in message.ToolCalls)
                    builder.Append("tool_call ").Append(call.Name).Append(": ").AppendLine(call.ArgumentsJson);
            }
        }

        builder.AppendLine("[ASSISTANT]");
        return builder.ToString();
    }
}
