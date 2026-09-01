namespace AURA.AI.UniversalAI;

/// <summary>Cria conexões universais somente a partir da configuração fornecida pelo usuário.</summary>
public static class UniversalRuntimeAdapter
{
    public static UniversalConnection CreateConnection(string providerId, string apiKey, string model, string? baseUrl = null, string? modelsUrl = null)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider obrigatório.", nameof(providerId));
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Modelo obrigatório.", nameof(model));

        UniversalProvider? provider = UniversalProviderRegistry.Find(providerId);
        if (provider is null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL obrigatória para provider não cadastrado.", nameof(baseUrl));

            provider = UniversalProviderRegistry.Custom(
                providerId,
                providerId,
                baseUrl,
                modelsUrl);
        }
        else if (!string.IsNullOrWhiteSpace(baseUrl) || !string.IsNullOrWhiteSpace(modelsUrl))
        {
            provider = provider with
            {
                BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? provider.BaseUrl : NormalizeChatUrl(baseUrl),
                ModelsUrl = string.IsNullOrWhiteSpace(modelsUrl) ? provider.ModelsUrl : modelsUrl.TrimEnd('/')
            };
        }

        if (provider.RequiresApiKey && string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key obrigatória para o provider selecionado.", nameof(apiKey));

        return new UniversalConnection(provider, apiKey?.Trim() ?? string.Empty, model.Trim());
    }

    private static string NormalizeChatUrl(string value)
    {
        string u = value.Trim().TrimEnd('/');
        return u.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ||
               u.EndsWith("/messages", StringComparison.OrdinalIgnoreCase) ||
               u.EndsWith("/api/chat", StringComparison.OrdinalIgnoreCase)
            ? u
            : u + "/chat/completions";
    }
}
