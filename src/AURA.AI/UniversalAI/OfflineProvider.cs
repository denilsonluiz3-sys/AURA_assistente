namespace AURA.AI.UniversalAI;

/// <summary>Provider local, sem endpoint de rede, chave ou chamadas HTTP.</summary>
public static class OfflineProvider
{
    public const string Id = "aura-offline";
    public const string DisplayName = "AURA Offline";
    public const string ModelId = LocalModelStore.RequiredModelId;

    public static UniversalProvider Definition { get; } = new(
        Id, DisplayName, string.Empty, string.Empty,
        UniversalApiFormat.OpenAiCompatible, string.Empty, string.Empty, false);

    public static UniversalConnection Connection() => new(Definition, string.Empty, ModelId);
}
