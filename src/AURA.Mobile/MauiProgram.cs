using AURA.AI;
using AURA.AI.UniversalAI;
using AURA.Agents;
using AURA.Agents.Programs;
using AURA.Abstractions;
using AURA.Abstractions.Execution;
using AURA.Abstractions.Orchestration;
using AURA.Core.Configuration;
using AURA.Core.Events;
using AURA.Core.Logging;
using AURA.Core.Launchers;
using AURA.Core.Runtime;
using AURA.Memory;
using AURA.Mobile.Diagnostics;
using AURA.Mobile.Pages;
using AURA.Modules;
using AURA.Modules.Executors;
using AURA.Network;
using AURA.SystemInfo;
using AURA.Mobile.Speech;
using AURA.Mobile.Services;
using AURA.Mobile.ViewModels;
using CommunityToolkit.Maui;

namespace AURA.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AuraLog.Info("MauiProgram.CreateMauiApp BEGIN");
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        if (OperatingSystem.IsAndroidVersionAtLeast(26)) builder.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: true);
#if ANDROID
        builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<Microsoft.Maui.Controls.WebView, AURA.Mobile.Platforms.Android.WebView.AuraWebViewHandler>());
        builder.Services.AddSingleton<IAndroidCapabilityService>(sp => new Services.AndroidCapabilityService(Android.App.Application.Context));
        builder.Services.AddSingleton<IAuraCellContextFactory, AuraCellContextFactory>();
        builder.Services.AddSingleton<CellProgramRegistry>(sp =>
        {
            var registry = new CellProgramRegistry();
            registry.Register(new DeviceDiagnosticProgram());
            registry.Register(new BrowserCellProgram());
            registry.Register(new BrowserReadCellProgram());
            registry.Register(new BrowserClickCellProgram());
            registry.Register(new BrowserTypeCellProgram());
            registry.Register(new BrowserScrollCellProgram());
            registry.Register(new BrowserBackCellProgram());
            registry.Register(new BrowserForwardCellProgram());
            registry.Register(new BrowserWaitCellProgram());
            registry.Register(new BrowserScreenshotCellProgram());
            return registry;
        });
        builder.Services.AddSingleton<ISpeechRecognitionService, AndroidSpeechRecognitionService>();
        builder.Services.AddSingleton<IEmbeddedPython, EmbeddedPythonService>();
