using System;
using System.IO;
using System.Linq;
using AURA.Core.Logging;

namespace AURA.AI.UniversalAI;

/// <summary>Ponto único de resolução do runtime de IA.</summary>
public static class UniversalAiRuntime
{
    public static UniversalConnection Resolve(string? providerId = null, string? apiKey = null, string? model = null, string? baseUrl = null, string? modelsUrl = null)
    {
        UniversalProvider provider = ResolveProvider(providerId, apiKey);
        string key = ResolveApiKey(provider, apiKey);

        if (provider.RequiresApiKey && string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException($"A chave do provedor '{provider.Name}' não foi configurada." +
                (string.IsNullOrWhiteSpace(provider.KeyEnv) ? " Informe a API key." : $" Defina {provider.KeyEnv}."));

        if (!string.IsNullOrWhiteSpace(baseUrl) || !string.IsNullOrWhiteSpace(modelsUrl))
        {
            provider = provider with
            {
                BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? provider.BaseUrl : NormalizeChatUrl(baseUrl),
                ModelsUrl = string.IsNullOrWhiteSpace(modelsUrl) ? provider.ModelsUrl : modelsUrl.TrimEnd('/')
            };
        }

        if (string.IsNullOrWhiteSpace(provider.BaseUrl))
            throw new InvalidOperationException($"O endpoint do provedor '{provider.Name}' não está configurado.");

        string selectedModel = !string.IsNullOrWhiteSpace(model) ? model.Trim() : provider.DefaultModelId;
        if (string.IsNullOrWhiteSpace(selectedModel))
            throw new InvalidOperationException($"Nenhum modelo foi selecionado para o provedor '{provider.Name}'.");

        return new UniversalConnection(provider, key, selectedModel);
    }

    public static OpenRouterClient CreateClient(string? providerId = null, string? apiKey = null, string? model = null,
        string? baseUrl = null, string? modelsUrl = null, int maxTokens = 1500, int timeoutSeconds = 90, ILogger? logger = null)
    {
        return UniversalAiClientFactory.Create(Resolve(providerId, apiKey, model, baseUrl, modelsUrl), maxTokens, timeoutSeconds, logger);
    }

    public static OpenRouterClient CreateClientFromEnvironment(string? model = null, ILogger? logger = null, string? providerId = null)
        => CreateClient(providerId, null, model, null, null, 1500, 120, logger);

    public static UniversalProvider ResolveProvider(string? providerId = null, string? apiKey = null)
    {
        string requested = FirstNonEmpty(providerId, Environment.GetEnvironmentVariable("AURA_PROVIDER"), Environment.GetEnvironmentVariable("AI_PROVIDER"));
        if (!string.IsNullOrWhiteSpace(requested))
        {
            UniversalProvider? found = UniversalProviderRegistry.Find(requested);
            if (found != null) return found;
            string? customBase = Environment.GetEnvironmentVariable("AURA_BASE_URL");
            if (!string.IsNullOrWhiteSpace(customBase))
                return UniversalProviderRegistry.Custom(customBase, Environment.GetEnvironmentVariable("AURA_MODELS_URL"));
            throw new InvalidOperationException($"Provedor '{requested}' não encontrado no catálogo universal.");
        }

        string candidateKey = FirstNonEmpty(apiKey, Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"), Environment.GetEnvironmentVariable("AURA_OPENROUTER_KEY"));
        if (!string.IsNullOrWhiteSpace(candidateKey))
        {
            UniversalProvider? detected = DetectByKey(candidateKey);
            if (detected != null) return detected;
            UniversalProvider? openRouter = UniversalProviderRegistry.Find("openrouter");
            if (openRouter != null) return openRouter;
        }

        return UniversalProviderRegistry.Find("ollama") ?? UniversalProviderRegistry.BuiltIns.First();
    }

    public static string ResolveApiKey(UniversalProvider provider, string? explicitKey = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitKey)) return explicitKey.Trim();
        if (!provider.RequiresApiKey) return string.Empty;

        string value = FirstNonEmpty(
            string.IsNullOrWhiteSpace(provider.KeyEnv) ? null : Environment.GetEnvironmentVariable(provider.KeyEnv),
            Environment.GetEnvironmentVariable("AURA_" + NormalizeEnvId(provider.Id) + "_API_KEY"));
        if (!string.IsNullOrWhiteSpace(value)) return value.Trim();

        if (string.Equals(provider.Id, "openrouter", StringComparison.OrdinalIgnoreCase))
            return FirstNonEmpty(Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"), Environment.GetEnvironmentVariable("AURA_OPENROUTER_KEY"), ReadLegacyOpenRouterKey());
        return string.Empty;
    }

    private static UniversalProvider? DetectByKey(string key)
        => UniversalProviderRegistry.BuiltIns.Where(p => p.RequiresApiKey)
            .Select(p => new { Provider = p, Match = p.KeyPrefixes.Any(prefix => key.Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) })
            .FirstOrDefault(x => x.Match)?.Provider;

    private static string ReadLegacyOpenRouterKey()
    {
        try
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aura", "ai_key.txt");
            return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
        }
        catch { return string.Empty; }
    }

    private static string NormalizeEnvId(string id) => new string((id ?? string.Empty).ToUpperInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

    private static string NormalizeChatUrl(string url)
    {
        string value = url.Trim().TrimEnd('/');
        if (value.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) || value.EndsWith("/messages", StringComparison.OrdinalIgnoreCase) || value.EndsWith("/api/chat", StringComparison.OrdinalIgnoreCase)) return value;
        return value + "/chat/completions";
    }

    private static string FirstNonEmpty(params string?[] values) => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;
}
