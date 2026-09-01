using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AURA.AI.UniversalAI;

/// <summary>
/// Catálogo único de providers. O arquivo config/providers.json é a fonte de
/// dados; esta classe apenas o transforma no modelo universal usado pelo runtime.
/// </summary>
public static class UniversalProviderRegistry
{
    private const string EmbeddedName = "AURA.AI.config.providers.json";
    private static readonly object Gate = new();
    private static IReadOnlyList<UniversalProvider>? _providers;

    public static IReadOnlyList<UniversalProvider> BuiltIns
    {
        get { lock (Gate) return _providers ??= Load(); }
    }

    public static UniversalProvider? Find(string? idOrName)
    {
        if (string.IsNullOrWhiteSpace(idOrName)) return null;
        string wanted = idOrName.Trim();
        return BuiltIns.FirstOrDefault(p =>
            string.Equals(p.Id, wanted, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(p.Name, wanted, StringComparison.OrdinalIgnoreCase));
    }

    public static UniversalProvider Custom(string baseUrl, string? modelsUrl = null, string name = "OpenAI-compatible (custom)")
    {
        string value = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Base URL obrigatória.", nameof(baseUrl));
        string chat = value.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ? value : value + "/chat/completions";
        string models = string.IsNullOrWhiteSpace(modelsUrl) ? value + "/models" : modelsUrl.Trim();
        return new UniversalProvider("custom", name.Trim(), chat, models)
        {
            KeyEnv = string.Empty,
            DefaultModelId = string.Empty
        };
    }

    public static void Reload()
    {
        lock (Gate) _providers = Load();
    }

    private static IReadOnlyList<UniversalProvider> Load()
    {
        string? json = ReadEmbeddedJson();
        if (string.IsNullOrWhiteSpace(json)) json = ReadCatalogFile();
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                CatalogFile? file = JsonSerializer.Deserialize<CatalogFile>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (file?.Providers is { Count: > 0 })
                    return file.Providers.Select(ToUniversal).ToArray();
            }
            catch { }
        }
        return Fallback();
    }

    private static UniversalProvider ToUniversal(CatalogProvider p)
    {
        UniversalApiFormat format = p.ApiFormat?.Trim().ToLowerInvariant() switch
        {
            "anthropicmessages" => UniversalApiFormat.AnthropicMessages,
            "gemini" or "geminigeneratecontent" => UniversalApiFormat.Gemini,
            _ => UniversalApiFormat.OpenAiCompatible
        };

        var models = (p.Models ?? new List<CatalogModel>())
            .Where(m => !string.IsNullOrWhiteSpace(m.Id))
            .Select(m => new UniversalModel(
                m.Id.Trim(),
                string.IsNullOrWhiteSpace(m.Label) ? m.Id.Trim() : m.Label.Trim(),
                p.Id ?? string.Empty,
                null,
                string.IsNullOrWhiteSpace(m.Category) ? (m.IsFree ? "Grátis" : "Modelo") : m.Category,
                m.IsFree))
            .ToArray();

        return new UniversalProvider(
            p.Id?.Trim() ?? string.Empty,
            p.Name?.Trim() ?? p.Id?.Trim() ?? string.Empty,
            p.BaseUrl?.Trim() ?? string.Empty,
            p.ModelsUrl?.Trim() ?? string.Empty,
            format,
            p.AuthHeaderName ?? string.Empty,
            p.AuthScheme ?? string.Empty,
            p.NeedsKey)
        {
            KeyEnv = p.KeyEnv?.Trim() ?? string.Empty,
            KeyHint = p.KeyHint?.Trim() ?? string.Empty,
            DefaultModelId = string.IsNullOrWhiteSpace(p.DefaultModelId)
                ? models.FirstOrDefault()?.Id ?? string.Empty
                : p.DefaultModelId.Trim(),
            KeyPrefixes = (p.KeyPrefixesList ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(),
            Models = models
        };
    }

    private static string? ReadEmbeddedJson()
    {
        try
        {
            using Stream? stream = typeof(UniversalProviderRegistry).Assembly.GetManifestResourceStream(EmbeddedName);
            if (stream == null) return null;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch { return null; }
    }

    private static string? ReadCatalogFile()
    {
        foreach (string candidate in new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "config", "providers.json"),
            Path.Combine(AppContext.BaseDirectory, "config", "providers.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config", "providers.json")
        })
        {
            try
            {
                string full = Path.GetFullPath(candidate);
                if (File.Exists(full)) return File.ReadAllText(full);
            }
            catch { }
        }
        return null;
    }

    private static IReadOnlyList<UniversalProvider> Fallback() => new[]
    {
        new UniversalProvider("ollama", "Ollama (local)",
            "http://127.0.0.1:11434/v1/chat/completions",
            "http://127.0.0.1:11434/v1/models",
            UniversalApiFormat.OpenAiCompatible, "", "", false)
        { DefaultModelId = "qwen2:0.5b" },
        new UniversalProvider("openrouter", "OpenRouter",
            "https://openrouter.ai/api/v1/chat/completions",
            "https://openrouter.ai/api/v1/models")
        {
            KeyEnv = "OPENROUTER_API_KEY",
            DefaultModelId = "deepseek/deepseek-chat-v3.1:free",
            KeyPrefixes = new[] { "sk-or-" }
        }
    };

    private sealed class CatalogFile { public List<CatalogProvider> Providers { get; set; } = new(); }

    private sealed class CatalogProvider
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? BaseUrl { get; set; }
        public string? ModelsUrl { get; set; }
        public bool NeedsKey { get; set; } = true;
        public string? KeyEnv { get; set; }
        public string? KeyHint { get; set; }
        public string? DefaultModelId { get; set; }
        public string? AuthHeaderName { get; set; }
        public string? AuthScheme { get; set; }
        public string? ApiFormat { get; set; }
        public List<string>? KeyPrefixesList { get; set; }
        public List<CatalogModel>? Models { get; set; }
    }

    private sealed class CatalogModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string? Category { get; set; }
        public bool IsFree { get; set; }
    }
}
