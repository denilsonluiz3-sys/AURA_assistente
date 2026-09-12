using System.Text.Json;

namespace AURA.AI.UniversalAI;

/// <summary>
/// Interpreta a resposta estruturada esperada de um modelo local.
/// O parser aceita JSON puro ou JSON dentro de um bloco markdown, mas nunca
/// trata texto livre como uma chamada de ferramenta.
/// </summary>
public static class LocalToolCallParser
{
    public static bool TryParseMany(
        string? content,
        out IReadOnlyList<AgentToolCall> calls,
        out string error)
    {
        calls = Array.Empty<AgentToolCall>();
        error = string.Empty;

        string json = ExtractJson(content);
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Resposta local vazia.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var parsed = new List<AgentToolCall>();

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                    AddCall(item, parsed);
            }
            else
            {
                if (root.TryGetProperty("tool_calls", out var toolCalls) &&
                    toolCalls.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in toolCalls.EnumerateArray())
                        AddCall(item, parsed);
                }
                else
                {
                    AddCall(root, parsed);
                }
            }

            if (parsed.Count == 0)
            {
                error = "Nenhuma chamada de ferramenta válida foi encontrada.";
                return false;
            }

            calls = parsed;
            return true;
        }
        catch (JsonException ex)
        {
            error = "JSON da chamada local inválido: " + ex.Message;
            return false;
        }
    }

    public static bool TryParse(
        string? content,
        out AgentToolCall? call,
        out string error)
    {
        call = null;
        if (!TryParseMany(content, out var calls, out error))
            return false;

        call = calls[0];
        return true;
    }

    private static void AddCall(JsonElement item, ICollection<AgentToolCall> calls)
    {
        if (item.ValueKind != JsonValueKind.Object)
            return;

        string? name = null;
        string? id = null;
        string? arguments = null;

        if (item.TryGetProperty("function", out var function) &&
            function.ValueKind == JsonValueKind.Object)
        {
            name = ReadString(function, "name");
            arguments = ReadArguments(function, "arguments");
            id = ReadString(item, "id");
        }
        else
        {
            name = ReadString(item, "name");
            id = ReadString(item, "id");
            arguments = ReadArguments(item, "arguments");

            if (string.IsNullOrWhiteSpace(name))
                name = ReadString(item, "tool");
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(arguments))
            return;

        calls.Add(new AgentToolCall
        {
            Id = string.IsNullOrWhiteSpace(id) ? "local-call-" + calls.Count : id,
            Name = name.Trim(),
            ArgumentsJson = arguments
        });
    }

    private static string? ReadArguments(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var value))
            return null;

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : value.GetRawText();
    }

    private static string? ReadString(JsonElement parent, string property) =>
        parent.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string ExtractJson(string? content)
    {
        string text = content?.Trim() ?? string.Empty;
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            int firstLineEnd = text.IndexOf('\n');
            int closing = text.LastIndexOf("```", StringComparison.Ordinal);
            if (firstLineEnd >= 0 && closing > firstLineEnd)
                text = text[(firstLineEnd + 1)..closing].Trim();
        }

        return text;
    }
}
