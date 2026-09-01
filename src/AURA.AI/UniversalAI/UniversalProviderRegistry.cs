using System.Text.Json;

namespace AURA.AI.UniversalAI;

/// <summary>
/// Fonte única de providers configurados pelo usuário/aplicação.
/// Não contém providers, modelos, endpoints ou chaves embutidos no código.
/// </summary>
public static class UniversalProviderRegistry
{
    private static readonly object Gate = new();
    private static IReadOnlyList<UniversalProvider>? _providers;

    public static IReadOnlyList<UniversalProvider> Providers
    {
        get { lock (Gate) return _providers ??= Load(); }
    }

    public static UniversalProvider? Find(string? idOrName)
    {
        if (string.IsNullOrWhiteSpace(idOrName)) return null;
        string wanted = idOrName.Trim();
        return Providers.FirstOrDefault(p =>
            string.Equals(p.Id, wanted, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Name, wanted, StringComparison.OrdinalIgnoreCase));
    }

    public static UniversalProvider Custom(
        string id,
        string name,
        string baseUrl,
        string? modelsUrl = null,
        UniversalApiFormat format = UniversalApiFormat.OpenAiCompatible,
        string authHeader = "Authorization",
        string authScheme = "Bearer",
        bool requiresApiKey = true)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("ID obrigatório.", nameof(id));
        if (string.IsNullOrWhiteSpace(baseUrl)) throw new ArgumentException("Base URL obrigatória.", nameof(baseUrl));
        string baseValue = baseUrl.Trim().TrimEnd('/');
        string chat = baseValue.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? baseValue : baseValue + "/chat/completions";
        string models = string.IsNullOrWhiteSpace(modelsUrl) ? baseValue + "/models" : modelsUrl.Trim();
        return new UniversalProvider(id.Trim(), string.IsNullOrWhiteSpace(name) ? id.Trim() : name.Trim(), chat, models,
            format, authHeader ?? string.Empty, authScheme ?? string.Empty, requiresApiKey);
    }

    public static void Reload()
    {
        lock (Gate) _providers = Load();
    }

    private static IReadOnlyList<UniversalProvider> Load()
    {
        string? path = LocateCatalog();
        if (path == null) return Array.Empty<UniversalProvider>();

        try
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("providers", out JsonElement providers) || providers.ValueKind != JsonValueKind.Array)
                return Array.Empty<UniversalProvider>();

            return providers.EnumerateArray().Select(ParseProvider).Where(p => p != null).Cast<UniversalProvider>().ToArray();
        }
        catch
        {
            return Array.Empty<UniversalProvider>();
        }
    }

    private static UniversalProvider? ParseProvider(JsonElement p)
    {
        string id = StringValue(p, "id");
        string name = StringValue(p, "name");
        string baseUrl = StringValue(p, "baseUrl");
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(baseUrl)) return null;

        UniversalApiFormat format = StringValue(p, "apiFormat").Trim().ToLowerInvariant() switch
        {
            "anthropicmessages" => UniversalApiFormat.AnthropicMessages,
            "gemini" => UniversalApiFormat.Gemini,
            _ => UniversalApiFormat.OpenAiCompatible
        };

        var models = new List<UniversalModel>();
        if (p.TryGetProperty("models", out JsonElement modelArray) && modelArray.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement m in modelArray.EnumerateArray())
            {
                string modelId = StringValue(m, "id");
                if (string.IsNullOrWhiteSpace(modelId)) continue;
                string label = StringValue(m, "label");
                models.Add(new UniversalModel(modelId, string.IsNullOrWhiteSpace(label) ? modelId : label, id));
            }
        }

        return new UniversalProvider(
            id.Trim(),
            string.IsNullOrWhiteSpace(name) ? id.Trim() : name.Trim(),
            NormalizeChatUrl(baseUrl),
            StringValue(p, "modelsUrl"),
            format,
            StringValue(p, "authHeaderName"),
            StringValue(p, "authScheme"),
            BoolValue(p, "needsKey", true))
        {
            KeyEnv = StringValue(p, "keyEnv"),
            KeyHint = StringValue(p, "keyHint"),
            DefaultModelId = StringValue(p, "defaultModelId"),
            KeyPrefixes = StringArray(p, "keyPrefixes")
        };
    }

    private static string? LocateCatalog()
    {
        string[] candidates =
        {
            Path.Combine(Directory.GetCurrentDirectory(), "config", "providers.json"),
            Path.Combine(AppContext.BaseDirectory, "config", "providers.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config", "providers.json")
        };
        return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }

    private static string NormalizeChatUrl(string value)
    {
        string u = value.Trim().TrimEnd('/');
        return u.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ||
               u.EndsWith("/messages", StringComparison.OrdinalIgnoreCase) ||
               u.EndsWith("/api/chat", StringComparison.OrdinalIgnoreCase) ? u : u + "/chat/completions";
    }

    private static string StringValue(JsonElement e, string name) =>
        e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? string.Empty : string.Empty;

    private static bool BoolValue(JsonElement e, string name, bool fallback) =>
        e.TryGetProperty(name, out JsonElement v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : fallback;

    private static IReadOnlyList<string> StringArray(JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out JsonElement v) || v.ValueKind != JsonValueKind.Array) return Array.Empty<string>();
        return v.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString() ?? string.Empty).Where(x => x.Length > 0).ToArray();
    }
}
