using System.Text;
using System.Text.Json;

namespace AURA.AI.UniversalAI;

/// <summary>
/// Constrói o prompt ChatML do modelo Qwen Instruct local. O runtime nativo
/// recebe texto já formatado e preserva os marcadores de fim de turno.
/// </summary>
public static class LocalChatPromptBuilder
{
    public static string Build(
        IReadOnlyList<AgentMessage> messages,
        IReadOnlyList<AgentToolDefinition> tools)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<|im_start|>system");
        builder.AppendLine("AURA LOCAL CHAT");
        builder.AppendLine("Você é o agente operacional local da AURA, não um chatbot genérico.");
        builder.AppendLine("Você pode usar as ferramentas registradas nesta sessão para ler e alterar o workspace e executar capacidades autorizadas.");
        builder.AppendLine("Nunca diga que é apenas um assistente de chat, que não pode acessar aplicativos ou que o usuário deve procurar suporte.");
        builder.AppendLine("Quando a solicitação exigir uma ferramenta disponível, faça a chamada JSON da ferramenta; só responda em texto depois do resultado.");
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

        builder.AppendLine("<|im_end|>");

        // Qwen Instruct GGUF expects its ChatML turn markers. Keeping the
        // model template here avoids an open-ended completion caused by the
        // generic [ROLE] prompt and makes the end-of-turn token observable.
        foreach (var message in messages)
        {
            string role = string.IsNullOrWhiteSpace(message.Role) ? "user" : message.Role.Trim().ToLowerInvariant();
            builder.Append("<|im_start|>").Append(role).Append('\n');
            if (!string.IsNullOrWhiteSpace(message.Content))
                builder.AppendLine(message.Content.Trim());

            if (message.ToolCalls is { Count: > 0 })
            {
                foreach (var call in message.ToolCalls)
                    builder.Append("tool_call ").Append(call.Name).Append(": ").AppendLine(call.ArgumentsJson);
            }

            builder.AppendLine("<|im_end|>");
        }

        builder.AppendLine("<|im_start|>assistant");
        return builder.ToString();
    }
}
