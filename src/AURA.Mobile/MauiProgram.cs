using AURA.AI;
using AURA.Agents;
using AURA.Core.Logging;
using AURA.Memory;
using AURA.Mobile.Pages;
using AURA.Modules.Executors;
using AURA.Network;
using AURA.SystemInfo;

namespace AURA.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AuraLog.Info("MauiProgram.CreateMauiApp BEGIN");
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

        AuraLog.Info("MauiProgram: builder created");

        // --- Infraestrutura AURA (mesmo Core/Abstractions usados no CLI/Termux) ---
        builder.Services.AddSingleton<ILogger, ConsoleLogger>();

        // Memória persistente do app: pasta privada do Android (sem permissão extra).
        builder.Services.AddSingleton(sp => new MemoryStore(
            sp.GetRequiredService<ILogger>(),
            Path.Combine(FileSystem.AppDataDirectory, "memory.json")));

        // IA (OpenRouter) — mesma stack do AURA.AI usado no CLI.
        builder.Services.AddSingleton(sp => new OpenRouterClient(new OpenRouterOptions
        {
            ApiKey = Preferences.Default.Get("openrouter_key", string.Empty),
            BaseUrl = "https://openrouter.ai/api/v1/chat/completions",
            Model = "qwen/qwen-plus",
            MaxTokens = 1500
        }, sp.GetRequiredService<ILogger>()));
        builder.Services.AddSingleton<AiAssistant>();

        builder.Services.AddSingleton(sp => new AgentManager(sp.GetRequiredService<ILogger>()));
        builder.Services.AddSingleton<SystemAnalyzer>();
        builder.Services.AddSingleton<NetworkManager>();

        // Executores do repo (Shell/Git/Python/Node) expostos na UI de status.
        builder.Services.AddSingleton<ShellExecutor>();
        builder.Services.AddSingleton<GitExecutor>();
        builder.Services.AddSingleton<PythonExecutor>();
        builder.Services.AddSingleton<NodeExecutor>();

        // Páginas
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<ChatPage>();
        builder.Services.AddSingleton<MemoryPage>();
        builder.Services.AddSingleton<ExecutorsPage>();
        builder.Services.AddSingleton<ModulesPage>();
        builder.Services.AddSingleton<LogsPage>();

        AuraLog.Info("MauiProgram: services registered");

        var app = builder.Build();
        AuraLog.Info("MauiProgram.CreateMauiApp OK");
        return app;
    }
}
