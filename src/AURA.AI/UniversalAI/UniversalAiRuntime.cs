using AURA.Core.Logging;

namespace AURA.AI.UniversalAI;

/// <summary>Ponto único de resolução da configuração de IA.</summary>
public static class UniversalAiRuntime
{
    public static UniversalConnection Resolve(
        string? providerId = null,
        string? apiKey = null,
        string? model = null,
        string? baseUrl = null,
        string? modelsUrl = null)
    {
        UniversalProvider provider = ResolveProvider(providerId, baseUrl);

        if (!string.IsNullOrWhiteSpace(baseUrl) || !string.IsNullOrWhiteSpace(modelsUrl))
        {
            provider = provider with
            {
                BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? provider.BaseUrl : NormalizeChatUrl(baseUrl),
                ModelsUrl = string.IsNullOrWhiteSpace(modelsUrl) ? provider.ModelsUrl : modelsUrl.TrimEnd('/')
            };
        }

        string key = apiKey?.Trim() ?? string.Empty;
        if (provider.RequiresApiKey && key.Length == 0 && !string.IsNullOrWhiteSpace(provider.KeyEnv))
            key = Environment.GetEnvironmentVariable(provider.KeyEnv)?.Trim() ?? string.Empty;

        if (provider.RequiresApiKey && key.Length == 0)
            throw new InvalidOperationException($"A API key do provider '{provider.Name}' não foi configurada.");
        if (string.IsNullOrWhiteSpace(provider.BaseUrl))
            throw new InvalidOperationException($"O endpoint do provider '{provider.Name}' não está configurado.");

        string selectedModel = !string.IsNullOrWhiteSpace(model) ? model.Trim() : provider.DefaultModelId;
        if (string.IsNullOrWhiteSpace(selectedModel))
            throw new InvalidOperationException($"Nenhum modelo foi selecionado para o provider '{provider.Name}'.");

        return new UniversalConnection(provider, key, selectedModel);
    }

    public static OpenRouterClient CreateClient(
        string? providerId = null,
        string? apiKey = null,
        string? model = null,
        string? baseUrl = null,
        string? modelsUrl = null,
        int maxTokens = 1500,
        int timeoutSeconds = 90,
        ILogger? logger = null)
        => UniversalAiClientFactory.Create(Resolve(providerId, apiKey, model, baseUrl, modelsUrl), maxTokens, timeoutSeconds);

    public static UniversalProvider ResolveProvider(string? providerId, string? baseUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(providerId))
        {
            UniversalProvider? configured = UniversalProviderRegistry.Find(providerId);
            if (configured != null) return configured;
            if (!string.IsNullOrWhiteSpace(baseUrl))
                return UniversalProviderRegistry.Custom(providerId.Trim(), providerId.Trim(), baseUrl);
            throw new InvalidOperationException($"Provider '{providerId}' não está configurado.");
        }

        string configuredId = Environment.GetEnvironmentVariable("AURA_PROVIDER")?.Trim() ?? string.Empty;
        if (configuredId.Length > 0)
            return ResolveProvider(configuredId, baseUrl);

        throw new InvalidOperationException("Nenhum provider foi selecionado. Configure o provider, endpoint e modelo na AURA.");
    }

    private static string NormalizeChatUrl(string value)
    {
        string u = value.Trim().TrimEnd('/');
        return u.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ||
               u.EndsWith("/messages", StringComparison.OrdinalIgnoreCase) ||
               u.EndsWith("/api/chat", StringComparison.OrdinalIgnoreCase) ? u : u + "/chat/completions";
    }
}
