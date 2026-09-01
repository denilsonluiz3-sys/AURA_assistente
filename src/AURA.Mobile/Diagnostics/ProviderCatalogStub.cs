namespace AURA.Mobile.Diagnostics;

/// <summary>
/// Stub mínimo para código legado do AgentPage que ainda consulta ProviderCatalog.
/// A fonte de verdade da config é RuntimeConfig + IUniversalAiClient.
/// </summary>
public sealed class ProviderInfo
{
    public bool NeedsKey { get; init; } = true;
}

public static class ProviderCatalog
{
    public static ProviderInfo? Find(string? providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return null;

        if (string.Equals(providerId, "ollama", StringComparison.OrdinalIgnoreCase))
            return new ProviderInfo { NeedsKey = false };

        return new ProviderInfo { NeedsKey = RuntimeConfig.RequiresApiKey };
    }
}
