#if ANDROID
using System.Runtime.InteropServices;
using AURA.AI;
using AURA.AI.UniversalAI;

namespace AURA.Mobile.Platforms.Android.AI;

public sealed class LocalModelInferenceOptions
{
    public int ContextSize { get; init; } = 4096;
    public int MaxTokens { get; init; } = 512;
    public int Threads { get; init; } = 4;
}

/// <summary>
/// Ponte C# para a biblioteca nativa Android da AURA.
/// A biblioteca .so será adicionada somente quando o build NDK do llama.cpp
/// estiver validado; sem ela a falha é explícita e não simula modo offline.
/// </summary>
public sealed class AndroidNativeLocalModelEngine : ILocalModelEngine
{
    private const string LibraryName = "aura_llama";
    private readonly LocalModelInferenceOptions _options;

    public AndroidNativeLocalModelEngine(LocalModelInferenceOptions? options = null)
    {
        _options = options ?? new LocalModelInferenceOptions();
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

        return Task.Run(() => GenerateCore(modelPath, messages, tools, ct), ct);
    }

    private string GenerateCore(
        string modelPath,
        IReadOnlyList<AgentMessage> messages,
        IReadOnlyList<AgentToolDefinition> tools,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        string prompt = LocalChatPromptBuilder.Build(messages, tools);
        IntPtr context = IntPtr.Zero;
        IntPtr output = IntPtr.Zero;

        try
        {
            context = aura_llama_open(modelPath, _options.ContextSize, _options.Threads);
            if (context == IntPtr.Zero)
                throw new LocalModelEngineException("O modelo local não pôde ser carregado.");

            ct.ThrowIfCancellationRequested();
            output = aura_llama_generate(context, prompt, _options.MaxTokens);
            if (output == IntPtr.Zero)
                throw new LocalModelEngineException("O runtime local não retornou uma resposta.");

            return Marshal.PtrToStringUTF8(output) ?? string.Empty;
        }
        catch (DllNotFoundException ex)
        {
            throw new LocalModelEngineException(
                "O runtime nativo Android ainda não está incluído neste APK.", ex);
        }
        catch (EntryPointNotFoundException ex)
        {
            throw new LocalModelEngineException(
                "A biblioteca nativa Android não possui a ABI esperada.", ex);
        }
        finally
        {
            if (output != IntPtr.Zero)
                aura_llama_free_text(output);
            if (context != IntPtr.Zero)
                aura_llama_close(context);
        }
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
        int maxTokens);

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