#endif
        AuraLog.Info("MauiProgram: builder created");
        builder.Services.AddSingleton<ILogger, ConsoleLogger>();
        builder.Services.AddSingleton<EventBus>();
        builder.Services.AddSingleton<IIntentResolver, HeuristicIntentResolver>();
        builder.Services.AddSingleton<PolicyGuard>();
        builder.Services.AddSingleton<CellProgramRunner>();
        string configDir = Path.Combine(FileSystem.AppDataDirectory, "config");
        builder.Services.AddSingleton(sp => new ConfigLoader(sp.GetRequiredService<ILogger>()).LoadSettings(Path.Combine(configDir, "settings.json")));
        builder.Services.AddSingleton(sp => new ConfigLoader(sp.GetRequiredService<ILogger>()).LoadModules(Path.Combine(configDir, "modules.json")));
        builder.Services.AddSingleton(sp => new ModuleManager(sp.GetRequiredService<ILogger>(), Path.Combine(FileSystem.AppDataDirectory, "modules"), Path.Combine(configDir, "modules.json"), sp.GetRequiredService<EventBus>(), localPackageProvider: ReadEmbeddedModulePackageAsync));
        builder.Services.AddSingleton(sp => new MemoryStore(sp.GetRequiredService<ILogger>(), Path.Combine(FileSystem.AppDataDirectory, "memory.json")));
        builder.Services.AddSingleton<IUniversalAiClient>(sp =>
        {
            var client = new UniversalAiClient(new UniversalAiClientOptions());
            try { RuntimeConfig.Apply(client); }
            catch (Exception ex) { AuraLog.Exception("MauiProgram.RuntimeConfig.Apply", ex); }
            return client;
        });
        builder.Services.AddSingleton<AiDiagnosticsService>();
        builder.Services.AddSingleton<AiAssistant>();
        builder.Services.AddSingleton<ISpeechService, HybridSpeechService>();
        builder.Services.AddSingleton<VoiceAssistantService>(sp => new VoiceAssistantService(sp.GetRequiredService<ISpeechService>(), sp.GetService<ISpeechRecognitionService>(), sp.GetService<IOrchestrator>(), sp.GetService<IIntentResolver>()));
        builder.Services.AddSingleton(sp => new AgentManager(sp.GetRequiredService<ILogger>()) { Events = sp.GetRequiredService<EventBus>() });
        builder.Services.AddSingleton<SystemAnalyzer>();
        builder.Services.AddSingleton<NetworkManager>();
        builder.Services.AddSingleton<ShellExecutor>();
        builder.Services.AddSingleton<GitExecutor>();
        builder.Services.AddSingleton<PythonExecutor>(sp => { PythonExecutor.Embedded = sp.GetService<IEmbeddedPython>(); return new PythonExecutor(); });
        builder.Services.AddSingleton<NodeExecutor>();
        builder.Services.AddSingleton<IToolExecutor>(sp => sp.GetRequiredService<ShellExecutor>());
        builder.Services.AddSingleton<AURA.Core.Abstractions.IWebSearch, AURA.Core.WebSearchService>();
        builder.Services.AddSingleton<AURA.Agents.MemoryAgent>();
        builder.Services.AddSingleton<AURA.Agents.AutomationAgent>();
        builder.Services.AddSingleton<AURA.Agents.AIAgent>();
        builder.Services.AddSingleton<AURA.Core.Abstractions.IAgent>(sp => sp.GetRequiredService<AURA.Agents.MemoryAgent>());
        builder.Services.AddSingleton<AURA.Core.Abstractions.IAgent>(sp => sp.GetRequiredService<AURA.Agents.AutomationAgent>());
        builder.Services.AddSingleton<AURA.Core.Abstractions.IAgent>(sp => sp.GetRequiredService<AURA.Agents.AIAgent>());
        builder.Services.AddSingleton(sp => new AURA.Core.Knowledge.KnowledgeManager(Path.Combine(FileSystem.AppDataDirectory, "knowledge"), sp.GetRequiredService<ILogger>()));
        builder.Services.AddSingleton<AURA.Core.Abstractions.IAgent>(sp => sp.GetRequiredService<AURA.Core.Knowledge.KnowledgeManager>());
        builder.Services.AddSingleton(sp => new SimulationRuntime(sp.GetRequiredService<ILogger>(), Path.Combine(FileSystem.AppDataDirectory, "cells"), new DirectoryCellBackend()) { Events = sp.GetRequiredService<EventBus>() });
        builder.Services.AddSingleton<Runner>();
        builder.Services.AddSingleton<ProcessRegistry>();
        builder.Services.AddSingleton<AgentExecutionCoordinator>();
        builder.Services.AddSingleton(sp => new SolutionStore(sp.GetRequiredService<ILogger>(), Path.Combine(FileSystem.AppDataDirectory, "aura")));
        builder.Services.AddSingleton(sp => new LocalPlaybook(sp.GetRequiredService<SolutionStore>(), sp.GetRequiredService<MemoryStore>()));
        builder.Services.AddSingleton<FileTool>(sp => new FileTool(AgentWorkspace.ActiveRoot));
        builder.Services.AddSingleton<AuraOrchestrator>(sp => new AuraOrchestrator(sp.GetRequiredService<ILogger>(), sp.GetRequiredService<SolutionStore>(), sp.GetRequiredService<Runner>(), sp.GetRequiredService<SimulationRuntime>(), sp.GetRequiredService<IToolExecutor>(), sp.GetRequiredService<AURA.Core.Abstractions.IWebSearch>(), sp.GetRequiredService<IUniversalAiClient>(), events: sp.GetRequiredService<EventBus>(), intentResolver: sp.GetRequiredService<IIntentResolver>(), policyGuard: sp.GetRequiredService<PolicyGuard>()));
        builder.Services.AddSingleton<IOrchestrator>(sp => sp.GetRequiredService<AuraOrchestrator>());
        builder.Services.AddSingleton<AURA.Abstractions.Process.IProcessOrchestrator>(sp => new AURA.Agents.LegalProcessEngine(sp.GetRequiredService<ILogger>(), sp.GetServices<AURA.Core.Abstractions.IAgent>(), sp.GetRequiredService<IOrchestrator>(), sp.GetRequiredService<EventBus>()));
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<EcosystemPage>();
        builder.Services.AddSingleton<DiagnosticoPage>(sp => new DiagnosticoPage(sp.GetRequiredService<SystemAnalyzer>(), sp.GetRequiredService<NetworkManager>(), sp.GetRequiredService<AgentManager>(), sp.GetRequiredService<AiDiagnosticsService>(),
#if ANDROID
            sp.GetService<CellProgramRegistry>(), sp.GetService<CellProgramRunner>(), sp.GetService<IAuraCellContextFactory>(), sp.GetService<ILogger>()
#else
            null, null, null, sp.GetService<ILogger>()
#endif
        ));
        builder.Services.AddSingleton<ChatPage>();
        builder.Services.AddSingleton<AgentPage>(sp => new AgentPage(sp.GetRequiredService<IUniversalAiClient>(), sp.GetRequiredService<MemoryStore>(), sp.GetRequiredService<ISpeechService>(), sp.GetRequiredService<ShellExecutor>(), sp.GetRequiredService<ProcessRegistry>(), sp.GetRequiredService<AuraOrchestrator>(), sp.GetRequiredService<AgentExecutionCoordinator>(), sp.GetService<LocalPlaybook>(), sp.GetService<VoiceAssistantService>(), sp.GetRequiredService<SolutionStore>(), sp.GetService<GitExecutor>(), sp.GetService<PythonExecutor>(), sp.GetService<NodeExecutor>(), sp.GetService<CellProgramRegistry>(), sp.GetRequiredService<SimulationRuntime>(), sp.GetService<IAndroidCapabilityService>()));
        builder.Services.AddSingleton<MemoryPage>();
        builder.Services.AddSingleton<ExecutorsPage>();
        builder.Services.AddSingleton<SpectrumPage>(sp => new SpectrumPage(sp.GetService<IAndroidCapabilityService>()));
        builder.Services.AddSingleton<ModulesPage>();
        builder.Services.AddSingleton<LogsPage>();
        builder.Services.AddSingleton<FixesPage>();
        builder.Services.AddSingleton<TerminalPage>();
        builder.Services.AddSingleton<BrowserPage>();
        builder.Services.AddSingleton<ImageSearchPage>();
        builder.Services.AddSingleton<CellsPage>();
        builder.Services.AddSingleton<RunPage>();
        builder.Services.AddSingleton<ProgramsPage>();
        builder.Services.AddSingleton<ProgramsPageViewModel>();
        AuraLog.Info("MauiProgram: services registered");
        var app = builder.Build();
        try { var bus = app.Services.GetRequiredService<EventBus>(); var memory = app.Services.GetRequiredService<MemoryStore>(); bus.Subscribe<CellStateChangedEvent>(evt => memory.Append(MemoryEntry.CellStateChange(evt.CellId, evt.To))); } catch (Exception ex) { AuraLog.Exception("MauiProgram.MemoryEventSink", ex); }
        try { PythonExecutor.Embedded = app.Services.GetService<IEmbeddedPython>(); if (PythonExecutor.Embedded is not null) AuraLog.Info("MauiProgram: Python embutido ligado ao PythonExecutor"); } catch (Exception ex) { AuraLog.Exception("MauiProgram.EmbeddedPython", ex); }
        AuraLog.Info("MauiProgram.CreateMauiApp OK");
        return app;
    }

    private static async Task<string?> ReadEmbeddedModulePackageAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        foreach (string path in new[] { $"modulepkgs/{id}/module.json", $"modulepkgs\\{id}\\module.json" })
        {
            try { using Stream stream = await FileSystem.OpenAppPackageFileAsync(path); using var reader = new StreamReader(stream); string json = await reader.ReadToEndAsync(); if (!string.IsNullOrWhiteSpace(json)) return json; }
            catch (Exception ex) { AuraLog.Info($"Asset '{path}' indisponível ({ex.GetType().Name})."); }
        }
        AuraLog.Warning($"Nenhum pacote embarcado encontrado para o módulo '{id}'."); return null;
    }
}
