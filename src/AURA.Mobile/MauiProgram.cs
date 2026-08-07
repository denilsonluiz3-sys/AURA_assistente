using AURA.AI;
using AURA.Agents;
using AURA.Core.Events;
using AURA.Core.Logging;
using AURA.Core.Launchers;
using AURA.Core.Runtime;
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
        builder.Services.AddSingleton<EventBus>();

        // Memória persistente do app: pasta privada do Android (sem permissão extra).
        builder.Services.AddSingleton(sp => new MemoryStore(
            sp.GetRequiredService<ILogger>(),
            Path.Combine(FileSystem.AppDataDirectory, "memory.json")));

        // IA (OpenRouter) — mesma stack do AURA.AI usado no CLI.
        builder.Services.AddSingleton(sp => new OpenRouterClient(new OpenRouterOptions
        {
            ApiKey = Preferences.Default.Get("ai_api_key", string.Empty),
            BaseUrl = "https://openrouter.ai/api/v1/chat/completions",
            Model = "qwen/qwen-plus",
            MaxTokens = 1500
        }, sp.GetRequiredService<ILogger>()));
        builder.Services.AddSingleton<AiAssistant>();

        builder.Services.AddSingleton(sp => new AgentManager(sp.GetRequiredService<ILogger>())
        {
            Events = sp.GetRequiredService<EventBus>()
        });
        builder.Services.AddSingleton<SystemAnalyzer>();
        builder.Services.AddSingleton<NetworkManager>();

        // Executores do repo (Shell/Git/Python/Node) expostos na UI de status.
        builder.Services.AddSingleton<ShellExecutor>();
        builder.Services.AddSingleton<GitExecutor>();
        builder.Services.AddSingleton<PythonExecutor>();
        builder.Services.AddSingleton<NodeExecutor>();

        // Runtime de células + runner ("AURA decide como rodar"), mesmo core do CLI.
        // Células ficam na pasta privada do app (sem permissão extra).
        builder.Services.AddSingleton(sp => new SimulationRuntime(
            sp.GetRequiredService<ILogger>(),
            Path.Combine(FileSystem.AppDataDirectory, "cells"),
            new DirectoryCellBackend())
        {
            Events = sp.GetRequiredService<EventBus>()
        });
        builder.Services.AddSingleton<Runner>();

        // Páginas
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<ChatPage>();
        builder.Services.AddSingleton<AgentPage>();
        builder.Services.AddSingleton<MemoryPage>();
        builder.Services.AddSingleton<ExecutorsPage>();
        builder.Services.AddSingleton<ModulesPage>();
        builder.Services.AddSingleton<LogsPage>();
        builder.Services.AddSingleton<FixesPage>();
        builder.Services.AddSingleton<TerminalPage>();
        builder.Services.AddSingleton<BrowserPage>();
        builder.Services.AddSingleton<CellsPage>();
        builder.Services.AddSingleton<RunPage>();

        AuraLog.Info("MauiProgram: services registered");

        var app = builder.Build();

        // Memória registra eventos de ciclo de vida das células (reativa MemoryKind.CellEvent).
        try
        {
            var bus = app.Services.GetRequiredService<EventBus>();
            var memory = app.Services.GetRequiredService<MemoryStore>();
            bus.Subscribe<CellStateChangedEvent>(evt =>
                memory.Append(MemoryEntry.CellStateChange(evt.CellId, evt.To)));
        }
        catch (Exception ex)
        {
            AuraLog.Exception("MauiProgram.MemoryEventSink", ex);
        }

        AuraLog.Info("MauiProgram.CreateMauiApp OK");
        return app;
    }
}
