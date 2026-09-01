namespace AURA.AI.UniversalAI;

/// <summary>
/// Registro deliberadamente vazio: a AURA não impõe providers, endpoints ou modelos.
/// A configuração é fornecida pelo usuário em tempo de execução.
/// </summary>
public static class UniversalProviderRegistry
{
    public static IReadOnlyList<UniversalProvider> BuiltIns { get; } = Array.Empty<UniversalProvider>();

    public static UniversalProvider Custom(
        string providerId,
        string baseUrl,
        string? modelsUrl = null,
        UniversalApiFormat format = UniversalApiFormat.OpenAiCompatible,
        string authHeader = "Authorization",
        string authScheme = "Bearer",
        bool requiresApiKey = true,
        string? name = null)
    {
        if (string.IsNullOrWhiteSpace(providerId)) throw new ArgumentException("Identificador do provider obrigatório.", nameof(providerId));
        var baseValue = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseValue)) throw new ArgumentException("Endpoint obrigatório.", nameof(baseUrl));
        var models = string.IsNullOrWhiteSpace(modelsUrl) ? string.Empty : modelsUrl.Trim();
        return new UniversalProvider(providerId.Trim(), string.IsNullOrWhiteSpace(name) ? providerId.Trim() : name.Trim(), baseValue, models, format, authHeader ?? string.Empty, authScheme ?? string.Empty, requiresApiKey);
    }
}
