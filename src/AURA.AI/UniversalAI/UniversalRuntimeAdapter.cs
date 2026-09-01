namespace AURA.AI.UniversalAI;

/// <summary>Transforma exclusivamente a configuração escolhida pelo usuário em uma conexão universal.</summary>
public static class UniversalRuntimeAdapter
{
    public static UniversalConnection CreateConnection(
        string providerId,
        string apiKey,
        string model,
        string baseUrl,
        string? modelsUrl = null,
        UniversalApiFormat format = UniversalApiFormat.OpenAiCompatible,
        string authHeader = "Authorization",
        string authScheme = "Bearer",
        bool requiresApiKey = true)
    {
        if (string.IsNullOrWhiteSpace(providerId)) throw new ArgumentException("Provider obrigatório.", nameof(providerId));
        if (string.IsNullOrWhiteSpace(model)) throw new ArgumentException("Modelo obrigatório.", nameof(model));
        var provider = UniversalProviderRegistry.Custom(providerId, baseUrl, modelsUrl, format, authHeader, authScheme, requiresApiKey);
        return new UniversalConnection(provider, apiKey ?? string.Empty, model.Trim());
    }
}
