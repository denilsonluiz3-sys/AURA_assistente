#if ANDROID
using System.Runtime.InteropServices;
using AURA.AI;
using AURA.AI.UniversalAI;

namespace AURA.Mobile.Platforms.Android.AI;

public sealed class LocalModelInferenceOptions
{
    public int ContextSize { get; init; } = 4096;
    public int MaxTokens { get; init; } = 256;
    public int Threads { get; init; } = 4;
}

/// <summary>
/// Ponte C# para a biblioteca nativa Android da AURA.
/// A biblioteca .so será adicionada somente quando o build NDK do llama.cpp
/// estiver validado; sem ela a falha é explícita e não simula modo offline.
/// </summary>
public sealed class AndroidNativeLocalModelEngine : ILocalModelEngine, ILocalModelEngineStatus, IDisposable
{
    private const string LibraryName = "aura_llama";
    private readonly LocalModelInferenceOptions _options;
    private readonly object _sync = new();
    private readonly SemaphoreSlim _inferenceGate = new(1, 1);
    private IntPtr _context;
    private string? _loadedModelPath;
    private LocalModelRuntimeState _state = LocalModelRuntimeState.Unloaded;

    public LocalModelRuntimeState State { get { lock (_sync) return _state; } }
    public event Action<LocalModelRuntimeState>? StateChanged;
    public event Action<LocalModelProgress>? ProgressChanged;
    private readonly NativeProgressCallback _nativeProgressCallback;

    private void SetState(LocalModelRuntimeState state)
    {
        lock (_sync) _state = state;
        StateChanged?.Invoke(state);
    }

    public AndroidNativeLocalModelEngine(LocalModelInferenceOptions? options = null)
    {
        _options = options ?? new LocalModelInferenceOptions();
        _nativeProgressCallback = OnNativeProgress;
        if (_options.ContextSize < 512) throw new ArgumentOutOfRangeException(nameof(options));
        if (_options.MaxTokens < 1) throw new ArgumentOutOfRangeException(nameof(options));
        if (_options.Threads < 1) throw new ArgumentOutOfRangeException(nameof(options));
    }

    public Task<string> GenerateAsync(
        string modelPath,
        IReadOnlyList<AgentMessage> messages,
        IReadOnlyList<AgentToolDefinition> tools,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new ArgumentException("Caminho do modelo obrigatório.", nameof(modelPath));

        return Task.Run(async () =>
        {
            await _inferenceGate.WaitAsync(ct).ConfigureAwait(false);
            try { return GenerateCore(modelPath, messages, tools, ct); }
            finally { _inferenceGate.Release(); }
        }, ct);
    }

    public void Unload()
    {
        IntPtr context;
        lock (_sync)
        {
            context = _context;
            _context = IntPtr.Zero;
            _loadedModelPath = null;
        }
        if (context != IntPtr.Zero) aura_llama_close(context);
        SetState(LocalModelRuntimeState.Unloaded);
    }

    public void Dispose()
    {
        Unload();
        _inferenceGate.Dispose();
    }

    private string GenerateCore(
        string modelPath,
        IReadOnlyList<AgentMessage> messages,
        IReadOnlyList<AgentToolDefinition> tools,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string prompt = LocalChatPromptBuilder.Build(messages, tools);
        IntPtr output = IntPtr.Zero;

        try
        {
            IntPtr context;
            lock (_sync) context = _context;
            if (context == IntPtr.Zero || !string.Equals(_loadedModelPath, modelPath, StringComparison.Ordinal))
            {
                Unload();
                SetState(LocalModelRuntimeState.Loading);
                context = aura_llama_open(modelPath, _options.ContextSize, _options.Threads);
                if (context == IntPtr.Zero)
                    throw new LocalModelEngineException("O modelo local não pôde ser carregado.");
                lock (_sync)
                {
                    _context = context;
                    _loadedModelPath = modelPath;
                }
                SetState(LocalModelRuntimeState.Loaded);
            }

            ct.ThrowIfCancellationRequested();
            SetState(LocalModelRuntimeState.Generating);
            output = aura_llama_generate(context, prompt, _options.MaxTokens, _nativeProgressCallback);
            if (output == IntPtr.Zero)
                throw new LocalModelEngineException("O runtime local não retornou uma resposta.");

            SetState(LocalModelRuntimeState.Loaded);
            return Marshal.PtrToStringUTF8(output) ?? string.Empty;
        }
        catch (DllNotFoundException ex)
        {
            SetState(LocalModelRuntimeState.Error);
            throw new LocalModelEngineException(
                "O runtime nativo Android ainda não está incluído neste APK.", ex);
        }
        catch (EntryPointNotFoundException ex)
        {
            SetState(LocalModelRuntimeState.Error);
            throw new LocalModelEngineException(
                "A biblioteca nativa Android não possui a ABI esperada.", ex);
        }
        catch
        {
            SetState(LocalModelRuntimeState.Error);
            throw;
        }
        finally
        {
            if (output != IntPtr.Zero)
                aura_llama_free_text(output);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NativeProgressCallback(int phase, int current, int total);

    private void OnNativeProgress(int phase, int current, int total)
    {
        ProgressChanged?.Invoke(new LocalModelProgress(
            phase == 0 ? "processando prompt" : "gerando resposta", current, total));
    }

    [DllImport(LibraryName, EntryPoint = "aura_llama_open", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr aura_llama_open(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string modelPath,
        int contextSize,
        int threads);

    [DllImport(LibraryName, EntryPoint = "aura_llama_generate", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr aura_llama_generate(
        IntPtr context,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string prompt,
        int maxTokens,
        NativeProgressCallback progress);

    [DllImport(LibraryName, EntryPoint = "aura_llama_free_text", CallingConvention = CallingConvention.Cdecl)]
    private static extern void aura_llama_free_text(IntPtr output);

    [DllImport(LibraryName, EntryPoint = "aura_llama_close", CallingConvention = CallingConvention.Cdecl)]
    private static extern void aura_llama_close(IntPtr context);
}

public sealed class LocalModelEngineException : Exception
{
    public LocalModelEngineException(string message) : base(message) { }
    public LocalModelEngineException(string message, Exception inner) : base(message, inner) { }
}
#endif
