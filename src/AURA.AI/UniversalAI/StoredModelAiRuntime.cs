namespace AURA.AI.UniversalAI;

/// <summary>
/// Ponte mínima para o motor nativo do modelo. A implementação Android poderá
/// usar llama.cpp ou outro backend sem contaminar o AgentSession.
/// </summary>
public enum LocalModelRuntimeState
{
    Unloaded,
    Loading,
    Loaded,
    Generating,
    Error
}

public readonly record struct LocalModelProgress(string Phase, int Current, int Total);

public interface ILocalModelEngineStatus
{
    LocalModelRuntimeState State { get; }
    event Action<LocalModelRuntimeState>? StateChanged;
    event Action<LocalModelProgress>? ProgressChanged;
}

public interface ILocalModelEngine
{
    Task<string> GenerateAsync(
        string modelPath,
        IReadOnlyList<AgentMessage> messages,
        IReadOnlyList<AgentToolDefinition> tools,
        CancellationToken ct = default);
}

/// <summary>
/// Runtime local que só carrega modelos previamente registrados no LocalModelStore.
/// Não aceita caminho arbitrário nem baixa modelo em tempo de execução.
/// </summary>
public sealed class StoredModelAiRuntime : ILocalAiRuntime
{
    private readonly LocalModelStore _store;
    private readonly ILocalModelEngine _engine;
    private readonly string _modelId;

    public StoredModelAiRuntime(LocalModelStore store, ILocalModelEngine engine, string modelId)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Modelo local obrigatório.", nameof(modelId));
        _modelId = modelId.Trim();
    }

    public Task<string> CompleteAsync(
        IReadOnlyList<AgentMessage> messages,
        IReadOnlyList<AgentToolDefinition> tools,
        CancellationToken ct = default)
    {
        string modelPath = _store.GetModelPath(_modelId);
        return _engine.GenerateAsync(modelPath, messages, tools, ct);
    }
}
