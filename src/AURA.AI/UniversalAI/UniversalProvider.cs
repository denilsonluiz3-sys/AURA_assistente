using System;
using System.Collections.Generic;

namespace AURA.AI.UniversalAI;

public enum UniversalApiFormat
{
    OpenAiCompatible,
    AnthropicMessages,
    Gemini
}

public sealed record UniversalProvider(
    string Id,
    string Name,
    string BaseUrl,
    string ModelsUrl,
    UniversalApiFormat Format = UniversalApiFormat.OpenAiCompatible,
    string AuthHeader = "Authorization",
    string AuthScheme = "Bearer",
    bool RequiresApiKey = true)
{
    public string KeyEnv { get; init; } = string.Empty;
    public string KeyHint { get; init; } = string.Empty;
    public string DefaultModelId { get; init; } = string.Empty;
    public IReadOnlyList<string> KeyPrefixes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<UniversalModel> Models { get; init; } = Array.Empty<UniversalModel>();
}

public sealed record UniversalModel(
    string Id,
    string DisplayName,
    string ProviderId,
    string? OwnedBy = null,
    string Category = "Modelo",
    bool IsFree = false);

public sealed record UniversalConnection(UniversalProvider Provider, string ApiKey, string Model);
