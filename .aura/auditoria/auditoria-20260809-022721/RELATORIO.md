# AUDITORIA COMPLETA AURA

Data: Sun Aug  9 02:27:21 -03 2026
Branch: feat/project-access
Commit: 2969a70

## 1. Estado Git
```
?? .aura/auditoria/
?? .aura/backup-agent-solution-20260808-162923/
?? .aura/backup-agent-solution-v2-20260808-163705/
?? .aura/backup-solutions-20260808-161907/
?? scripts/auditoria-completa.sh
?? scripts/instalar-memoria-solucoes.sh
?? scripts/integrar-solutionstore-agent-v2.sh
?? scripts/integrar-solutionstore-agent.sh
```

## 2. Projetos
```
src/AURA.AI/AURA.AI.csproj
src/AURA.Abstractions/AURA.Abstractions.csproj
src/AURA.Agents/AURA.Agents.csproj
src/AURA.CLI/AURA.CLI.csproj
src/AURA.Core/AURA.Core.csproj
src/AURA.Installer/AURA.Installer.csproj
src/AURA.Memory/AURA.Memory.csproj
src/AURA.Mobile/AURA.Mobile.csproj
src/AURA.Modules/AURA.Modules.csproj
src/AURA.Network/AURA.Network.csproj
src/AURA.SystemInfo/AURA.SystemInfo.csproj
src/AURA.Windows/AURA.Windows.csproj
```

## 3. Arquivos C#
```
src/AURA.AI/AgentChat.cs
src/AURA.AI/AgentSession.cs
src/AURA.AI/AgentTool.cs
src/AURA.AI/AgentTools/FileTools.cs
src/AURA.AI/AgentTools/ShellAgentTool.cs
src/AURA.AI/AgentTools/WorkspaceAgentTool.cs
src/AURA.AI/AiAssistant.cs
src/AURA.AI/AiAssistantService.cs
src/AURA.AI/OpenRouterClient.cs
src/AURA.AI/ProviderCatalog.cs
src/AURA.Abstractions/Execution/ExecutionRequest.cs
src/AURA.Abstractions/Execution/ExecutionResult.cs
src/AURA.Abstractions/Execution/IToolExecutor.cs
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs
src/AURA.Abstractions/Runtime/RuntimeModels.cs
src/AURA.Agents/AgentManager.cs
src/AURA.CLI/Program.cs
src/AURA.Core/Abstractions/IAgent.cs
src/AURA.Core/Abstractions/ICommand.cs
src/AURA.Core/Abstractions/IModule.cs
src/AURA.Core/Abstractions/IPlugin.cs
src/AURA.Core/Abstractions/IService.cs
src/AURA.Core/Bootstrap/AuraBootstrap.cs
src/AURA.Core/Configuration/AuraConfiguration.cs
src/AURA.Core/Configuration/ConfigLoader.cs
src/AURA.Core/Configuration/ModulesConfiguration.cs
src/AURA.Core/DependencyInjection/ServiceContainer.cs
src/AURA.Core/Events/AuraEvents.cs
src/AURA.Core/Events/EventBus.cs
src/AURA.Core/Events/IEvent.cs
src/AURA.Core/Launchers/CellCommand.cs
src/AURA.Core/Launchers/DllLauncher.cs
src/AURA.Core/Launchers/GoLauncher.cs
src/AURA.Core/Launchers/ILauncher.cs
src/AURA.Core/Launchers/JarLauncher.cs
src/AURA.Core/Launchers/NodeLauncher.cs
src/AURA.Core/Launchers/PythonLauncher.cs
src/AURA.Core/Launchers/Runner.cs
src/AURA.Core/Logging/ConsoleLogger.cs
src/AURA.Core/Logging/FileLogger.cs
src/AURA.Core/Logging/ILogger.cs
src/AURA.Core/Runtime/Cell.cs
src/AURA.Core/Runtime/CellState.cs
src/AURA.Core/Runtime/CellStore.cs
src/AURA.Core/Runtime/DirectoryCellBackend.cs
src/AURA.Core/Runtime/ICellBackend.cs
src/AURA.Core/Runtime/PluginWatcher.cs
src/AURA.Core/Runtime/ResourceLimits.cs
src/AURA.Core/Runtime/SimulationRuntime.cs
src/AURA.Core/VersionInfo.cs
src/AURA.Installer/ArtifactAnalysisService.cs
src/AURA.Installer/ArtifactIdentification.cs
src/AURA.Installer/ArtifactType.cs
src/AURA.Installer/DependencyReport.cs
src/AURA.Installer/EnvironmentSelectionResult.cs
src/AURA.Installer/EnvironmentSelectionService.cs
src/AURA.Installer/FileIdentifier.cs
src/AURA.Installer/IDependencyAnalyzer.cs
src/AURA.Installer/IEnvironmentSelector.cs
src/AURA.Installer/IFileIdentifier.cs
src/AURA.Installer/IInstaller.cs
src/AURA.Installer/InstallationResult.cs
src/AURA.Installer/InstallationService.cs
src/AURA.Installer/PythonDependencyAnalyzer.cs
src/AURA.Installer/PythonEnvironmentSelector.cs
src/AURA.Installer/PythonInstaller.cs
src/AURA.Installer/PythonStdlibModules.cs
src/AURA.Memory/MemoryEntry.cs
src/AURA.Memory/MemoryStore.cs
src/AURA.Memory/RequestContext.cs
src/AURA.Memory/SolutionRule.cs
src/AURA.Memory/SolutionStore.cs
src/AURA.Mobile/App.xaml.cs
src/AURA.Mobile/Diagnostics/AgentWorkspace.cs
src/AURA.Mobile/Diagnostics/FixProposal.cs
src/AURA.Mobile/Diagnostics/ProjectAccessService.cs
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs
src/AURA.Mobile/Diagnostics/SearchCatalog.cs
src/AURA.Mobile/MainPage.cs
src/AURA.Mobile/MauiProgram.cs
src/AURA.Mobile/Pages/AgentPage.xaml.cs
src/AURA.Mobile/Pages/BrowserPage.xaml.cs
src/AURA.Mobile/Pages/BrowserSettingsPage.cs
src/AURA.Mobile/Pages/CellsPage.xaml.cs
src/AURA.Mobile/Pages/ChatPage.xaml.cs
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs
src/AURA.Mobile/Pages/FixesPage.xaml.cs
src/AURA.Mobile/Pages/HomePage.xaml.cs
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs
src/AURA.Mobile/Pages/LogsPage.xaml.cs
src/AURA.Mobile/Pages/MemoryPage.xaml.cs
src/AURA.Mobile/Pages/ModulesPage.xaml.cs
src/AURA.Mobile/Pages/RunPage.xaml.cs
src/AURA.Mobile/Pages/SectionPage.cs
src/AURA.Mobile/Pages/TerminalPage.xaml.cs
src/AURA.Mobile/Platforms/Android/AuraLog.cs
src/AURA.Mobile/Platforms/Android/MainActivity.cs
src/AURA.Mobile/Platforms/Android/MainApplication.cs
src/AURA.Mobile/Platforms/Android/StoragePermissionHelper.cs
src/AURA.Mobile/Platforms/Android/VpnHelper.cs
src/AURA.Mobile/Platforms/Android/WebView/AuraDownloadListener.cs
src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs
src/AURA.Mobile/Platforms/Android/WebView/AuraTouchListener.cs
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs
src/AURA.Mobile/ViewModels/ModuleRow.cs
src/AURA.Modules/Executors/GitExecutor.cs
src/AURA.Modules/Executors/NodeExecutor.cs
src/AURA.Modules/Executors/ProcessExecutorBase.cs
src/AURA.Modules/Executors/PythonExecutor.cs
src/AURA.Modules/Executors/ShellExecutor.cs
src/AURA.Modules/ModuleCatalog.cs
src/AURA.Modules/ModuleDifficulty.cs
src/AURA.Modules/ModuleInfo.cs
src/AURA.Modules/ModuleManager.cs
src/AURA.Modules/ModuleStatus.cs
src/AURA.Modules/Runtime/BinaryPath.cs
src/AURA.Modules/Runtime/CompatibilityChecker.cs
src/AURA.Modules/Runtime/DependencyAnalyzer.cs
src/AURA.Modules/Runtime/Installer.cs
src/AURA.Modules/Runtime/LanguageDetector.cs
src/AURA.Modules/Runtime/RuntimeCatalog.cs
src/AURA.Modules/Runtime/RuntimeManager.cs
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs
src/AURA.Modules/Runtime/RuntimeResolver.cs
src/AURA.Modules/Runtime/SyntaxValidator.cs
src/AURA.Network/NetworkManager.cs
src/AURA.Network/NetworkStatus.cs
src/AURA.SystemInfo/SystemAnalyzer.cs
src/AURA.SystemInfo/SystemDiagnosticsResult.cs
```

## 4. Estatísticas
```
Projetos: 12
Arquivos C#: 129
Classes: 148
Interfaces: 23
Métodos aproximados: 0
```

## 5. Referências entre projetos
```
src/AURA.AI/AURA.AI.csproj:10:    <ProjectReference Include="..\AURA.Core\AURA.Core.csproj" />
src/AURA.AI/AURA.AI.csproj:11:    <ProjectReference Include="..\AURA.Memory\AURA.Memory.csproj" />
src/AURA.Agents/AURA.Agents.csproj:10:    <ProjectReference Include="..\AURA.Core\AURA.Core.csproj" />
src/AURA.Agents/AURA.Agents.csproj:11:    <ProjectReference Include="..\AURA.Memory\AURA.Memory.csproj" />
src/AURA.CLI/AURA.CLI.csproj:11:    <ProjectReference Include="..\AURA.Core\AURA.Core.csproj" />
src/AURA.CLI/AURA.CLI.csproj:12:    <ProjectReference Include="..\AURA.SystemInfo\AURA.SystemInfo.csproj" />
src/AURA.CLI/AURA.CLI.csproj:13:    <ProjectReference Include="..\AURA.Network\AURA.Network.csproj" />
src/AURA.CLI/AURA.CLI.csproj:14:    <ProjectReference Include="..\AURA.Modules\AURA.Modules.csproj" />
src/AURA.CLI/AURA.CLI.csproj:15:    <ProjectReference Include="..\AURA.Agents\AURA.Agents.csproj" />
src/AURA.CLI/AURA.CLI.csproj:16:    <ProjectReference Include="..\AURA.AI\AURA.AI.csproj" />
src/AURA.CLI/AURA.CLI.csproj:17:    <ProjectReference Include="..\AURA.Windows\AURA.Windows.csproj" />
src/AURA.Installer/AURA.Installer.csproj:10:    <ProjectReference Include="..\AURA.Modules\AURA.Modules.csproj" />
src/AURA.Installer/AURA.Installer.csproj:11:    <ProjectReference Include="..\AURA.SystemInfo\AURA.SystemInfo.csproj" />
src/AURA.Memory/AURA.Memory.csproj:10:    <ProjectReference Include="..\AURA.Core\AURA.Core.csproj" />
src/AURA.Mobile/AURA.Mobile.csproj:36:    <ProjectReference Include="..\AURA.Abstractions\AURA.Abstractions.csproj" />
src/AURA.Mobile/AURA.Mobile.csproj:37:    <ProjectReference Include="..\AURA.Core\AURA.Core.csproj" />
src/AURA.Mobile/AURA.Mobile.csproj:38:    <ProjectReference Include="..\AURA.Modules\AURA.Modules.csproj" />
src/AURA.Mobile/AURA.Mobile.csproj:39:    <ProjectReference Include="..\AURA.Agents\AURA.Agents.csproj" />
src/AURA.Mobile/AURA.Mobile.csproj:40:    <ProjectReference Include="..\AURA.Memory\AURA.Memory.csproj" />
src/AURA.Mobile/AURA.Mobile.csproj:41:    <ProjectReference Include="..\AURA.AI\AURA.AI.csproj" />
src/AURA.Mobile/AURA.Mobile.csproj:42:    <ProjectReference Include="..\AURA.Network\AURA.Network.csproj" />
src/AURA.Mobile/AURA.Mobile.csproj:43:    <ProjectReference Include="..\AURA.SystemInfo\AURA.SystemInfo.csproj" />
src/AURA.Modules/AURA.Modules.csproj:10:    <ProjectReference Include="..\AURA.Abstractions\AURA.Abstractions.csproj" />
src/AURA.Modules/AURA.Modules.csproj:11:    <ProjectReference Include="..\AURA.Core\AURA.Core.csproj" />
src/AURA.Windows/AURA.Windows.csproj:10:    <ProjectReference Include="..\AURA.Core\AURA.Core.csproj" />
```

## 6. AgentSession
```
src/AURA.AI/AgentChat.cs:44:    /// <summary>Evento emitido pelo AgentSession a cada ferramenta executada (para a UI).</summary>
src/AURA.AI/AgentSession.cs:102:                "O agente atingiu o limite de " + MaxRounds + " passos de ferramentas.");
src/AURA.AI/AgentSession.cs:109:        private SolutionRule? TryGetKnownSolution(
src/AURA.AI/AgentSession.cs:123:        private async Task<string> ExecuteToolAsync(
src/AURA.AI/AgentSession.cs:19:    public sealed class AgentSession
src/AURA.AI/AgentSession.cs:21:        private const int MaxRounds = 20;
src/AURA.AI/AgentSession.cs:26:        private readonly List<AgentMessage> _messages = new();
src/AURA.AI/AgentSession.cs:30:        public AgentSession(OpenRouterClient client, IEnumerable<AgentTool> tools,
src/AURA.AI/AgentSession.cs:43:        public IReadOnlyList<AgentMessage> Messages => _messages;
src/AURA.AI/AgentSession.cs:45:        public async Task<string> RunAsync(string userText,
src/AURA.AI/AgentSession.cs:53:            _messages.Add(new AgentMessage { Role = "user", Content = userText });
src/AURA.AI/AgentSession.cs:56:            while (round++ < MaxRounds)
src/AURA.AI/AgentSession.cs:58:                AgentChatResponse response = await _client.ChatToolsAsync(
src/AURA.AI/AgentSession.cs:59:                    _messages,
src/AURA.AI/AgentSession.cs:72:                    _messages.Add(new AgentMessage
src/AURA.AI/AgentSession.cs:82:                        string result = await ExecuteToolAsync(call, ct).ConfigureAwait(false);
src/AURA.AI/AgentSession.cs:83:                        _messages.Add(new AgentMessage
src/AURA.AI/AgentSession.cs:97:                _messages.Add(new AgentMessage { Role = "assistant", Content = final });
src/AURA.AI/OpenRouterClient.cs:133:        /// modelo; o AgentSession executa as chamadas e faz o loop.
src/AURA.AI/OpenRouterClient.cs:135:        public async Task<AgentChatResponse> ChatToolsAsync(
```

## 7. IA / contexto / tokens
```
src/AURA.AI/AgentChat.cs:7:    /// (roles: system | user | assistant | tool). Em tool_calls o conteúdo é
src/AURA.AI/AgentSession.cs:110:            RequestContext request)
src/AURA.AI/AgentSession.cs:26:        private readonly List<AgentMessage> _messages = new();
src/AURA.AI/AgentSession.cs:43:        public IReadOnlyList<AgentMessage> Messages => _messages;
src/AURA.AI/AgentSession.cs:53:            _messages.Add(new AgentMessage { Role = "user", Content = userText });
src/AURA.AI/AgentSession.cs:58:                AgentChatResponse response = await _client.ChatToolsAsync(
src/AURA.AI/AgentSession.cs:59:                    _messages,
src/AURA.AI/AgentSession.cs:72:                    _messages.Add(new AgentMessage
src/AURA.AI/AgentSession.cs:83:                        _messages.Add(new AgentMessage
src/AURA.AI/AgentSession.cs:97:                _messages.Add(new AgentMessage { Role = "assistant", Content = final });
src/AURA.AI/AiAssistant.cs:12:    /// persists the conversation turn in MemoryStore so context survives across
src/AURA.AI/AiAssistantService.cs:47:                messages = new[] { new { role = "user", content = question } },
src/AURA.AI/OpenRouterClient.cs:135:        public async Task<AgentChatResponse> ChatToolsAsync(
src/AURA.AI/OpenRouterClient.cs:136:            List<AgentMessage> messages,
src/AURA.AI/OpenRouterClient.cs:147:                ["max_tokens"] = Options.MaxTokens
src/AURA.AI/OpenRouterClient.cs:156:            if (messages != null)
src/AURA.AI/OpenRouterClient.cs:158:                foreach (AgentMessage m in messages)
src/AURA.AI/OpenRouterClient.cs:188:                        mo["tool_calls"] = calls;
src/AURA.AI/OpenRouterClient.cs:195:            payload["messages"] = arr;
src/AURA.AI/OpenRouterClient.cs:26:        public int MaxTokens { get; set; } = 1500;
src/AURA.AI/OpenRouterClient.cs:282:                        if (msg.TryGetProperty("tool_calls", out JsonElement toolCalls))
src/AURA.AI/OpenRouterClient.cs:305:                        // como JSON no campo content, em vez de usar tool_calls.
src/AURA.AI/OpenRouterClient.cs:54:            var messages = new List<object>();
src/AURA.AI/OpenRouterClient.cs:57:                messages.Add(new { role = "system", content = systemPrompt });
src/AURA.AI/OpenRouterClient.cs:60:            messages.Add(new { role = "user", content = question });
src/AURA.AI/OpenRouterClient.cs:65:                max_tokens = Options.MaxTokens,
src/AURA.AI/OpenRouterClient.cs:66:                messages
src/AURA.Abstractions/Runtime/RuntimeModels.cs:148:    public List<string> Messages { get; set; } = new();
src/AURA.Abstractions/Runtime/RuntimeModels.cs:150:    public void Log(string message) => Messages.Add(message);
src/AURA.Abstractions/Runtime/RuntimeModels.cs:83:    public List<string> Messages { get; set; } = new();
src/AURA.CLI/Program.cs:516:                        MaxTokens = 1500,
src/AURA.Core/Logging/ConsoleLogger.cs:6:    /// Writes log messages to the console with color coding by severity.
src/AURA.Core/Logging/FileLogger.cs:7:    /// Appends log messages to a rolling text file, used mainly by the GUI
src/AURA.Core/Runtime/PluginWatcher.cs:111:                UnloadContext();
src/AURA.Core/Runtime/PluginWatcher.cs:119:                Assembly assembly = _context.LoadFromAssemblyPath(dllPath);
src/AURA.Core/Runtime/PluginWatcher.cs:158:        private void UnloadContext()
src/AURA.Core/Runtime/PluginWatcher.cs:16:    /// collectible AssemblyLoadContext and watches that directory for changes.
src/AURA.Core/Runtime/PluginWatcher.cs:175:            if (_context == null)
src/AURA.Core/Runtime/PluginWatcher.cs:17:    /// When a .dll is added or replaced the affected load context is unloaded
src/AURA.Core/Runtime/PluginWatcher.cs:182:                _context.Unload();
src/AURA.Core/Runtime/PluginWatcher.cs:186:                _logger.Warning("Falha ao descarregar contexto de plugins: " + ex.Message);
src/AURA.Core/Runtime/PluginWatcher.cs:190:                _context = null;
src/AURA.Core/Runtime/PluginWatcher.cs:220:        /// Collectible load context so plugin assemblies can be released on
src/AURA.Core/Runtime/PluginWatcher.cs:222:        /// first, then from the default context (framework + AURA.Core).
src/AURA.Core/Runtime/PluginWatcher.cs:224:        private sealed class PluginLoadContext : AssemblyLoadContext
src/AURA.Core/Runtime/PluginWatcher.cs:228:            public PluginLoadContext(string pluginsRoot)
src/AURA.Core/Runtime/PluginWatcher.cs:29:        private PluginLoadContext _context;
src/AURA.Core/Runtime/PluginWatcher.cs:79:                    UnloadContext();
src/AURA.Core/Runtime/PluginWatcher.cs:90:                    _context = new PluginLoadContext(_pluginsRoot);
src/AURA.Installer/PythonStdlibModules.cs:18:        "http", "urllib", "copy", "enum", "dataclasses", "abc", "contextlib",
src/AURA.Memory/MemoryStore.cs:14:    /// ~/AURA/memory.json so the assistant keeps context across restarts.
src/AURA.Memory/RequestContext.cs:11:    public sealed class RequestContext
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:12:        public static int MaxTokens
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:14:            get => Preferences.Default.Get("ai_max_tokens", 1500);
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:15:            set => Preferences.Default.Set("ai_max_tokens", value);
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:75:            client.Options.MaxTokens = MaxTokens;
src/AURA.Mobile/MauiProgram.cs:65:            MaxTokens = 1500
src/AURA.Mobile/Pages/ChatPage.xaml.cs:99:        _client.Options.MaxTokens = RuntimeConfig.MaxTokens;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:112:            $"max_tokens: {_client.Options.MaxTokens}\n" +
src/AURA.Mobile/Pages/FixesPage.xaml.cs:142:                    case "max_tokens":
src/AURA.Mobile/Pages/FixesPage.xaml.cs:145:                            RuntimeConfig.MaxTokens = tokens;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:146:                            _client.Options.MaxTokens = tokens;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:195:        Preferences.Default.Remove("ai_max_tokens");
src/AURA.Mobile/Pages/FixesPage.xaml.cs:30:            $"max_tokens: {_client.Options.MaxTokens}\n" +
src/AURA.Mobile/Pages/FixesPage.xaml.cs:61:            "Keys aceitas: model, provider, max_tokens, timeout_seconds, log_lines, api_key. " +
src/AURA.Mobile/Platforms/Android/AuraLog.cs:115:                    context.ContentResolver.Insert(MediaStore.Downloads.ExternalContentUri, values);
src/AURA.Mobile/Platforms/Android/AuraLog.cs:122:                Stream? stream = context.ContentResolver.OpenOutputStream(uri, "wa");
src/AURA.Mobile/Platforms/Android/AuraLog.cs:44:        private static Context? _appContext;
src/AURA.Mobile/Platforms/Android/AuraLog.cs:48:        /// <summary>Inicializa o caminho do arquivo de log (contexto disponível).</summary>
src/AURA.Mobile/Platforms/Android/AuraLog.cs:49:        public static void Init(Context context)
src/AURA.Mobile/Platforms/Android/AuraLog.cs:61:                        context.GetExternalFilesDir(null)?.AbsolutePath
src/AURA.Mobile/Platforms/Android/AuraLog.cs:62:                        ?? context.FilesDir?.AbsolutePath;
src/AURA.Mobile/Platforms/Android/AuraLog.cs:83:                    _appContext = context;
src/AURA.Mobile/Platforms/Android/AuraLog.cs:84:                    TryCreateDownloadMirror(context);
src/AURA.Mobile/Platforms/Android/AuraLog.cs:98:        private static void TryCreateDownloadMirror(Context context)
src/AURA.Mobile/Platforms/Android/MainApplication.cs:12:        // Logcat já funciona aqui; arquivo ainda não (sem Context útil) — é armazenado em buffer.
src/AURA.Mobile/Platforms/Android/MainApplication.cs:18:        // Contexto disponível: inicializa o arquivo de log e instala os handlers
src/AURA.Mobile/Platforms/Android/VpnHelper.cs:20:            AndroidApp.Context.StartActivity(intent);
src/AURA.Mobile/Platforms/Android/VpnHelper.cs:27:                AndroidApp.Context.PackageManager.GetPackageInfo(OrbotPackage, 0);
src/AURA.Mobile/Platforms/Android/VpnHelper.cs:40:                Intent launch = AndroidApp.Context
src/AURA.Mobile/Platforms/Android/VpnHelper.cs:48:                AndroidApp.Context.StartActivity(launch);
src/AURA.Mobile/Platforms/Android/WebView/AuraDownloadListener.cs:24:                var context = global::Android.App.Application.Context;
src/AURA.Mobile/Platforms/Android/WebView/AuraDownloadListener.cs:35:                context.StartActivity(intent);
src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs:7:    /// Em links/texto devolve false e mantém o menu de contexto nativo.
src/AURA.Modules/ModuleCatalog.cs:108:                ShortDescription = "Guarda preferências e histórico para a AURA lembrar do contexto entre sessões.",
src/AURA.Modules/ModuleCatalog.cs:122:                    "Continuidade de contexto entre sessões",
src/AURA.Modules/Runtime/CompatibilityChecker.cs:21:            report.Messages.Add($"Runtime '{runtime.Language}' OK: {runtime.Binary} {runtime.Version}");
src/AURA.Modules/Runtime/CompatibilityChecker.cs:26:            report.Messages.Add(!runtime.Available
src/AURA.Modules/Runtime/CompatibilityChecker.cs:37:                report.Messages.Add($"Manifesto encontrado: {dep.Name} → {dep.InstallCommand}");
src/AURA.Modules/Runtime/CompatibilityChecker.cs:44:                report.Messages.Add($"Dependência OK: {dep.Name}");
src/AURA.Modules/Runtime/CompatibilityChecker.cs:49:                report.Messages.Add($"Dependência faltando: {dep.Name} → {dep.InstallCommand}");
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:18:        "builtins", "collections", "concurrent", "configparser", "contextlib",
src/AURA.Modules/Runtime/RuntimeManager.cs:107:        foreach (string msg in report.Compat.Messages)
```

## 8. Memória
```
src/AURA.AI/AgentSession.cs:109:        private SolutionRule? TryGetKnownSolution(
src/AURA.AI/AgentSession.cs:110:            RequestContext request)
src/AURA.AI/AgentSession.cs:117:            return _solutionStore.Find(
src/AURA.AI/AgentSession.cs:28:        private readonly SolutionStore _solutionStore;
src/AURA.AI/AgentSession.cs:37:            _solutionStore = new SolutionStore();
src/AURA.AI/AgentSession.cs:9:using AURA.Memory;
src/AURA.AI/AiAssistant.cs:12:    /// persists the conversation turn in MemoryStore so context survives across
src/AURA.AI/AiAssistant.cs:13:    /// restarts (mirror of the mobile app's AURA.AI / MemoryService).
src/AURA.AI/AiAssistant.cs:18:        private readonly MemoryStore _memory;
src/AURA.AI/AiAssistant.cs:21:        public AiAssistant(OpenRouterClient client, MemoryStore memory, ILogger? logger = null)
src/AURA.AI/AiAssistant.cs:24:            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
src/AURA.AI/AiAssistant.cs:31:            _memory.Append(MemoryEntry.Question(question));
src/AURA.AI/AiAssistant.cs:34:            _memory.Append(MemoryEntry.Answer(answer));
src/AURA.AI/AiAssistant.cs:6:using AURA.Memory;
src/AURA.AI/AiAssistantService.cs:15:    /// <br/>Persists conversation history in MemoryStore for cross-session continuity.
src/AURA.AI/AiAssistantService.cs:28:        public static async Task<string> AskAsync(string question, MemoryStore? memory = null, ILogger? logger = null, OpenRouterOptions? options = null, HttpClient? http = null)
src/AURA.AI/AiAssistantService.cs:38:            if (memory != null)
src/AURA.AI/AiAssistantService.cs:40:                memory.Append(MemoryEntry.Question(question));
src/AURA.AI/AiAssistantService.cs:69:            if (memory != null)
src/AURA.AI/AiAssistantService.cs:71:                memory.Append(MemoryEntry.Answer(answer));
src/AURA.AI/AiAssistantService.cs:8:using AURA.Memory;
src/AURA.AI/OpenRouterClient.cs:11:using AURA.Memory;
src/AURA.AI/OpenRouterClient.cs:17:    /// provedor via MemoryService; aqui o cliente HTTP direto. Defaults seguem
src/AURA.CLI/Program.cs:600:                limits.MemoryLimitMb = value;
src/AURA.CLI/Program.cs:745:            Console.WriteLine("Memória              : " + result.AvailableMemoryGb + " GB livres de " + result.TotalMemoryGb + " GB");
src/AURA.Core/Abstractions/IModule.cs:5:    /// modules (Windows Assistant, AI, Automation, Memory, Plugins, ...).
src/AURA.Core/Configuration/ModulesConfiguration.cs:28:        public bool Memory { get; set; }
src/AURA.Core/Configuration/ModulesConfiguration.cs:50:                case "memory": return Memory;
src/AURA.Core/Configuration/ModulesConfiguration.cs:73:                case "memory": Memory = value; break;
src/AURA.Core/Runtime/ResourceLimits.cs:12:        /// <summary>Address-space (memory) cap in MiB, maps to `--as`.</summary>
src/AURA.Core/Runtime/ResourceLimits.cs:13:        public long? MemoryLimitMb { get; set; }
src/AURA.Core/Runtime/ResourceLimits.cs:26:            !MemoryLimitMb.HasValue &&
src/AURA.Core/Runtime/SimulationRuntime.cs:113:            if (l.MemoryLimitMb.HasValue) parts.Add("mem=" + l.MemoryLimitMb.Value + "M");
src/AURA.Core/Runtime/SimulationRuntime.cs:553:            if (limits.MemoryLimitMb.HasValue)
src/AURA.Core/Runtime/SimulationRuntime.cs:555:                parts.Add("--as=" + (limits.MemoryLimitMb.Value * 1024 * 1024));
src/AURA.Installer/FileIdentifier.cs:106:        int read = await stream.ReadAsync(buffer.AsMemory(0, byteCount), cancellationToken);
src/AURA.Installer/FileIdentifier.cs:114:        int read = await reader.ReadAsync(buffer.AsMemory(0, maxChars), cancellationToken);
src/AURA.Memory/MemoryEntry.cs:12:    public sealed class MemoryEntry
src/AURA.Memory/MemoryEntry.cs:14:        public MemoryKind Kind { get; set; }
src/AURA.Memory/MemoryEntry.cs:29:        public MemoryEntry()
src/AURA.Memory/MemoryEntry.cs:33:        public static MemoryEntry Question(string question)
src/AURA.Memory/MemoryEntry.cs:35:            return new MemoryEntry { Kind = MemoryKind.Turn, Role = "user", Text = question };
src/AURA.Memory/MemoryEntry.cs:38:        public static MemoryEntry Answer(string answer)
src/AURA.Memory/MemoryEntry.cs:40:            return new MemoryEntry { Kind = MemoryKind.Turn, Role = "assistant", Text = answer };
src/AURA.Memory/MemoryEntry.cs:43:        public static MemoryEntry CellStateChange(string cellId, string state)
src/AURA.Memory/MemoryEntry.cs:45:            return new MemoryEntry { Kind = MemoryKind.CellEvent, CellId = cellId, Detail = state };
src/AURA.Memory/MemoryEntry.cs:4:namespace AURA.Memory
src/AURA.Memory/MemoryEntry.cs:6:    public enum MemoryKind
src/AURA.Memory/MemoryStore.cs:104:                    return new MemoryDocument();
src/AURA.Memory/MemoryStore.cs:108:                MemoryDocument document = JsonSerializer.Deserialize<MemoryDocument>(json, Options);
src/AURA.Memory/MemoryStore.cs:109:                return document ?? new MemoryDocument();
src/AURA.Memory/MemoryStore.cs:114:                return new MemoryDocument();
src/AURA.Memory/MemoryStore.cs:118:        private void PersistLocked(MemoryDocument document)
src/AURA.Memory/MemoryStore.cs:11:    /// F3/F5 backend: short-term working memory for the assistant. Mirrors the
src/AURA.Memory/MemoryStore.cs:12:    /// memory store exposed by the mobile app (AURA.Memory) - an append-only
src/AURA.Memory/MemoryStore.cs:144:        private sealed class MemoryDocument
src/AURA.Memory/MemoryStore.cs:146:            public List<MemoryEntry> Entries { get; set; } = new List<MemoryEntry>();
src/AURA.Memory/MemoryStore.cs:14:    /// ~/AURA/memory.json so the assistant keeps context across restarts.
src/AURA.Memory/MemoryStore.cs:16:    /// This is the backend the mobile app's MemoryService/MemoryManager consume;
src/AURA.Memory/MemoryStore.cs:19:    public sealed class MemoryStore
src/AURA.Memory/MemoryStore.cs:32:        public MemoryStore(ILogger logger, string path = null)
src/AURA.Memory/MemoryStore.cs:35:            _path = path ?? SimulationRuntime.ExpandUserHome("~/AURA/memory.json");
src/AURA.Memory/MemoryStore.cs:40:        public void Append(MemoryEntry entry)
src/AURA.Memory/MemoryStore.cs:51:                    MemoryDocument document = LoadLocked();
src/AURA.Memory/MemoryStore.cs:64:        public IReadOnlyList<MemoryEntry> Read(int tail = 64)
src/AURA.Memory/MemoryStore.cs:68:                MemoryDocument document = LoadLocked();
src/AURA.Memory/MemoryStore.cs:70:                var slice = new List<MemoryEntry>();
src/AURA.Memory/MemoryStore.cs:8:namespace AURA.Memory
src/AURA.Memory/MemoryStore.cs:98:        private MemoryDocument LoadLocked()
src/AURA.Memory/RequestContext.cs:11:    public sealed class RequestContext
src/AURA.Memory/RequestContext.cs:4:namespace AURA.Memory
src/AURA.Memory/SolutionRule.cs:12:    public sealed class SolutionRule
src/AURA.Memory/SolutionRule.cs:4:namespace AURA.Memory
src/AURA.Memory/SolutionStore.cs:115:                List<SolutionRule> all = LoadLocked();
src/AURA.Memory/SolutionStore.cs:117:                SolutionRule? rule =
src/AURA.Memory/SolutionStore.cs:136:        private List<SolutionRule> LoadLocked()
src/AURA.Memory/SolutionStore.cs:141:                    return new List<SolutionRule>();
src/AURA.Memory/SolutionStore.cs:146:                    List<SolutionRule>>(json, Options)
src/AURA.Memory/SolutionStore.cs:147:                    ?? new List<SolutionRule>();
src/AURA.Memory/SolutionStore.cs:156:                return new List<SolutionRule>();
src/AURA.Memory/SolutionStore.cs:161:            List<SolutionRule> rules)
src/AURA.Memory/SolutionStore.cs:17:    public sealed class SolutionStore
src/AURA.Memory/SolutionStore.cs:31:        public SolutionStore(
src/AURA.Memory/SolutionStore.cs:44:        public IReadOnlyList<SolutionRule> ReadAll()
src/AURA.Memory/SolutionStore.cs:54:        public SolutionRule? Find(
src/AURA.Memory/SolutionStore.cs:71:        public void SaveValidated(SolutionRule rule)
src/AURA.Memory/SolutionStore.cs:86:                List<SolutionRule> all = LoadLocked();
src/AURA.Memory/SolutionStore.cs:88:                SolutionRule? existing =
src/AURA.Memory/SolutionStore.cs:9:namespace AURA.Memory
src/AURA.Mobile/MainPage.cs:19:            MemoryPage memory,
src/AURA.Mobile/MainPage.cs:41:                ("memory", "Assistente", "Memória", memory),
src/AURA.Mobile/MauiProgram.cs:113:        // Memória registra eventos de ciclo de vida das células (reativa MemoryKind.CellEvent).
src/AURA.Mobile/MauiProgram.cs:117:            var memory = app.Services.GetRequiredService<MemoryStore>();
src/AURA.Mobile/MauiProgram.cs:119:                memory.Append(MemoryEntry.CellStateChange(evt.CellId, evt.To)));
src/AURA.Mobile/MauiProgram.cs:123:            AuraLog.Exception("MauiProgram.MemoryEventSink", ex);
src/AURA.Mobile/MauiProgram.cs:55:        builder.Services.AddSingleton(sp => new MemoryStore(
src/AURA.Mobile/MauiProgram.cs:57:            Path.Combine(FileSystem.AppDataDirectory, "memory.json")));
src/AURA.Mobile/MauiProgram.cs:8:using AURA.Memory;
src/AURA.Mobile/MauiProgram.cs:98:        builder.Services.AddSingleton<MemoryPage>();
src/AURA.Mobile/Pages/ChatPage.xaml.cs:11:    public ChatPage(OpenRouterClient client, AURA.Memory.MemoryStore memory)
src/AURA.Mobile/Pages/ChatPage.xaml.cs:15:        _memory = memory;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:162:            var assistant = new AiAssistant(_client, _memory);
src/AURA.Mobile/Pages/ChatPage.xaml.cs:9:    private readonly AURA.Memory.MemoryStore _memory;
src/AURA.Mobile/Pages/HomePage.xaml.cs:41:            RamLabel.Text = $"RAM: {diagnostics.TotalMemoryGb:0.0} GB total / {diagnostics.AvailableMemoryGb:0.0} GB livre";
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs:91:            using var ms = new MemoryStream();
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:11:    public MemoryPage(MemoryStore memoryStore)
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:14:        _memoryStore = memoryStore;
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:2:using AURA.Memory;
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:33:            var entries = await Task.Run(() => _memoryStore.Read(64));
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:42:                Entries.Add(new MemoryEntry { Role = "AURA", Text = "Nenhuma memória registrada ainda." });
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:48:            Entries.Add(new MemoryEntry { Role = "Erro", Text = ex.Message });
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:60:        await Task.Run(() => _memoryStore.Clear());
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:6:public partial class MemoryPage : ContentPage
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:8:    private readonly MemoryStore _memoryStore;
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:9:    public ObservableCollection<MemoryEntry> Entries { get; } = new();
src/AURA.Mobile/Pages/RunPage.xaml.cs:75:            limits.MemoryLimitMb = mb;
src/AURA.Modules/ModuleCatalog.cs:105:                Id = "memory",
src/AURA.Modules/ModuleCatalog.cs:109:                PackageUrl = PackageBase + "/memory/module.json",
src/AURA.Modules/ModuleCatalog.cs:113:                Includes = new List<string> { "MemoryStore" },
src/AURA.Modules/ModuleStatus.cs:9:        /// <summary>Código existe e está em uso (ex.: AI, Memory, Plugins).</summary>
src/AURA.SystemInfo/SystemAnalyzer.cs:124:        private struct MEMORYSTATUSEX
src/AURA.SystemInfo/SystemAnalyzer.cs:127:            public uint dwMemoryLoad;
src/AURA.SystemInfo/SystemAnalyzer.cs:139:        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
src/AURA.SystemInfo/SystemAnalyzer.cs:31:            ReadMemory(result);
src/AURA.SystemInfo/SystemAnalyzer.cs:37:        private static void ReadMemory(SystemDiagnosticsResult result)
src/AURA.SystemInfo/SystemAnalyzer.cs:39:            result.TotalMemoryGb = 0;
src/AURA.SystemInfo/SystemAnalyzer.cs:40:            result.AvailableMemoryGb = 0;
src/AURA.SystemInfo/SystemAnalyzer.cs:46:                    ReadWindowsMemory(result);
src/AURA.SystemInfo/SystemAnalyzer.cs:50:                    ReadLinuxMemory(result);
src/AURA.SystemInfo/SystemAnalyzer.cs:59:        private static void ReadWindowsMemory(SystemDiagnosticsResult result)
src/AURA.SystemInfo/SystemAnalyzer.cs:61:            var status = new MEMORYSTATUSEX();
src/AURA.SystemInfo/SystemAnalyzer.cs:62:            status.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
src/AURA.SystemInfo/SystemAnalyzer.cs:64:            if (GlobalMemoryStatusEx(ref status))
src/AURA.SystemInfo/SystemAnalyzer.cs:66:                result.TotalMemoryGb = BytesToGb(status.ullTotalPhys);
src/AURA.SystemInfo/SystemAnalyzer.cs:67:                result.AvailableMemoryGb = BytesToGb(status.ullAvailPhys);
src/AURA.SystemInfo/SystemAnalyzer.cs:71:        private static void ReadLinuxMemory(SystemDiagnosticsResult result)
src/AURA.SystemInfo/SystemAnalyzer.cs:79:                    result.TotalMemoryGb = BytesToGb(KibToBytes(ParseKilobytes(line)));
src/AURA.SystemInfo/SystemAnalyzer.cs:83:                    result.AvailableMemoryGb = BytesToGb(KibToBytes(ParseKilobytes(line)));
src/AURA.SystemInfo/SystemDiagnosticsResult.cs:14:        public double TotalMemoryGb { get; set; }
src/AURA.SystemInfo/SystemDiagnosticsResult.cs:16:        public double AvailableMemoryGb { get; set; }
src/AURA.SystemInfo/SystemDiagnosticsResult.cs:4:    /// Result of a system analysis: operating system, CPU, memory and disk.
```

## 9. Ferramentas
```
src/AURA.AI/AgentChat.cs:16:        public string? ToolCallId { get; set; }
src/AURA.AI/AgentChat.cs:18:        public List<AgentToolCall>? ToolCalls { get; set; }
src/AURA.AI/AgentChat.cs:22:    public sealed class AgentToolCall
src/AURA.AI/AgentChat.cs:39:        public List<AgentToolCall>? ToolCalls { get; set; }
src/AURA.AI/AgentChat.cs:44:    /// <summary>Evento emitido pelo AgentSession a cada ferramenta executada (para a UI).</summary>
src/AURA.AI/AgentChat.cs:8:    /// null; o resultado da ferramenta volta com ToolCallId apontando o call.
src/AURA.AI/AgentSession.cs:107:        /// A consulta não executa a solução e não substitui a IA.
src/AURA.AI/AgentSession.cs:123:        private async Task<string> ExecuteToolAsync(
src/AURA.AI/AgentSession.cs:124:            AgentToolCall call,
src/AURA.AI/AgentSession.cs:127:            AgentTool? tool = _tools.FirstOrDefault(
src/AURA.AI/AgentSession.cs:147:                return await tool.ExecuteAsync(
src/AURA.AI/AgentSession.cs:15:    /// ferramentas registradas, executa as chamadas de ferramenta solicitadas
src/AURA.AI/AgentSession.cs:25:        private readonly List<AgentTool> _tools;
src/AURA.AI/AgentSession.cs:30:        public AgentSession(OpenRouterClient client, IEnumerable<AgentTool> tools,
src/AURA.AI/AgentSession.cs:311:                                    // list_dir sem valor real =
src/AURA.AI/AgentSession.cs:317:                                            "list_dir",
src/AURA.AI/AgentSession.cs:34:            _tools = (tools ?? Enumerable.Empty<AgentTool>()).ToList();
src/AURA.AI/AgentSession.cs:354:                // list_dir
src/AURA.AI/AgentSession.cs:356:                        "list_dir",
src/AURA.AI/AgentSession.cs:369:                // read_file
src/AURA.AI/AgentSession.cs:371:                        "read_file",
src/AURA.AI/AgentSession.cs:380:                // run_shell
src/AURA.AI/AgentSession.cs:382:                        "run_shell",
src/AURA.AI/AgentSession.cs:398:                // write_file
src/AURA.AI/AgentSession.cs:400:                        "write_file",
src/AURA.AI/AgentSession.cs:40:        /// <summary>Emitido a cada ferramenta executada (para atualizar a UI).</summary>
src/AURA.AI/AgentSession.cs:414:                // edit_file
src/AURA.AI/AgentSession.cs:416:                        "edit_file",
src/AURA.AI/AgentSession.cs:467:                    "list_dir",
src/AURA.AI/AgentSession.cs:474:                    "read_file",
src/AURA.AI/AgentSession.cs:481:                    "run_shell",
src/AURA.AI/AgentSession.cs:488:                    "write_file",
src/AURA.AI/AgentSession.cs:495:                    "edit_file",
src/AURA.AI/AgentSession.cs:70:                if (response.ToolCalls is { Count: > 0 })
src/AURA.AI/AgentSession.cs:76:                        ToolCalls = response.ToolCalls
src/AURA.AI/AgentSession.cs:79:                    foreach (AgentToolCall call in response.ToolCalls)
src/AURA.AI/AgentSession.cs:82:                        string result = await ExecuteToolAsync(call, ct).ConfigureAwait(false);
src/AURA.AI/AgentSession.cs:86:                            ToolCallId = call.Id,
src/AURA.AI/AgentTool.cs:17:    public sealed class AgentToolDefinition
src/AURA.AI/AgentTool.cs:23:        public Dictionary<string, AgentToolParameter> Parameters { get; } = new();
src/AURA.AI/AgentTool.cs:32:    public abstract class AgentTool
src/AURA.AI/AgentTool.cs:34:        public abstract AgentToolDefinition Definition { get; }
src/AURA.AI/AgentTool.cs:36:        public abstract Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default);
src/AURA.AI/AgentTool.cs:9:    public sealed class AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:112:    public sealed class WriteFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:118:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:11:    public sealed class ListDirTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:120:            Name = "write_file",
src/AURA.AI/AgentTools/FileTools.cs:124:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:129:                ["content"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:138:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/FileTools.cs:157:    public sealed class EditFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:163:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:165:            Name = "edit_file",
src/AURA.AI/AgentTools/FileTools.cs:169:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:174:                ["old_text"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:179:                ["new_text"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:17:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:188:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/FileTools.cs:19:            Name = "list_dir",
src/AURA.AI/AgentTools/FileTools.cs:23:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:31:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/FileTools.cs:71:    public sealed class ReadFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:77:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:79:            Name = "read_file",
src/AURA.AI/AgentTools/FileTools.cs:83:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:92:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/ShellAgentTool.cs:12:    /// Executa comandos shell (sh -c) dentro do workspace. Usado para git,
src/AURA.AI/AgentTools/ShellAgentTool.cs:15:    public sealed class ShellAgentTool : AgentTool
src/AURA.AI/AgentTools/ShellAgentTool.cs:23:        public ShellAgentTool(string workspaceRoot)
src/AURA.AI/AgentTools/ShellAgentTool.cs:29:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/ShellAgentTool.cs:31:            Name = "run_shell",
src/AURA.AI/AgentTools/ShellAgentTool.cs:32:            Description = "Executa um comando shell (sh -c) no diretório do workspace. " +
src/AURA.AI/AgentTools/ShellAgentTool.cs:36:                ["command"] = new AgentToolParameter
src/AURA.AI/AgentTools/ShellAgentTool.cs:45:        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/ShellAgentTool.cs:70:                UseShellExecute = false,
src/AURA.AI/AgentTools/WorkspaceAgentTool.cs:11:    public abstract class WorkspaceAgentTool : AgentTool
src/AURA.AI/AgentTools/WorkspaceAgentTool.cs:13:        protected WorkspaceAgentTool(string workspaceRoot)
src/AURA.AI/OpenRouterClient.cs:133:        /// modelo; o AgentSession executa as chamadas e faz o loop.
src/AURA.AI/OpenRouterClient.cs:137:            List<AgentToolDefinition>? tools = null,
src/AURA.AI/OpenRouterClient.cs:166:                    if (m.ToolCallId != null)
src/AURA.AI/OpenRouterClient.cs:168:                        mo["tool_call_id"] = m.ToolCallId;
src/AURA.AI/OpenRouterClient.cs:171:                    if (m.ToolCalls is { Count: > 0 })
src/AURA.AI/OpenRouterClient.cs:174:                        foreach (AgentToolCall tc in m.ToolCalls)
src/AURA.AI/OpenRouterClient.cs:200:                foreach (AgentToolDefinition t in tools)
src/AURA.AI/OpenRouterClient.cs:203:                    foreach (KeyValuePair<string, AgentToolParameter> p in t.Parameters)
src/AURA.AI/OpenRouterClient.cs:281:                        var calls = new List<AgentToolCall>();
src/AURA.AI/OpenRouterClient.cs:282:                        if (msg.TryGetProperty("tool_calls", out JsonElement toolCalls))
src/AURA.AI/OpenRouterClient.cs:284:                            foreach (JsonElement call in toolCalls.EnumerateArray())
src/AURA.AI/OpenRouterClient.cs:295:                                calls.Add(new AgentToolCall
src/AURA.AI/OpenRouterClient.cs:308:                            List<AgentToolCall>? textCalls = TryParseTextToolCall(content);
src/AURA.AI/OpenRouterClient.cs:315:                                    ToolCalls = textCalls
src/AURA.AI/OpenRouterClient.cs:323:                            ToolCalls = calls.Count > 0 ? calls : null
src/AURA.AI/OpenRouterClient.cs:33:    /// (testável sem rede) com BuildRequest; execute com ChatAsync.
src/AURA.AI/OpenRouterClient.cs:374:        private static List<AgentToolCall>? TryParseTextToolCall(string? content)
src/AURA.AI/OpenRouterClient.cs:426:                return new List<AgentToolCall>
src/AURA.AI/OpenRouterClient.cs:428:                    new AgentToolCall
src/AURA.Abstractions/Execution/ExecutionRequest.cs:12:    public sealed class ExecutionRequest
src/AURA.Abstractions/Execution/ExecutionRequest.cs:24:        /// <summary>Timeout da execução. Null = sem timeout.</summary>
src/AURA.Abstractions/Execution/ExecutionRequest.cs:4:namespace AURA.Abstractions.Execution
src/AURA.Abstractions/Execution/ExecutionRequest.cs:7:    /// Descreve um comando a ser executado por um IToolExecutor. O significado
src/AURA.Abstractions/Execution/ExecutionRequest.cs:8:    /// de <see cref="Command"/> e <see cref="Arguments"/> varia por executor
src/AURA.Abstractions/Execution/ExecutionResult.cs:21:        public static ExecutionResult Failed(string message)
src/AURA.Abstractions/Execution/ExecutionResult.cs:23:            return new ExecutionResult
src/AURA.Abstractions/Execution/ExecutionResult.cs:4:namespace AURA.Abstractions.Execution
src/AURA.Abstractions/Execution/ExecutionResult.cs:7:    /// Resultado padronizado de uma execução de IToolExecutor.
src/AURA.Abstractions/Execution/ExecutionResult.cs:9:    public sealed class ExecutionResult
src/AURA.Abstractions/Execution/IToolExecutor.cs:11:    public interface IToolExecutor
src/AURA.Abstractions/Execution/IToolExecutor.cs:18:        Task<ExecutionResult> ExecuteAsync(
src/AURA.Abstractions/Execution/IToolExecutor.cs:19:            ExecutionRequest request,
src/AURA.Abstractions/Execution/IToolExecutor.cs:4:namespace AURA.Abstractions.Execution
src/AURA.Abstractions/Execution/IToolExecutor.cs:7:    /// Contrato de um executor de ferramentas (shell, git, python, node, ...).
src/AURA.Abstractions/Execution/IToolExecutor.cs:8:    /// Cada executor resolve o binário e monta os argumentos a partir de um
src/AURA.Abstractions/Execution/IToolExecutor.cs:9:    /// ExecutionRequest, devolvendo sempre um ExecutionResult padronizado.
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:34:/// Valida a sintaxe de um arquivo sem executá-lo.
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:52:/// Monta e opcionalmente executa o plano de instalação.
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:59:    Task<IReadOnlyList<string>> ExecuteAsync(
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:67:/// compatibilidade → instala → executa → gerencia). Passos 1-9 — equivalente
src/AURA.Abstractions/Runtime/RuntimeModels.cs:101:/// Resultado da execução (passo 8 do pipeline). Fábrica <see cref="From"/>
src/AURA.Abstractions/Runtime/RuntimeModels.cs:102:/// converte um <see cref="ExecutionResult"/> existente da AURA.
src/AURA.Abstractions/Runtime/RuntimeModels.cs:104:public sealed class ExecutionOutcome
src/AURA.Abstractions/Runtime/RuntimeModels.cs:114:    public static ExecutionOutcome From(ExecutionResult result, bool timedOut = false, string command = "")
src/AURA.Abstractions/Runtime/RuntimeModels.cs:116:        return new ExecutionOutcome
src/AURA.Abstractions/Runtime/RuntimeModels.cs:142:    public ExecutionOutcome? Outcome { get; set; }
src/AURA.Abstractions/Runtime/RuntimeModels.cs:143:    public bool Executed { get; set; }
src/AURA.Abstractions/Runtime/RuntimeModels.cs:2:using AURA.Abstractions.Execution;
src/AURA.Agents/AgentManager.cs:131:            if (assistant.Executable == null || !File.Exists(assistant.Executable))
src/AURA.Agents/AgentManager.cs:14:    /// "Available" means its executable can be resolved on PATH or ~/bin.
src/AURA.Agents/AgentManager.cs:188:            if (assistant.Executable == null || !File.Exists(assistant.Executable))
src/AURA.Agents/AgentManager.cs:20:        public string Executable { get; set; }
src/AURA.Agents/AgentManager.cs:223:                return new Definition(assistant.Executable, string.Empty, "-s aura-ask");
src/AURA.Agents/AgentManager.cs:230:                return new Definition(assistant.Executable, "run", "run");
src/AURA.Agents/AgentManager.cs:234:            return new Definition(assistant.Executable, string.Empty, string.Empty);
src/AURA.Agents/AgentManager.cs:239:        /// repository (self-improvement space), not in the executable's dir.
src/AURA.Agents/AgentManager.cs:249:            string exeDir = Path.GetDirectoryName(assistant.Executable);
src/AURA.Agents/AgentManager.cs:26:            return Name + " -> " + Executable + (Description == null ? "" : " (" + Description + ")");
src/AURA.Agents/AgentManager.cs:304:        /// <summary>Finds an executable on PATH or ~/bin (mirror of PythonLauncher).</summary>
src/AURA.Agents/AgentManager.cs:305:        public static string ResolveExecutable(string name)
src/AURA.Agents/AgentManager.cs:54:                    Executable = ResolveExecutable("aichat")
src/AURA.Agents/AgentManager.cs:60:                    Executable = ResolveExecutable("termux-ai")
src/AURA.Agents/AgentManager.cs:66:                    Executable = ResolveExecutable("opencode")
src/AURA.Agents/AgentManager.cs:86:                if (a.Executable != null && File.Exists(a.Executable))
src/AURA.CLI/Program.cs:14:using AURA.Modules.Executors;
src/AURA.CLI/Program.cs:168:                    case "exec":
src/AURA.CLI/Program.cs:169:                        ExecCommand(parts);
src/AURA.CLI/Program.cs:298:                bool ok = agent.Executable != null && System.IO.File.Exists(agent.Executable);
src/AURA.CLI/Program.cs:308:        private static void ExecCommand(string[] parts)
src/AURA.CLI/Program.cs:312:                Console.WriteLine("Uso: exec <shell|git|python|node> <comando> [argumentos...]");
src/AURA.CLI/Program.cs:316:            IToolExecutor executor = parts[1].ToLowerInvariant() switch
src/AURA.CLI/Program.cs:325:            if (executor == null)
src/AURA.CLI/Program.cs:327:                Console.WriteLine("Executor desconhecido: " + parts[1] + " (use shell, git, python ou node)");
src/AURA.CLI/Program.cs:32:        private static readonly ShellExecutor Shell = new();
src/AURA.CLI/Program.cs:331:            if (!executor.IsAvailable())
src/AURA.CLI/Program.cs:333:                Console.WriteLine("Executor '" + executor.Name + "' não está disponível neste ambiente.");
src/AURA.CLI/Program.cs:337:            var request = new ExecutionRequest
src/AURA.CLI/Program.cs:33:        private static readonly GitExecutor Git = new();
src/AURA.CLI/Program.cs:344:            Console.WriteLine("Executando via " + executor.Name + ": " + request.Command +
src/AURA.CLI/Program.cs:348:            ExecutionResult result = executor.ExecuteAsync(request).GetAwaiter().GetResult();
src/AURA.CLI/Program.cs:34:        private static readonly PythonExecutor Python = new();
src/AURA.CLI/Program.cs:35:        private static readonly NodeExecutor Node = new();
src/AURA.CLI/Program.cs:403:        "Execute a solicitação do usuário de forma objetiva. " +
src/AURA.CLI/Program.cs:421:            var tools = new System.Collections.Generic.List<AgentTool>
src/AURA.CLI/Program.cs:427:                new ShellAgentTool(workspace)
src/AURA.CLI/Program.cs:444:            Console.WriteLine("Executando Aura...");
src/AURA.CLI/Program.cs:5:using AURA.Abstractions.Execution;
src/AURA.CLI/Program.cs:761:            Console.WriteLine("Comandos básicos: 'ajuda' para ajuda, 'run <arquivo>' para executar,");
src/AURA.CLI/Program.cs:811:            Console.WriteLine("  exec <shell|git|python|node> <cmd> [args]   Executa via executor");
src/AURA.Core/Abstractions/ICommand.cs:13:        void Execute(string[] args);
src/AURA.Core/Abstractions/ICommand.cs:4:    /// Represents a single executable command (used by the CLI and, later,
src/AURA.Core/Configuration/ModulesConfiguration.cs:29:        public bool Executors { get; set; }
src/AURA.Core/Configuration/ModulesConfiguration.cs:51:                case "executors": return Executors;
src/AURA.Core/Configuration/ModulesConfiguration.cs:74:                case "executors": Executors = value; break;
src/AURA.Core/Events/AuraEvents.cs:38:    /// Publicado quando um executor de ferramenta (shell/git/python/node)
src/AURA.Core/Events/AuraEvents.cs:39:    /// termina a execução.
src/AURA.Core/Events/AuraEvents.cs:41:    public sealed class ExecutorCompletedEvent : IEvent
src/AURA.Core/Events/AuraEvents.cs:43:        public string Executor { get; set; }
src/AURA.Core/Launchers/CellCommand.cs:7:    /// A fully resolved command for a cell: the executable plus any extra
src/AURA.Core/Runtime/SimulationRuntime.cs:528:                UseShellExecute = false,
src/AURA.Installer/ArtifactAnalysisService.cs:21:/// configuração, execução, gerenciamento) entram como novos métodos/serviços
src/AURA.Installer/FileIdentifier.cs:12:    // PE (Portable Executable): usado por .dll e .exe do .NET/Windows.
src/AURA.Installer/InstallationResult.cs:15:    /// <summary>Comando(s) que foram (ou seriam, em dry-run) executados, em texto legível.</summary>
src/AURA.Installer/InstallationResult.cs:5:/// simulação (DryRun=true, nada é executado) quanto pra instalação real.
src/AURA.Installer/PythonEnvironmentSelector.cs:1:using AURA.Abstractions.Execution;
src/AURA.Installer/PythonEnvironmentSelector.cs:21:    private readonly IToolExecutor _pythonExecutor;
src/AURA.Installer/PythonEnvironmentSelector.cs:25:        : this(new PythonExecutor(), () => new SystemAnalyzer().Analyze())
src/AURA.Installer/PythonEnvironmentSelector.cs:29:    /// <summary>Construtor para testes: permite injetar um executor e diagnósticos falsos.</summary>
src/AURA.Installer/PythonEnvironmentSelector.cs:2:using AURA.Modules.Executors;
src/AURA.Installer/PythonEnvironmentSelector.cs:30:    public PythonEnvironmentSelector(IToolExecutor pythonExecutor, Func<SystemDiagnosticsResult> diagnosticsProvider)
src/AURA.Installer/PythonEnvironmentSelector.cs:32:        _pythonExecutor = pythonExecutor;
src/AURA.Installer/PythonEnvironmentSelector.cs:41:        bool runtimeAvailable = _pythonExecutor.IsAvailable();
src/AURA.Installer/PythonEnvironmentSelector.cs:51:            RuntimeBinary = runtimeAvailable ? _pythonExecutor.Name : null,
src/AURA.Installer/PythonEnvironmentSelector.cs:8:/// Etapa 3 para artefatos Python: reaproveita <see cref="PythonExecutor.IsAvailable"/>
src/AURA.Installer/PythonInstaller.cs:10:/// o comando e devolve o que seria executado, sem tocar em nada.
src/AURA.Installer/PythonInstaller.cs:14:    private readonly IToolExecutor _pythonExecutor;
src/AURA.Installer/PythonInstaller.cs:16:    public PythonInstaller() : this(new PythonExecutor()) { }
src/AURA.Installer/PythonInstaller.cs:18:    /// <summary>Construtor para testes: permite injetar um executor falso.</summary>
src/AURA.Installer/PythonInstaller.cs:19:    public PythonInstaller(IToolExecutor pythonExecutor)
src/AURA.Installer/PythonInstaller.cs:1:using AURA.Abstractions.Execution;
src/AURA.Installer/PythonInstaller.cs:21:        _pythonExecutor = pythonExecutor;
src/AURA.Installer/PythonInstaller.cs:2:using AURA.Modules.Executors;
src/AURA.Installer/PythonInstaller.cs:45:            result.Notes.Add("[SIMULAÇÃO] Nenhum comando foi executado de verdade. Chame com dryRun: false pra instalar.");
src/AURA.Installer/PythonInstaller.cs:49:        if (!_pythonExecutor.IsAvailable())
src/AURA.Installer/PythonInstaller.cs:57:        var request = new ExecutionRequest
src/AURA.Installer/PythonInstaller.cs:64:        var execResult = await _pythonExecutor.ExecuteAsync(request, cancellationToken);
src/AURA.Installer/PythonInstaller.cs:66:        result.Success = execResult.Success;
src/AURA.Installer/PythonInstaller.cs:67:        result.StandardOutput = execResult.StandardOutput;
src/AURA.Installer/PythonInstaller.cs:68:        result.StandardError = execResult.StandardError;
src/AURA.Installer/PythonInstaller.cs:70:        if (!execResult.Success)
src/AURA.Installer/PythonInstaller.cs:72:            result.Notes.Add($"pip install terminou com código {execResult.ExitCode}.");
src/AURA.Installer/PythonInstaller.cs:8:/// o PythonExecutor já existente (evita duplicar lógica de resolução de
src/AURA.Memory/RequestContext.cs:9:    /// que a AURA já conhece e consegue executar.
src/AURA.Memory/SolutionRule.cs:9:    /// Uma regra só deve ser marcada como validada depois que sua execução
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:6:    /// Configuração aplicável em tempo de execução (sem recompilar o APK).
src/AURA.Mobile/MainPage.cs:20:            ExecutorsPage executors,
src/AURA.Mobile/MainPage.cs:43:                ("executors", "Ferramentas", "Executores", executors),
src/AURA.Mobile/MauiProgram.cs:11:using AURA.Modules.Executors;
src/AURA.Mobile/MauiProgram.cs:76:        // Executores do repo (Shell/Git/Python/Node) expostos na UI de status.
src/AURA.Mobile/MauiProgram.cs:77:        builder.Services.AddSingleton<ShellExecutor>();
src/AURA.Mobile/MauiProgram.cs:78:        builder.Services.AddSingleton<GitExecutor>();
src/AURA.Mobile/MauiProgram.cs:79:        builder.Services.AddSingleton<PythonExecutor>();
src/AURA.Mobile/MauiProgram.cs:80:        builder.Services.AddSingleton<NodeExecutor>();
src/AURA.Mobile/MauiProgram.cs:99:        builder.Services.AddSingleton<ExecutorsPage>();
src/AURA.Mobile/Pages/AgentPage.xaml.cs:40:        var tools = new List<AgentTool>
src/AURA.Mobile/Pages/AgentPage.xaml.cs:46:            new ShellAgentTool(root)
src/AURA.Mobile/Pages/AgentPage.xaml.cs:54:            "workspace e executar comandos shell (sh -c) nesse diretório. " +
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:102:            ResultEditor.Text = "Erro ao executar: " + ex.Message;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:103:            AuraLog.Exception("ExecutorsPage.Execute", ex);
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:107:            ExecButton.IsEnabled = true;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:10:    private readonly GitExecutor _git;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:11:    private readonly PythonExecutor _python;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:121:        ExecutorsView.ItemsSource = statuses;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:125:    private static ExecutorStatus MakeStatus(ProcessExecutorBase executor)
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:127:        bool available = executor.IsAvailable();
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:128:        return new ExecutorStatus
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:12:    private readonly NodeExecutor _node;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:130:            Name = executor.Name,
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:139:public class ExecutorStatus
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:15:    public ExecutorsPage(ShellExecutor shell, GitExecutor git, PythonExecutor python, NodeExecutor node, EventBus events)
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:1:using AURA.Abstractions.Execution;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:23:        ExecutorPicker.ItemsSource = new[] { "Shell", "Git", "Python", "Node" };
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:24:        ExecutorPicker.SelectedIndex = 0;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:38:    private async void OnExecuteClicked(object sender, EventArgs e)
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:3:using AURA.Modules.Executors;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:40:        string selected = ExecutorPicker.SelectedItem as string;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:41:        ProcessExecutorBase executor = selected switch
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:50:        if (executor == null)
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:52:            ResultEditor.Text = "Selecione um executor.";
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:56:        if (!executor.IsAvailable())
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:58:            ResultEditor.Text = "Ferramenta '" + executor.Name + "' não disponível neste dispositivo.";
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:72:        var request = new ExecutionRequest
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:79:        ExecButton.IsEnabled = false;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:7:public partial class ExecutorsPage : ContentPage
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:80:        ResultEditor.Text = "Executando...";
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:83:            ExecutionResult result = await executor.ExecuteAsync(request);
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:92:            _events.Publish(new ExecutorCompletedEvent
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:94:                Executor = executor.Name,
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:9:    private readonly ShellExecutor _shell;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:56:            "Receba o log de execução e a configuração atual do app. Identifique problemas " +
src/AURA.Mobile/Pages/FixesPage.xaml.cs:57:            "e proponha correções que possam ser aplicadas em tempo de execução " +
src/AURA.Mobile/Pages/LogsPage.xaml.cs:181:            "feito em .NET MAUI). Receba o log de execução do app e: " +
src/AURA.Mobile/Pages/RunPage.xaml.cs:80:            ResultLabel.Text = "Escolha um arquivo ou informe um executável.";
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:117:            var request = new ExecutionRequest
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:124:            ExecutionResult result = await _shell.ExecuteAsync(request);
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:13:    public TerminalPage(ShellExecutor shell)
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:1:using AURA.Abstractions.Execution;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:2:using AURA.Modules.Executors;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:8:    private readonly ShellExecutor _shell;
src/AURA.Mobile/Platforms/Android/AuraLog.cs:16:    /// Objetivo: registrar o máximo de informação desde o PRIMEIRO frame de execução
src/AURA.Modules/Executors/GitExecutor.cs:11:public sealed class GitExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/GitExecutor.cs:17:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Executors/GitExecutor.cs:1:using AURA.Abstractions.Execution;
src/AURA.Modules/Executors/GitExecutor.cs:20:            return Task.FromResult(ExecutionResult.Failed("git não encontrado no ambiente."));
src/AURA.Modules/Executors/GitExecutor.cs:30:    // Por enquanto, ExecuteAsync genérico cobre qualquer subcomando git.
src/AURA.Modules/Executors/GitExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/GitExecutor.cs:6:/// Executor para o Git. request.Command é o subcomando (ex: "status", "diff",
src/AURA.Modules/Executors/NodeExecutor.cs:15:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Executors/NodeExecutor.cs:18:            return Task.FromResult(ExecutionResult.Failed("Node.js não encontrado no ambiente."));
src/AURA.Modules/Executors/NodeExecutor.cs:1:using AURA.Abstractions.Execution;
src/AURA.Modules/Executors/NodeExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/NodeExecutor.cs:6:/// Executor para Node.js. request.Command é o script (ex: "index.js") ou
src/AURA.Modules/Executors/NodeExecutor.cs:9:public sealed class NodeExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/ProcessExecutorBase.cs:11:/// executor concreto só precise resolver o binário e montar os argumentos.
src/AURA.Modules/Executors/ProcessExecutorBase.cs:13:public abstract class ProcessExecutorBase : IToolExecutor
src/AURA.Modules/Executors/ProcessExecutorBase.cs:17:    public abstract Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default);
src/AURA.Modules/Executors/ProcessExecutorBase.cs:19:    /// <summary>Executa um processo já resolvido (fileName + argumentos) e devolve o resultado padronizado.</summary>
src/AURA.Modules/Executors/ProcessExecutorBase.cs:20:    protected static async Task<ExecutionResult> RunAsync(
src/AURA.Modules/Executors/ProcessExecutorBase.cs:23:        ExecutionRequest request,
src/AURA.Modules/Executors/ProcessExecutorBase.cs:32:            UseShellExecute = false,
src/AURA.Modules/Executors/ProcessExecutorBase.cs:3:using AURA.Abstractions.Execution;
src/AURA.Modules/Executors/ProcessExecutorBase.cs:5:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/ProcessExecutorBase.cs:71:            return new ExecutionResult
src/AURA.Modules/Executors/ProcessExecutorBase.cs:76:                StandardError = stderr + "\n[AURA] Execução cancelada por timeout.",
src/AURA.Modules/Executors/ProcessExecutorBase.cs:83:            return ExecutionResult.Failed($"[AURA] Falha ao iniciar '{fileName}': {ex.Message}");
src/AURA.Modules/Executors/ProcessExecutorBase.cs:88:        return new ExecutionResult
src/AURA.Modules/Executors/ProcessExecutorBase.cs:8:/// Base compartilhada por todos os executores que rodam um processo externo
src/AURA.Modules/Executors/PythonExecutor.cs:10:public sealed class PythonExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/PythonExecutor.cs:16:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Executors/PythonExecutor.cs:19:            return Task.FromResult(ExecutionResult.Failed("Python não encontrado (tentado: python3, python)."));
src/AURA.Modules/Executors/PythonExecutor.cs:1:using AURA.Abstractions.Execution;
src/AURA.Modules/Executors/PythonExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/PythonExecutor.cs:6:/// Executor para Python. Tenta resolver "python3" primeiro (padrão no Termux),
src/AURA.Modules/Executors/ShellExecutor.cs:14:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Executors/ShellExecutor.cs:17:            return Task.FromResult(ExecutionResult.Failed("Shell (/bin/sh) não encontrado no ambiente."));
src/AURA.Modules/Executors/ShellExecutor.cs:1:using AURA.Abstractions.Execution;
src/AURA.Modules/Executors/ShellExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/ShellExecutor.cs:6:/// Executor base: roda comandos diretamente via shell (sh -c).
src/AURA.Modules/Executors/ShellExecutor.cs:8:public sealed class ShellExecutor : ProcessExecutorBase
src/AURA.Modules/ModuleCatalog.cs:131:                Id = "executors",
src/AURA.Modules/ModuleCatalog.cs:132:                DisplayName = "Executores",
src/AURA.Modules/ModuleCatalog.cs:134:                ShortDescription = "Executa comandos Shell, Git, Python e Node com saída capturada.",
src/AURA.Modules/ModuleCatalog.cs:135:                PackageUrl = PackageBase + "/executors/module.json",
src/AURA.Modules/ModuleCatalog.cs:139:                Includes = new List<string> { "ShellExecutor", "GitExecutor", "PythonExecutor", "NodeExecutor" },
src/AURA.Modules/ModuleCatalog.cs:142:                    "Integração com cada executor",
src/AURA.Modules/ModuleCatalog.cs:183:                ShortDescription = "Processos isolados com ciclo de vida, limites e a rota de execução automática.",
src/AURA.Modules/ModuleCatalog.cs:208:                ShortDescription = "Visualiza o log de execução da AURA e aplica correções de sistema.",
src/AURA.Modules/ModuleCatalog.cs:212:                Features = new List<string> { "Log de execução", "Correções de sistema" },
src/AURA.Modules/ModuleCatalog.cs:239:                    "Integrar execução de PowerShell com saída capturada",
src/AURA.Modules/ModuleCatalog.cs:266:                    "Execução automática de tarefas",
src/AURA.Modules/ModuleCatalog.cs:97:                    "Execução de tarefas em arquivos"
src/AURA.Modules/Runtime/BinaryPath.cs:4:/// Procura binários no PATH. Espelha <c>ProcessExecutorBase.ResolveBinary</c>
src/AURA.Modules/Runtime/CompatibilityChecker.cs:7:/// Verifica ANTES de executar se o ambiente está pronto: runtime presente e
src/AURA.Modules/Runtime/CompatibilityChecker.cs:86:                UseShellExecute = false,
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:60:        "set", "shift", "source", "eval", "exec", "trap", "ulimit", "umask",
src/AURA.Modules/Runtime/Installer.cs:42:    public async Task<IReadOnlyList<string>> ExecuteAsync(
src/AURA.Modules/Runtime/Installer.cs:59:            Console.Write("Executar agora? [s/N] ");
src/AURA.Modules/Runtime/Installer.cs:7:/// Monta e opcionalmente executa o plano de instalação. Por segurança o plano
src/AURA.Modules/Runtime/Installer.cs:88:            UseShellExecute = false,
src/AURA.Modules/Runtime/Installer.cs:8:/// é sempre construído primeiro e exibido; a execução só roda com confirmação.
src/AURA.Modules/Runtime/RuntimeCatalog.cs:43:    /// <summary>Linguagens de dados/documentos que não têm runtime executável.</summary>
src/AURA.Modules/Runtime/RuntimeManager.cs:112:        // [6] Plano de instalação (executa só se autorizado)
src/AURA.Modules/Runtime/RuntimeManager.cs:122:            IReadOnlyList<string> results = await _installer.ExecuteAsync(
src/AURA.Modules/Runtime/RuntimeManager.cs:138:        // [7/8] Execução
src/AURA.Modules/Runtime/RuntimeManager.cs:139:        report.Outcome = await ExecuteAsync(report, args, timeout, workdir, cancellationToken);
src/AURA.Modules/Runtime/RuntimeManager.cs:140:        report.Executed = true;
src/AURA.Modules/Runtime/RuntimeManager.cs:141:        report.Steps.Add("execucao");
src/AURA.Modules/Runtime/RuntimeManager.cs:142:        report.Log($"Execução: {(report.Outcome.Success ? "OK" : "FALHOU")} " +
src/AURA.Modules/Runtime/RuntimeManager.cs:175:    private static async Task<ExecutionOutcome> ExecuteAsync(
src/AURA.Modules/Runtime/RuntimeManager.cs:185:            return new ExecutionOutcome
src/AURA.Modules/Runtime/RuntimeManager.cs:192:        var executor = new RuntimeProcessExecutor(runtime);
src/AURA.Modules/Runtime/RuntimeManager.cs:193:        if (!executor.IsAvailable())
src/AURA.Modules/Runtime/RuntimeManager.cs:195:            return new ExecutionOutcome
src/AURA.Modules/Runtime/RuntimeManager.cs:1:using AURA.Abstractions.Execution;
src/AURA.Modules/Runtime/RuntimeManager.cs:204:        var request = new ExecutionRequest
src/AURA.Modules/Runtime/RuntimeManager.cs:212:        ExecutionResult result = await executor.ExecuteAsync(request, cancellationToken);
src/AURA.Modules/Runtime/RuntimeManager.cs:215:                        result.StandardError.Contains("[AURA] Execução cancelada por timeout.");
src/AURA.Modules/Runtime/RuntimeManager.cs:217:        return ExecutionOutcome.From(result, timedOut, string.Join(' ', commandArgs.Prepend(fileName)));
src/AURA.Modules/Runtime/RuntimeManager.cs:62:        // Linguagens sem runtime executável (dados/documentos)
src/AURA.Modules/Runtime/RuntimeManager.cs:66:            report.Log($"'{language}' não é um programa executável.");
src/AURA.Modules/Runtime/RuntimeManager.cs:9:/// compatibilidade → instala (se autorizado) → executa → gerencia resultado.
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:10:/// sem shell). Equivalente a <c>executor.py</c>.
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:12:public sealed class RuntimeProcessExecutor : ProcessExecutorBase
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:17:    public RuntimeProcessExecutor(RuntimeResolution runtime)
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:1:using AURA.Abstractions.Execution;
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:27:    public override Task<ExecutionResult> ExecuteAsync(
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:28:        ExecutionRequest request,
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:3:using AURA.Modules.Executors;
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:8:/// Executa o programa com o runtime resolvido, reutilizando a base
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:9:/// <see cref="ProcessExecutorBase"/> (timeout, captura de stdout/stderr,
src/AURA.Modules/Runtime/RuntimeResolver.cs:22:                Detail = "linguagem sem runtime executável (dado/documento)",
src/AURA.Modules/Runtime/RuntimeResolver.cs:85:                UseShellExecute = false,
src/AURA.Modules/Runtime/SyntaxValidator.cs:45:                UseShellExecute = false,
src/AURA.Modules/Runtime/SyntaxValidator.cs:66:                Detail = "sintaxe inválida — corrija antes de executar",
src/AURA.Modules/Runtime/SyntaxValidator.cs:8:/// executar o programa. Roda ANTES da instalação (não instale nada para um
```

## 10. Executores
```
src/AURA.AI/AgentSession.cs:147:                return await tool.ExecuteAsync(
src/AURA.AI/AgentSession.cs:380:                // run_shell
src/AURA.AI/AgentSession.cs:382:                        "run_shell",
src/AURA.AI/AgentSession.cs:386:                            "command",
src/AURA.AI/AgentSession.cs:387:                            out object? command))
src/AURA.AI/AgentSession.cs:390:                            Convert.ToString(command) ??
src/AURA.AI/AgentSession.cs:393:                        output["command"] =
src/AURA.AI/AgentSession.cs:481:                    "run_shell",
src/AURA.AI/AgentSession.cs:484:                return "{\"command\":\"\"}";
src/AURA.AI/AgentTool.cs:36:        public abstract Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default);
src/AURA.AI/AgentTools/FileTools.cs:138:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/FileTools.cs:188:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/FileTools.cs:31:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/FileTools.cs:92:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/ShellAgentTool.cs:12:    /// Executa comandos shell (sh -c) dentro do workspace. Usado para git,
src/AURA.AI/AgentTools/ShellAgentTool.cs:15:    public sealed class ShellAgentTool : AgentTool
src/AURA.AI/AgentTools/ShellAgentTool.cs:21:        private readonly string _shellPath;
src/AURA.AI/AgentTools/ShellAgentTool.cs:23:        public ShellAgentTool(string workspaceRoot)
src/AURA.AI/AgentTools/ShellAgentTool.cs:26:            _shellPath = File.Exists("/system/bin/sh") ? "/system/bin/sh" : "/bin/sh";
src/AURA.AI/AgentTools/ShellAgentTool.cs:31:            Name = "run_shell",
src/AURA.AI/AgentTools/ShellAgentTool.cs:32:            Description = "Executa um comando shell (sh -c) no diretório do workspace. " +
src/AURA.AI/AgentTools/ShellAgentTool.cs:36:                ["command"] = new AgentToolParameter
src/AURA.AI/AgentTools/ShellAgentTool.cs:39:                    Description = "Comando shell completo (ex.: 'git status --short')."
src/AURA.AI/AgentTools/ShellAgentTool.cs:42:            Required = { "command" }
src/AURA.AI/AgentTools/ShellAgentTool.cs:45:        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/ShellAgentTool.cs:47:            string command;
src/AURA.AI/AgentTools/ShellAgentTool.cs:50:                command = ReadString(doc.RootElement, "command") ?? string.Empty;
src/AURA.AI/AgentTools/ShellAgentTool.cs:53:            if (string.IsNullOrWhiteSpace(command))
src/AURA.AI/AgentTools/ShellAgentTool.cs:58:            if (!File.Exists(_shellPath))
src/AURA.AI/AgentTools/ShellAgentTool.cs:60:                return "ERRO: shell não encontrado neste dispositivo (" + _shellPath + ").";
src/AURA.AI/AgentTools/ShellAgentTool.cs:63:            var psi = new ProcessStartInfo
src/AURA.AI/AgentTools/ShellAgentTool.cs:65:                FileName = _shellPath,
src/AURA.AI/AgentTools/ShellAgentTool.cs:66:                Arguments = "-c \"" + command.Replace("\"", "\\\"") + "\"",
src/AURA.AI/AgentTools/ShellAgentTool.cs:70:                UseShellExecute = false,
src/AURA.Abstractions/Execution/ExecutionRequest.cs:10:    /// argumentos dele; no Shell, Command é o comando completo).
src/AURA.Abstractions/Execution/ExecutionRequest.cs:14:        public string Command { get; set; } = string.Empty;
src/AURA.Abstractions/Execution/ExecutionRequest.cs:7:    /// Descreve um comando a ser executado por um IToolExecutor. O significado
src/AURA.Abstractions/Execution/ExecutionRequest.cs:8:    /// de <see cref="Command"/> e <see cref="Arguments"/> varia por executor
src/AURA.Abstractions/Execution/ExecutionRequest.cs:9:    /// (ex.: no Git, Command é o subcomando "status" e Arguments são os
src/AURA.Abstractions/Execution/ExecutionResult.cs:7:    /// Resultado padronizado de uma execução de IToolExecutor.
src/AURA.Abstractions/Execution/IToolExecutor.cs:11:    public interface IToolExecutor
src/AURA.Abstractions/Execution/IToolExecutor.cs:18:        Task<ExecutionResult> ExecuteAsync(
src/AURA.Abstractions/Execution/IToolExecutor.cs:7:    /// Contrato de um executor de ferramentas (shell, git, python, node, ...).
src/AURA.Abstractions/Execution/IToolExecutor.cs:8:    /// Cada executor resolve o binário e monta os argumentos a partir de um
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:25:/// Analisa dependências de um arquivo (imports, manifestos, binários shell).
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:59:    Task<IReadOnlyList<string>> ExecuteAsync(
src/AURA.Abstractions/Runtime/RuntimeModels.cs:112:    public string Command { get; set; } = string.Empty;
src/AURA.Abstractions/Runtime/RuntimeModels.cs:114:    public static ExecutionOutcome From(ExecutionResult result, bool timedOut = false, string command = "")
src/AURA.Abstractions/Runtime/RuntimeModels.cs:124:            Command = command,
src/AURA.Abstractions/Runtime/RuntimeModels.cs:46:    public string InstallCommand { get; set; } = string.Empty;
src/AURA.Abstractions/Runtime/RuntimeModels.cs:89:public sealed record InstallStep(string What, string Command, bool IsRuntime);
src/AURA.CLI/Program.cs:120:                RunCommand(cmd);
src/AURA.CLI/Program.cs:124:        private static void RunCommand(string cmd)
src/AURA.CLI/Program.cs:14:using AURA.Modules.Executors;
src/AURA.CLI/Program.cs:159:                        ChatCommand(parts);
src/AURA.CLI/Program.cs:163:                        AuraCommand(parts);
src/AURA.CLI/Program.cs:166:                        AiKeyCommand(parts);
src/AURA.CLI/Program.cs:169:                        ExecCommand(parts);
src/AURA.CLI/Program.cs:182:                        CellCommand(parts);
src/AURA.CLI/Program.cs:308:        private static void ExecCommand(string[] parts)
src/AURA.CLI/Program.cs:312:                Console.WriteLine("Uso: exec <shell|git|python|node> <comando> [argumentos...]");
src/AURA.CLI/Program.cs:316:            IToolExecutor executor = parts[1].ToLowerInvariant() switch
src/AURA.CLI/Program.cs:318:                "shell" => Shell,
src/AURA.CLI/Program.cs:325:            if (executor == null)
src/AURA.CLI/Program.cs:327:                Console.WriteLine("Executor desconhecido: " + parts[1] + " (use shell, git, python ou node)");
src/AURA.CLI/Program.cs:32:        private static readonly ShellExecutor Shell = new();
src/AURA.CLI/Program.cs:331:            if (!executor.IsAvailable())
src/AURA.CLI/Program.cs:333:                Console.WriteLine("Executor '" + executor.Name + "' não está disponível neste ambiente.");
src/AURA.CLI/Program.cs:339:                Command = parts[2],
src/AURA.CLI/Program.cs:33:        private static readonly GitExecutor Git = new();
src/AURA.CLI/Program.cs:344:            Console.WriteLine("Executando via " + executor.Name + ": " + request.Command +
src/AURA.CLI/Program.cs:348:            ExecutionResult result = executor.ExecuteAsync(request).GetAwaiter().GetResult();
src/AURA.CLI/Program.cs:34:        private static readonly PythonExecutor Python = new();
src/AURA.CLI/Program.cs:35:        private static readonly NodeExecutor Node = new();
src/AURA.CLI/Program.cs:365:        private static void ChatCommand(string[] parts)
src/AURA.CLI/Program.cs:408:private static void AuraCommand(string[] parts)
src/AURA.CLI/Program.cs:427:                new ShellAgentTool(workspace)
src/AURA.CLI/Program.cs:460:        private static void AiKeyCommand(string[] parts)
src/AURA.CLI/Program.cs:652:        private static void CellCommand(string[] parts)
src/AURA.CLI/Program.cs:811:            Console.WriteLine("  exec <shell|git|python|node> <cmd> [args]   Executa via executor");
src/AURA.CLI/Program.cs:92:                RunCommand(string.Join(" ", args));
src/AURA.Core/Abstractions/ICommand.cs:4:    /// Represents a single executable command (used by the CLI and, later,
src/AURA.Core/Abstractions/ICommand.cs:7:    public interface ICommand
src/AURA.Core/Configuration/ModulesConfiguration.cs:29:        public bool Executors { get; set; }
src/AURA.Core/Configuration/ModulesConfiguration.cs:51:                case "executors": return Executors;
src/AURA.Core/Configuration/ModulesConfiguration.cs:74:                case "executors": Executors = value; break;
src/AURA.Core/Events/AuraEvents.cs:38:    /// Publicado quando um executor de ferramenta (shell/git/python/node)
src/AURA.Core/Events/AuraEvents.cs:41:    public sealed class ExecutorCompletedEvent : IEvent
src/AURA.Core/Events/AuraEvents.cs:43:        public string Executor { get; set; }
src/AURA.Core/Events/AuraEvents.cs:45:        public string Command { get; set; }
src/AURA.Core/Launchers/CellCommand.cs:10:    public sealed class CellCommand
src/AURA.Core/Launchers/CellCommand.cs:12:        public CellCommand(string fileName, string arguments = null)
src/AURA.Core/Launchers/CellCommand.cs:7:    /// A fully resolved command for a cell: the executable plus any extra
src/AURA.Core/Launchers/DllLauncher.cs:21:        public CellCommand BuildCommand(string filePath, string arguments)
src/AURA.Core/Launchers/DllLauncher.cs:23:            return new CellCommand("dotnet", "\"" + filePath + "\" " + arguments);
src/AURA.Core/Launchers/GoLauncher.cs:22:        public CellCommand BuildCommand(string filePath, string arguments)
src/AURA.Core/Launchers/GoLauncher.cs:24:            return new CellCommand("go", "run \"" + filePath + "\" " + arguments);
src/AURA.Core/Launchers/ILauncher.cs:18:        /// <summary>Builds the command line used to start the file in a cell.</summary>
src/AURA.Core/Launchers/ILauncher.cs:19:        CellCommand BuildCommand(string filePath, string arguments);
src/AURA.Core/Launchers/JarLauncher.cs:22:        public CellCommand BuildCommand(string filePath, string arguments)
src/AURA.Core/Launchers/JarLauncher.cs:24:            return new CellCommand("java", "-jar \"" + filePath + "\" " + arguments);
src/AURA.Core/Launchers/NodeLauncher.cs:22:        public CellCommand BuildCommand(string filePath, string arguments)
src/AURA.Core/Launchers/NodeLauncher.cs:24:            return new CellCommand("node", "\"" + filePath + "\" " + arguments);
src/AURA.Core/Launchers/PythonLauncher.cs:22:        public CellCommand BuildCommand(string filePath, string arguments)
src/AURA.Core/Launchers/PythonLauncher.cs:25:            return new CellCommand(python, "\"" + filePath + "\" " + arguments);
src/AURA.Core/Launchers/Runner.cs:80:            CellCommand command = launcher.BuildCommand(filePath, arguments);
src/AURA.Core/Launchers/Runner.cs:88:            Cell cell = runtime.CreateCell(id, command.FileName, command.Arguments,
src/AURA.Core/Runtime/ResourceLimits.cs:7:    /// command with `prlimit` (available on Termux and Linux), so a runaway
src/AURA.Core/Runtime/SimulationRuntime.cs:518:                BuildPrlimitCommand(cell.Limits, ref fileName, ref arguments);
src/AURA.Core/Runtime/SimulationRuntime.cs:521:            var psi = new ProcessStartInfo
src/AURA.Core/Runtime/SimulationRuntime.cs:528:                UseShellExecute = false,
src/AURA.Core/Runtime/SimulationRuntime.cs:545:        /// Rewrites the command as `prlimit --as=.. --cpu=.. --nofile=.. --nproc=..
src/AURA.Core/Runtime/SimulationRuntime.cs:547:        /// command with the limits set; child processes inherit them.
src/AURA.Core/Runtime/SimulationRuntime.cs:549:        private void BuildPrlimitCommand(ResourceLimits limits, ref string fileName, ref string arguments)
src/AURA.Installer/InstallationResult.cs:16:    public List<string> Commands { get; set; } = new();
src/AURA.Installer/PythonEnvironmentSelector.cs:21:    private readonly IToolExecutor _pythonExecutor;
src/AURA.Installer/PythonEnvironmentSelector.cs:25:        : this(new PythonExecutor(), () => new SystemAnalyzer().Analyze())
src/AURA.Installer/PythonEnvironmentSelector.cs:29:    /// <summary>Construtor para testes: permite injetar um executor e diagnósticos falsos.</summary>
src/AURA.Installer/PythonEnvironmentSelector.cs:2:using AURA.Modules.Executors;
src/AURA.Installer/PythonEnvironmentSelector.cs:30:    public PythonEnvironmentSelector(IToolExecutor pythonExecutor, Func<SystemDiagnosticsResult> diagnosticsProvider)
src/AURA.Installer/PythonEnvironmentSelector.cs:32:        _pythonExecutor = pythonExecutor;
src/AURA.Installer/PythonEnvironmentSelector.cs:41:        bool runtimeAvailable = _pythonExecutor.IsAvailable();
src/AURA.Installer/PythonEnvironmentSelector.cs:51:            RuntimeBinary = runtimeAvailable ? _pythonExecutor.Name : null,
src/AURA.Installer/PythonEnvironmentSelector.cs:59:            result.InstallRuntimeSuggestions.AddRange(SuggestPythonInstallCommands());
src/AURA.Installer/PythonEnvironmentSelector.cs:80:    private static List<string> SuggestPythonInstallCommands()
src/AURA.Installer/PythonEnvironmentSelector.cs:8:/// Etapa 3 para artefatos Python: reaproveita <see cref="PythonExecutor.IsAvailable"/>
src/AURA.Installer/PythonInstaller.cs:14:    private readonly IToolExecutor _pythonExecutor;
src/AURA.Installer/PythonInstaller.cs:16:    public PythonInstaller() : this(new PythonExecutor()) { }
src/AURA.Installer/PythonInstaller.cs:18:    /// <summary>Construtor para testes: permite injetar um executor falso.</summary>
src/AURA.Installer/PythonInstaller.cs:19:    public PythonInstaller(IToolExecutor pythonExecutor)
src/AURA.Installer/PythonInstaller.cs:21:        _pythonExecutor = pythonExecutor;
src/AURA.Installer/PythonInstaller.cs:2:using AURA.Modules.Executors;
src/AURA.Installer/PythonInstaller.cs:33:        string commandText = $"python -m pip install {string.Join(" ", dependencies.Dependencies)}";
src/AURA.Installer/PythonInstaller.cs:39:            Commands = { commandText }
src/AURA.Installer/PythonInstaller.cs:49:        if (!_pythonExecutor.IsAvailable())
src/AURA.Installer/PythonInstaller.cs:59:            Command = "-m",
src/AURA.Installer/PythonInstaller.cs:64:        var execResult = await _pythonExecutor.ExecuteAsync(request, cancellationToken);
src/AURA.Installer/PythonInstaller.cs:8:/// o PythonExecutor já existente (evita duplicar lógica de resolução de
src/AURA.Mobile/MainPage.cs:20:            ExecutorsPage executors,
src/AURA.Mobile/MainPage.cs:43:                ("executors", "Ferramentas", "Executores", executors),
src/AURA.Mobile/MauiProgram.cs:11:using AURA.Modules.Executors;
src/AURA.Mobile/MauiProgram.cs:76:        // Executores do repo (Shell/Git/Python/Node) expostos na UI de status.
src/AURA.Mobile/MauiProgram.cs:77:        builder.Services.AddSingleton<ShellExecutor>();
src/AURA.Mobile/MauiProgram.cs:78:        builder.Services.AddSingleton<GitExecutor>();
src/AURA.Mobile/MauiProgram.cs:79:        builder.Services.AddSingleton<PythonExecutor>();
src/AURA.Mobile/MauiProgram.cs:80:        builder.Services.AddSingleton<NodeExecutor>();
src/AURA.Mobile/MauiProgram.cs:99:        builder.Services.AddSingleton<ExecutorsPage>();
src/AURA.Mobile/Pages/AgentPage.xaml.cs:106:        string text = CommandEditor.Text?.Trim() ?? string.Empty;
src/AURA.Mobile/Pages/AgentPage.xaml.cs:114:        CommandEditor.Text = string.Empty;
src/AURA.Mobile/Pages/AgentPage.xaml.cs:46:            new ShellAgentTool(root)
src/AURA.Mobile/Pages/AgentPage.xaml.cs:54:            "workspace e executar comandos shell (sh -c) nesse diretório. " +
src/AURA.Mobile/Pages/AgentPage.xaml.cs:65:            "rodar comandos shell. O que deseja fazer?", user: false);
src/AURA.Mobile/Pages/CellsPage.xaml.cs:103:        if ((sender as Button)?.CommandParameter is not Cell cell)
src/AURA.Mobile/Pages/CellsPage.xaml.cs:114:        if ((sender as Button)?.CommandParameter is not Cell cell)
src/AURA.Mobile/Pages/CellsPage.xaml.cs:51:        if ((sender as Button)?.CommandParameter is not Cell cell)
src/AURA.Mobile/Pages/CellsPage.xaml.cs:70:        if ((sender as Button)?.CommandParameter is not Cell cell)
src/AURA.Mobile/Pages/CellsPage.xaml.cs:81:        if ((sender as Button)?.CommandParameter is not Cell cell)
src/AURA.Mobile/Pages/CellsPage.xaml.cs:92:        if ((sender as Button)?.CommandParameter is not Cell cell)
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:103:            AuraLog.Exception("ExecutorsPage.Execute", ex);
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:10:    private readonly GitExecutor _git;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:115:            MakeStatus(_shell),
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:11:    private readonly PythonExecutor _python;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:121:        ExecutorsView.ItemsSource = statuses;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:125:    private static ExecutorStatus MakeStatus(ProcessExecutorBase executor)
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:127:        bool available = executor.IsAvailable();
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:128:        return new ExecutorStatus
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:12:    private readonly NodeExecutor _node;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:130:            Name = executor.Name,
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:139:public class ExecutorStatus
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:15:    public ExecutorsPage(ShellExecutor shell, GitExecutor git, PythonExecutor python, NodeExecutor node, EventBus events)
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:18:        _shell = shell;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:23:        ExecutorPicker.ItemsSource = new[] { "Shell", "Git", "Python", "Node" };
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:24:        ExecutorPicker.SelectedIndex = 0;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:3:using AURA.Modules.Executors;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:40:        string selected = ExecutorPicker.SelectedItem as string;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:41:        ProcessExecutorBase executor = selected switch
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:43:            "Shell" => _shell,
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:50:        if (executor == null)
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:52:            ResultEditor.Text = "Selecione um executor.";
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:56:        if (!executor.IsAvailable())
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:58:            ResultEditor.Text = "Ferramenta '" + executor.Name + "' não disponível neste dispositivo.";
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:62:        string command = CommandEntry.Text?.Trim() ?? string.Empty;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:63:        if (string.IsNullOrWhiteSpace(command))
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:65:            ResultEditor.Text = "Informe um comando (ex.: git → status; python → script.py; shell → ls).";
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:74:            Command = command,
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:7:public partial class ExecutorsPage : ContentPage
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:83:            ExecutionResult result = await executor.ExecuteAsync(request);
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:92:            _events.Publish(new ExecutorCompletedEvent
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:94:                Executor = executor.Name,
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:95:                Command = command,
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:9:    private readonly ShellExecutor _shell;
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:26:            var row = (ModuleRow)button.CommandParameter;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:119:                Command = command,
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:124:            ExecutionResult result = await _shell.ExecuteAsync(request);
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:13:    public TerminalPage(ShellExecutor shell)
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:144:            AuraLog.Exception("TerminalPage.RunCommand", ex);
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:16:        _shell = shell;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:26:    private async void OnCommandCompleted(object sender, EventArgs e)
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:28:        await RunCommandAsync(CommandEntry.Text);
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:2:using AURA.Modules.Executors;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:39:        CommandEntry.Text = _history[_historyIndex];
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:40:        CommandEntry.CursorPosition = CommandEntry.Text.Length;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:51:        CommandEntry.Text = _historyIndex >= _history.Count ? string.Empty : _history[_historyIndex];
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:52:        CommandEntry.CursorPosition = CommandEntry.Text.Length;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:60:    private async Task RunCommandAsync(string? input)
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:62:        string command = input?.Trim() ?? string.Empty;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:63:        if (command.Length == 0)
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:68:        _history.Add(command);
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:70:        CommandEntry.Text = string.Empty;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:72:        AppendLine("$ " + command, prompt: true);
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:74:        string lower = command.ToLowerInvariant();
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:8:    private readonly ShellExecutor _shell;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:97:            string target = command.Substring(3).Trim();
src/AURA.Modules/Executors/GitExecutor.cs:11:public sealed class GitExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/GitExecutor.cs:17:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Executors/GitExecutor.cs:22:        var args = new List<string> { request.Command };
src/AURA.Modules/Executors/GitExecutor.cs:30:    // Por enquanto, ExecuteAsync genérico cobre qualquer subcomando git.
src/AURA.Modules/Executors/GitExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/GitExecutor.cs:6:/// Executor para o Git. request.Command é o subcomando (ex: "status", "diff",
src/AURA.Modules/Executors/GitExecutor.cs:8:/// (ex: ["-m", "mensagem"]). Rodar via argumentos separados (sem shell)
src/AURA.Modules/Executors/NodeExecutor.cs:15:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Executors/NodeExecutor.cs:20:        var args = new List<string> { request.Command };
src/AURA.Modules/Executors/NodeExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/NodeExecutor.cs:6:/// Executor para Node.js. request.Command é o script (ex: "index.js") ou
src/AURA.Modules/Executors/NodeExecutor.cs:9:public sealed class NodeExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/ProcessExecutorBase.cs:11:/// executor concreto só precise resolver o binário e montar os argumentos.
src/AURA.Modules/Executors/ProcessExecutorBase.cs:13:public abstract class ProcessExecutorBase : IToolExecutor
src/AURA.Modules/Executors/ProcessExecutorBase.cs:17:    public abstract Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default);
src/AURA.Modules/Executors/ProcessExecutorBase.cs:26:        var psi = new ProcessStartInfo
src/AURA.Modules/Executors/ProcessExecutorBase.cs:32:            UseShellExecute = false,
src/AURA.Modules/Executors/ProcessExecutorBase.cs:5:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/ProcessExecutorBase.cs:8:/// Base compartilhada por todos os executores que rodam um processo externo
src/AURA.Modules/Executors/ProcessExecutorBase.cs:9:/// (Shell, Git, Dotnet, Python, Node, Cargo, Java). Centraliza a lógica de
src/AURA.Modules/Executors/PythonExecutor.cs:10:public sealed class PythonExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/PythonExecutor.cs:16:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Executors/PythonExecutor.cs:21:        var args = new List<string> { request.Command };
src/AURA.Modules/Executors/PythonExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/PythonExecutor.cs:6:/// Executor para Python. Tenta resolver "python3" primeiro (padrão no Termux),
src/AURA.Modules/Executors/PythonExecutor.cs:7:/// caindo para "python" se necessário. request.Command é o script/módulo/flag
src/AURA.Modules/Executors/ShellExecutor.cs:10:    public override string Name => "shell";
src/AURA.Modules/Executors/ShellExecutor.cs:14:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Executors/ShellExecutor.cs:17:            return Task.FromResult(ExecutionResult.Failed("Shell (/bin/sh) não encontrado no ambiente."));
src/AURA.Modules/Executors/ShellExecutor.cs:19:        var fullCommand = request.Arguments.Count > 0
src/AURA.Modules/Executors/ShellExecutor.cs:20:            ? $"{request.Command} {string.Join(' ', request.Arguments)}"
src/AURA.Modules/Executors/ShellExecutor.cs:21:            : request.Command;
src/AURA.Modules/Executors/ShellExecutor.cs:23:        return RunAsync("/bin/sh", new[] { "-c", fullCommand }, request, cancellationToken);
src/AURA.Modules/Executors/ShellExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/ShellExecutor.cs:6:/// Executor base: roda comandos diretamente via shell (sh -c).
src/AURA.Modules/Executors/ShellExecutor.cs:8:public sealed class ShellExecutor : ProcessExecutorBase
src/AURA.Modules/ModuleCatalog.cs:131:                Id = "executors",
src/AURA.Modules/ModuleCatalog.cs:132:                DisplayName = "Executores",
src/AURA.Modules/ModuleCatalog.cs:134:                ShortDescription = "Executa comandos Shell, Git, Python e Node com saída capturada.",
src/AURA.Modules/ModuleCatalog.cs:135:                PackageUrl = PackageBase + "/executors/module.json",
src/AURA.Modules/ModuleCatalog.cs:138:                Features = new List<string> { "Shell", "Git", "Python", "Node" },
src/AURA.Modules/ModuleCatalog.cs:139:                Includes = new List<string> { "ShellExecutor", "GitExecutor", "PythonExecutor", "NodeExecutor" },
src/AURA.Modules/ModuleCatalog.cs:142:                    "Integração com cada executor",
src/AURA.Modules/ModuleCatalog.cs:172:                    "Interação direta com o shell"
src/AURA.Modules/ModuleCatalog.cs:234:                ShortDescription = "Automatiza tarefas do Windows: WMI, Registro, Serviços e PowerShell.",
src/AURA.Modules/ModuleCatalog.cs:235:                Includes = new List<string> { "WMI", "Registro", "Serviços", "PowerShell" },
src/AURA.Modules/ModuleCatalog.cs:239:                    "Integrar execução de PowerShell com saída capturada",
src/AURA.Modules/Runtime/BinaryPath.cs:4:/// Procura binários no PATH. Espelha <c>ProcessExecutorBase.ResolveBinary</c>
src/AURA.Modules/Runtime/CompatibilityChecker.cs:37:                report.Messages.Add($"Manifesto encontrado: {dep.Name} → {dep.InstallCommand}");
src/AURA.Modules/Runtime/CompatibilityChecker.cs:49:                report.Messages.Add($"Dependência faltando: {dep.Name} → {dep.InstallCommand}");
src/AURA.Modules/Runtime/CompatibilityChecker.cs:81:            var psi = new ProcessStartInfo
src/AURA.Modules/Runtime/CompatibilityChecker.cs:86:                UseShellExecute = false,
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:10:/// binários invocados em scripts shell. Equivalente a <c>deps.py</c>.
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:117:                InstallCommand = $"pip install {package}",
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:133:                    InstallCommand = $"pip install -r {manifest}",
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:150:                InstallCommand = "npm install",
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:155:    private void AnalyzeShell(string filePath, DependencyReport report)
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:161:        foreach (Match match in ShellCommandRegex.Matches(text))
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:163:            string command = match.Groups[1].Value;
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:164:            if (command.StartsWith("sudo ", StringComparison.Ordinal))
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:166:                command = command[5..];
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:168:            if (ShellWhitelist.Contains(command) || !seen.Add(command))
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:175:                Name = command,
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:178:                InstallCommand = $"pkg install {command}",
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:194:                InstallCommand = "mvn dependency:resolve",
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:49:    private static readonly Regex ShellCommandRegex = new(
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:53:    private static readonly HashSet<string> ShellWhitelist = new(StringComparer.Ordinal)
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:61:        "which", "type", "command", "let", "find", "xargs", "basename", "dirname",
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:82:            case "shell": AnalyzeShell(filePath, report); break;
src/AURA.Modules/Runtime/Installer.cs:22:                Command: runtime.InstallHint,
src/AURA.Modules/Runtime/Installer.cs:31:                if (string.IsNullOrWhiteSpace(dep.InstallCommand) || !seen.Add(dep.Name))
src/AURA.Modules/Runtime/Installer.cs:35:                plan.Steps.Add(new InstallStep(What: dep.Name, Command: dep.InstallCommand, IsRuntime: false));
src/AURA.Modules/Runtime/Installer.cs:42:    public async Task<IReadOnlyList<string>> ExecuteAsync(
src/AURA.Modules/Runtime/Installer.cs:57:                Console.WriteLine($"  - {step.What}: {step.Command}");
src/AURA.Modules/Runtime/Installer.cs:70:            Console.WriteLine($">>> {step.Command}");
src/AURA.Modules/Runtime/Installer.cs:71:            results.Add($"{step.Command}: {await RunInstallCommandAsync(step.Command, cancellationToken)}");
src/AURA.Modules/Runtime/Installer.cs:78:    private static async Task<string> RunInstallCommandAsync(string command, CancellationToken cancellationToken)
src/AURA.Modules/Runtime/Installer.cs:80:        // Instalação usa o shell (pkg install / pip install / npm install).
src/AURA.Modules/Runtime/Installer.cs:81:        string shell = BinaryPath.FindOnPath("bash") ?? BinaryPath.FindOnPath("sh")
src/AURA.Modules/Runtime/Installer.cs:82:            ?? throw new InvalidOperationException("Nenhum shell (bash/sh) encontrado no PATH.");
src/AURA.Modules/Runtime/Installer.cs:83:        var psi = new ProcessStartInfo
src/AURA.Modules/Runtime/Installer.cs:85:            FileName = shell,
src/AURA.Modules/Runtime/Installer.cs:88:            UseShellExecute = false,
src/AURA.Modules/Runtime/Installer.cs:92:        psi.ArgumentList.Add(command);
src/AURA.Modules/Runtime/LanguageDetector.cs:16:        (new Regex(@"bash"), "shell"),
src/AURA.Modules/Runtime/LanguageDetector.cs:17:        (new Regex(@"sh\b"), "shell"),
src/AURA.Modules/Runtime/LanguageDetector.cs:18:        (new Regex(@"zsh"), "shell"),
src/AURA.Modules/Runtime/RuntimeCatalog.cs:25:            ["shell"] = new(new[] { "bash", "sh" }, new[] { "--version" },
src/AURA.Modules/Runtime/RuntimeCatalog.cs:53:            [".py"] = "python", [".pyw"] = "python", [".sh"] = "shell",
src/AURA.Modules/Runtime/RuntimeCatalog.cs:54:            [".bash"] = "shell", [".zsh"] = "shell", [".ksh"] = "shell",
src/AURA.Modules/Runtime/RuntimeManager.cs:122:            IReadOnlyList<string> results = await _installer.ExecuteAsync(
src/AURA.Modules/Runtime/RuntimeManager.cs:134:                report.Log($"  - {step.What}: {step.Command}");
src/AURA.Modules/Runtime/RuntimeManager.cs:139:        report.Outcome = await ExecuteAsync(report, args, timeout, workdir, cancellationToken);
src/AURA.Modules/Runtime/RuntimeManager.cs:175:    private static async Task<ExecutionOutcome> ExecuteAsync(
src/AURA.Modules/Runtime/RuntimeManager.cs:192:        var executor = new RuntimeProcessExecutor(runtime);
src/AURA.Modules/Runtime/RuntimeManager.cs:193:        if (!executor.IsAvailable())
src/AURA.Modules/Runtime/RuntimeManager.cs:202:        (string fileName, List<string> commandArgs) = BuildCommand(runtime, report.File, args);
src/AURA.Modules/Runtime/RuntimeManager.cs:206:            Command = fileName,
src/AURA.Modules/Runtime/RuntimeManager.cs:207:            Arguments = commandArgs,
src/AURA.Modules/Runtime/RuntimeManager.cs:212:        ExecutionResult result = await executor.ExecuteAsync(request, cancellationToken);
src/AURA.Modules/Runtime/RuntimeManager.cs:217:        return ExecutionOutcome.From(result, timedOut, string.Join(' ', commandArgs.Prepend(fileName)));
src/AURA.Modules/Runtime/RuntimeManager.cs:220:    /// <summary>Monta (binário, argumentos) por linguagem — sem shell.</summary>
src/AURA.Modules/Runtime/RuntimeManager.cs:221:    private static (string, List<string>) BuildCommand(
src/AURA.Modules/Runtime/RuntimeManager.cs:224:        var commandArgs = new List<string>();
src/AURA.Modules/Runtime/RuntimeManager.cs:230:                commandArgs.Add("-jar");
src/AURA.Modules/Runtime/RuntimeManager.cs:233:                commandArgs.Add("run");
src/AURA.Modules/Runtime/RuntimeManager.cs:237:        commandArgs.Add(filePath);
src/AURA.Modules/Runtime/RuntimeManager.cs:238:        if (args is not null) commandArgs.AddRange(args);
src/AURA.Modules/Runtime/RuntimeManager.cs:240:        return (binary, commandArgs);
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:10:/// sem shell). Equivalente a <c>executor.py</c>.
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:12:public sealed class RuntimeProcessExecutor : ProcessExecutorBase
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:17:    public RuntimeProcessExecutor(RuntimeResolution runtime)
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:27:    public override Task<ExecutionResult> ExecuteAsync(
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:31:        return RunAsync(request.Command, request.Arguments, request, cancellationToken);
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:3:using AURA.Modules.Executors;
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:9:/// <see cref="ProcessExecutorBase"/> (timeout, captura de stdout/stderr,
src/AURA.Modules/Runtime/RuntimeResolver.cs:80:            var psi = new ProcessStartInfo
src/AURA.Modules/Runtime/RuntimeResolver.cs:85:                UseShellExecute = false,
src/AURA.Modules/Runtime/SyntaxValidator.cs:107:            "shell" => new[] { "bash", "sh" },
src/AURA.Modules/Runtime/SyntaxValidator.cs:40:            var psi = new ProcessStartInfo
src/AURA.Modules/Runtime/SyntaxValidator.cs:45:                UseShellExecute = false,
src/AURA.Modules/Runtime/SyntaxValidator.cs:91:            case "shell": return ("bash -n", new[] { binary, "-n" });
```

## 11. CLI
```
src/AURA.CLI/Program.cs:133:                    case "diagnostico":
src/AURA.CLI/Program.cs:134:                    case "diag":
src/AURA.CLI/Program.cs:137:                    case "internet":
src/AURA.CLI/Program.cs:140:                    case "modulos":
src/AURA.CLI/Program.cs:143:                    case "config":
src/AURA.CLI/Program.cs:144:                        PrintConfig();
src/AURA.CLI/Program.cs:146:                    case "launchers":
src/AURA.CLI/Program.cs:149:                    case "plugins":
src/AURA.CLI/Program.cs:150:                        PrintPlugins();
src/AURA.CLI/Program.cs:152:                    case "agents":
src/AURA.CLI/Program.cs:153:                        PrintAgents();
src/AURA.CLI/Program.cs:155:                    case "ask":
src/AURA.CLI/Program.cs:158:                    case "chat":
src/AURA.CLI/Program.cs:161:                    case "aura":
src/AURA.CLI/Program.cs:162:                    case "agent":
src/AURA.CLI/Program.cs:165:                    case "aichave":
src/AURA.CLI/Program.cs:168:                    case "exec":
src/AURA.CLI/Program.cs:171:                    case "run":
src/AURA.CLI/Program.cs:174:                    case "cells":
src/AURA.CLI/Program.cs:175:                        PrintCells();
src/AURA.CLI/Program.cs:177:                    case "persist":
src/AURA.CLI/Program.cs:178:                    case "save":
src/AURA.CLI/Program.cs:181:                    case "cell":
src/AURA.CLI/Program.cs:184:                    case "ajuda":
src/AURA.CLI/Program.cs:185:                    case "help":
src/AURA.CLI/Program.cs:205:                Console.WriteLine("Uso: run <arquivo> [argumentos...] [--cell <id>] [--wait]");
src/AURA.CLI/Program.cs:211:            // F3: run aichat|termux-ai starts a long-lived assistant cell.
src/AURA.CLI/Program.cs:22:    /// how to run it (launcher resolution) and inside which cell (isolation).
src/AURA.CLI/Program.cs:292:        private static void PrintAgents()
src/AURA.CLI/Program.cs:294:            Console.WriteLine("Assistentes configurados:");
src/AURA.CLI/Program.cs:296:            foreach (AgentInfo agent in _agentManager.Assistants)
src/AURA.CLI/Program.cs:312:                Console.WriteLine("Uso: exec <shell|git|python|node> <comando> [argumentos...]");
src/AURA.CLI/Program.cs:370:                Console.WriteLine("Uso: chat \"sua pergunta\" [--model <modelo>]");
src/AURA.CLI/Program.cs:432:            var session = new AgentSession(client, tools, systemPrompt);
src/AURA.CLI/Program.cs:556:                Console.WriteLine("Uso: ask \"sua pergunta\" [--assistente aichat] [--cell <id>]");
src/AURA.CLI/Program.cs:629:        private static void PrintCells()
src/AURA.CLI/Program.cs:631:            Cell[] cells = _runtime.Cells.ToArray();
src/AURA.CLI/Program.cs:633:            if (cells.Length == 0)
src/AURA.CLI/Program.cs:635:                Console.WriteLine("Nenhuma célula. Use 'run <arquivo>' para criar uma.");
src/AURA.CLI/Program.cs:639:            Console.WriteLine("Células (" + _runtime.CellsRoot + "):");
src/AURA.CLI/Program.cs:642:            foreach (Cell cell in cells)
src/AURA.CLI/Program.cs:665:                case "start":
src/AURA.CLI/Program.cs:669:                case "stop":
src/AURA.CLI/Program.cs:672:                case "pause":
src/AURA.CLI/Program.cs:675:                case "resume":
src/AURA.CLI/Program.cs:678:                case "delete":
src/AURA.CLI/Program.cs:67:            bootstrap.Events.Subscribe<CellStateChangedEvent>(evt =>
src/AURA.CLI/Program.cs:681:                case "log":
src/AURA.CLI/Program.cs:684:                case "limits":
src/AURA.CLI/Program.cs:6:using AURA.Agents;
src/AURA.CLI/Program.cs:720:        private static void PrintPlugins()
src/AURA.CLI/Program.cs:722:            Console.WriteLine("Plugins (" + _pluginWatcher.PluginsRoot + "):");
src/AURA.CLI/Program.cs:735:            Console.WriteLine("Launchers de plugins : " + _pluginWatcher.Launchers.Count);
src/AURA.CLI/Program.cs:736:            Console.WriteLine("Plugins IPlugin      : " + _pluginWatcher.Plugins.Count);
src/AURA.CLI/Program.cs:753:            Console.WriteLine("Acesso à Internet : " + (status.HasInternetAccess ? "Sim" : "Não"));
src/AURA.CLI/Program.cs:761:            Console.WriteLine("Comandos básicos: 'ajuda' para ajuda, 'run <arquivo>' para executar,");
src/AURA.CLI/Program.cs:762:            Console.WriteLine("'agents' para listar assistentes, 'config' para ver a configuração.");
src/AURA.CLI/Program.cs:766:        private static void PrintConfig()
src/AURA.CLI/Program.cs:768:            Console.WriteLine("Configuração (" + _bootstrap.SettingsPath + "):");
src/AURA.CLI/Program.cs:769:            Console.WriteLine("  Internet           : " + _bootstrap.Settings.Internet);
src/AURA.CLI/Program.cs:797:            Console.WriteLine("Comandos:");
src/AURA.CLI/Program.cs:798:            Console.WriteLine("  run <arquivo> [args]   Escolhe um programa; AURA decide como rodar");
src/AURA.CLI/Program.cs:799:            Console.WriteLine("  run --wait app.go      Roda em primeiro plano e mostra a saída");
src/AURA.CLI/Program.cs:800:            Console.WriteLine("  run --mem 256 --cpu 30 app.py   Aplica limites (prlimit) à célula");
src/AURA.CLI/Program.cs:801:            Console.WriteLine("  cells                   Lista as células");
src/AURA.CLI/Program.cs:804:            Console.WriteLine("  diagnostico             Diagnóstico do sistema");
src/AURA.CLI/Program.cs:805:            Console.WriteLine("  internet                Verifica conexão");
src/AURA.CLI/Program.cs:806:            Console.WriteLine("  agents                  Lista assistentes (aichat/termux-ai)");
src/AURA.CLI/Program.cs:807:            Console.WriteLine("  ask \"pergunta\"          Pergunta via assistente, logada em célula");
src/AURA.CLI/Program.cs:808:            Console.WriteLine("  chat \"pergunta\"          Pergunta direta à IA (OpenRouter) [--model x]");
src/AURA.CLI/Program.cs:809:            Console.WriteLine("  agent \"instrução\"        Agente de arquivos num workspace (IA + ferramentas)");
src/AURA.CLI/Program.cs:811:            Console.WriteLine("  exec <shell|git|python|node> <cmd> [args]   Executa via executor");
src/AURA.CLI/Program.cs:812:            Console.WriteLine("  run aichat --cell chat  Inicia assistente como célula");
src/AURA.CLI/Program.cs:813:            Console.WriteLine("  modulos                 Lista módulos disponíveis");
src/AURA.CLI/Program.cs:814:            Console.WriteLine("  config                  Mostra configuração (settings + módulos)");
src/AURA.CLI/Program.cs:816:            Console.WriteLine("  plugins                 Lista plugins carregados");
```

## 12. Runtime / módulos / plugins
```
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:10:public interface IRuntimeDetector
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:16:/// Resolve o runtime necessário para uma linguagem (PATH + versão mínima).
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:19:public interface IRuntimeResolver
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:21:    RuntimeResolution Resolve(string language);
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:28:public interface IDependencyAnalyzer
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:43:/// Verifica se o ambiente está pronto (runtime + deps + binários auxiliares).
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:48:    CompatReport Check(RuntimeResolution runtime, DependencyReport deps, bool checkNetwork = false);
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:4:namespace AURA.Abstractions.Runtime;
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:55:public interface IRuntimeInstaller
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:57:    InstallPlan BuildPlan(RuntimeResolution? runtime, DependencyReport? deps);
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:70:public interface IRuntimeManager
src/AURA.Abstractions/Runtime/RuntimeModels.cs:130:/// Resultado do pipeline completo (RuntimeManager).
src/AURA.Abstractions/Runtime/RuntimeModels.cs:137:    public RuntimeResolution? Runtime { get; set; }
src/AURA.Abstractions/Runtime/RuntimeModels.cs:22:/// Resultado da resolução do runtime (passo 2 do pipeline).
src/AURA.Abstractions/Runtime/RuntimeModels.cs:23:/// Equivalente a <c>RuntimeResolution</c> do protótipo Python.
src/AURA.Abstractions/Runtime/RuntimeModels.cs:25:public sealed class RuntimeResolution
src/AURA.Abstractions/Runtime/RuntimeModels.cs:47:    public bool IsRuntime { get; set; }
src/AURA.Abstractions/Runtime/RuntimeModels.cs:4:namespace AURA.Abstractions.Runtime;
src/AURA.Abstractions/Runtime/RuntimeModels.cs:60:    public IReadOnlyList<Dependency> Runtimes => Dependencies.FindAll(d => d.IsRuntime);
src/AURA.Abstractions/Runtime/RuntimeModels.cs:61:    public IReadOnlyList<Dependency> Packages => Dependencies.FindAll(d => !d.IsRuntime);
src/AURA.Abstractions/Runtime/RuntimeModels.cs:80:    public bool RuntimeOk { get; set; }
src/AURA.Abstractions/Runtime/RuntimeModels.cs:85:    public bool Ok => RuntimeOk && DependenciesOk && AuxiliaryOk;
src/AURA.Abstractions/Runtime/RuntimeModels.cs:89:public sealed record InstallStep(string What, string Command, bool IsRuntime);
src/AURA.Agents/AgentManager.cs:112:        public async Task<string> AskAsync(SimulationRuntime runtime, string question,
src/AURA.Agents/AgentManager.cs:115:            if (runtime == null)
src/AURA.Agents/AgentManager.cs:117:                throw new ArgumentNullException(nameof(runtime));
src/AURA.Agents/AgentManager.cs:144:            Cell cell = runtime.CreateCell(cellId,
src/AURA.Agents/AgentManager.cs:148:            await runtime.StartCellAsync(cell.Id);
src/AURA.Agents/AgentManager.cs:152:            await WaitFinishedAsync(runtime, cell);
src/AURA.Agents/AgentManager.cs:154:            string log = runtime.ReadCellLog(cell.Id);
src/AURA.Agents/AgentManager.cs:175:        public Cell StartAssistantCell(SimulationRuntime runtime, string id, string assistantName = "aichat")
src/AURA.Agents/AgentManager.cs:177:            if (runtime == null)
src/AURA.Agents/AgentManager.cs:179:                throw new ArgumentNullException(nameof(runtime));
src/AURA.Agents/AgentManager.cs:198:            Cell cell = runtime.CreateCell(id, definition.FileName,
src/AURA.Agents/AgentManager.cs:205:        private static async Task WaitFinishedAsync(SimulationRuntime runtime, Cell cell)
src/AURA.Agents/AgentManager.cs:8:using AURA.Core.Runtime;
src/AURA.CLI/Program.cs:12:using AURA.Core.Runtime;
src/AURA.CLI/Program.cs:13:using AURA.Modules;
src/AURA.CLI/Program.cs:141:                        PrintModules();
src/AURA.CLI/Program.cs:149:                    case "plugins":
src/AURA.CLI/Program.cs:14:using AURA.Modules.Executors;
src/AURA.CLI/Program.cs:150:                        PrintPlugins();
src/AURA.CLI/Program.cs:179:                        Console.WriteLine("Células persistidas em: " + _runtime.PersistNow());
src/AURA.CLI/Program.cs:251:            Cell cell = _runner.RunAsync(_runtime, cellId, filePath, arguments, null, limits.IsEmpty ? null : limits).GetAwaiter().GetResult();
src/AURA.CLI/Program.cs:261:                _runtime.WaitCellAsync(cell.Id).GetAwaiter().GetResult();
src/AURA.CLI/Program.cs:264:                Console.WriteLine(_runtime.ReadCellLog(cell.Id));
src/AURA.CLI/Program.cs:26:        private static SimulationRuntime _runtime;
src/AURA.CLI/Program.cs:285:            Cell cell = _agentManager.StartAssistantCell(_runtime, cellId, assistant);
src/AURA.CLI/Program.cs:28:        private static PluginWatcher _pluginWatcher;
src/AURA.CLI/Program.cs:588:            string answer = _agentManager.AskAsync(_runtime, question, assistant, cellId).GetAwaiter().GetResult();
src/AURA.CLI/Program.cs:59:            _runtime = new SimulationRuntime(_logger);
src/AURA.CLI/Program.cs:60:            _runtime.Events = bootstrap.Events;
src/AURA.CLI/Program.cs:61:            _pluginWatcher = new PluginWatcher(_logger);
src/AURA.CLI/Program.cs:62:            _runner = new Runner(_pluginWatcher.Launchers.Concat(
src/AURA.CLI/Program.cs:631:            Cell[] cells = _runtime.Cells.ToArray();
src/AURA.CLI/Program.cs:639:            Console.WriteLine("Células (" + _runtime.CellsRoot + "):");
src/AURA.CLI/Program.cs:666:                    _runtime.StartCellAsync(id).GetAwaiter().GetResult();
src/AURA.CLI/Program.cs:670:                    _runtime.StopCell(id);
src/AURA.CLI/Program.cs:673:                    _runtime.PauseCell(id);
src/AURA.CLI/Program.cs:676:                    _runtime.ResumeCell(id);
src/AURA.CLI/Program.cs:679:                    _runtime.DeleteCell(id);
src/AURA.CLI/Program.cs:682:                    Console.WriteLine(_runtime.ReadCellLog(id));
src/AURA.CLI/Program.cs:706:            _runtime.SetCellLimits(id, limits);
src/AURA.CLI/Program.cs:720:        private static void PrintPlugins()
src/AURA.CLI/Program.cs:722:            Console.WriteLine("Plugins (" + _pluginWatcher.PluginsRoot + "):");
src/AURA.CLI/Program.cs:723:            string[] paths = _pluginWatcher.PluginPaths.ToArray();
src/AURA.CLI/Program.cs:726:                Console.WriteLine("  (nenhum plugin .dll encontrado)");
src/AURA.CLI/Program.cs:72:            _runtime.LoadFromStoreAsync().GetAwaiter().GetResult();
src/AURA.CLI/Program.cs:735:            Console.WriteLine("Launchers de plugins : " + _pluginWatcher.Launchers.Count);
src/AURA.CLI/Program.cs:736:            Console.WriteLine("Plugins IPlugin      : " + _pluginWatcher.Plugins.Count);
src/AURA.CLI/Program.cs:773:            Console.WriteLine("Módulos (" + _bootstrap.ModulesPath + "):");
src/AURA.CLI/Program.cs:774:            foreach (ModuleInfo m in ModuleCatalog.GetAll())
src/AURA.CLI/Program.cs:778:                    : _bootstrap.Modules.Modules.IsEnabled(m.Id) ? "aplicado" : "não aplicado";
src/AURA.CLI/Program.cs:783:        private static void PrintModules()
src/AURA.CLI/Program.cs:785:            foreach (ModuleInfo module in ModuleCatalog.GetAll())
src/AURA.CLI/Program.cs:787:                string kind = module.IsCore
src/AURA.CLI/Program.cs:789:                    : string.IsNullOrWhiteSpace(module.PackageUrl) ? "planejado" : "baixável";
src/AURA.CLI/Program.cs:790:                Console.WriteLine(module.Icon + " " + module.DisplayName +
src/AURA.CLI/Program.cs:791:                    " [" + module.Status + ", " + kind + "] - " + module.ShortDescription);
src/AURA.CLI/Program.cs:816:            Console.WriteLine("  plugins                 Lista plugins carregados");
src/AURA.CLI/Program.cs:84:            _pluginWatcher.Dispose();
src/AURA.CLI/Program.cs:85:            _runtime.Dispose();
src/AURA.Core/Abstractions/ICommand.cs:5:    /// by the automation/plugin systems).
src/AURA.Core/Abstractions/IModule.cs:10:    public interface IModule
src/AURA.Core/Abstractions/IModule.cs:5:    /// modules (Windows Assistant, AI, Automation, Memory, Plugins, ...).
src/AURA.Core/Abstractions/IModule.cs:7:    /// Genesis Core MVP - actual module implementations arrive in later
src/AURA.Core/Abstractions/IPlugin.cs:4:    /// Represents an externally loaded plugin. Reserved for the future
src/AURA.Core/Abstractions/IPlugin.cs:5:    /// AURA.Plugins module.
src/AURA.Core/Abstractions/IPlugin.cs:7:    public interface IPlugin
src/AURA.Core/Bootstrap/AuraBootstrap.cs:12:    /// before it can start. Front-ends load their own modules afterwards.
src/AURA.Core/Bootstrap/AuraBootstrap.cs:24:        public ModulesConfiguration Modules { get; private set; }
src/AURA.Core/Bootstrap/AuraBootstrap.cs:28:        public string ModulesPath { get; private set; }
src/AURA.Core/Bootstrap/AuraBootstrap.cs:43:            ModulesPath = System.IO.Path.Combine(baseDirectory, "config", "modules.json");
src/AURA.Core/Bootstrap/AuraBootstrap.cs:61:            Modules = configLoader.LoadModules(ModulesPath);
src/AURA.Core/Bootstrap/AuraBootstrap.cs:68:        public void SaveModules()
src/AURA.Core/Bootstrap/AuraBootstrap.cs:71:            configLoader.SaveModules(ModulesPath, Modules);
src/AURA.Core/Configuration/ConfigLoader.cs:10:    /// (config/settings.json and config/modules.json). Uses System.Text.Json
src/AURA.Core/Configuration/ConfigLoader.cs:11:    /// (part of the .NET runtime) so the project has zero third-party NuGet
src/AURA.Core/Configuration/ConfigLoader.cs:47:        public ModulesConfiguration LoadModules(string path)
src/AURA.Core/Configuration/ConfigLoader.cs:49:            ModulesConfiguration config = Load<ModulesConfiguration>(path);
src/AURA.Core/Configuration/ConfigLoader.cs:53:                config = new ModulesConfiguration();
src/AURA.Core/Configuration/ConfigLoader.cs:54:                SaveModules(path, config);
src/AURA.Core/Configuration/ConfigLoader.cs:60:        public void SaveModules(string path, ModulesConfiguration config)
src/AURA.Core/Configuration/ModulesConfiguration.cs:11:        public ModuleFlags Modules { get; set; }
src/AURA.Core/Configuration/ModulesConfiguration.cs:13:        public ModulesConfiguration()
src/AURA.Core/Configuration/ModulesConfiguration.cs:15:            Modules = new ModuleFlags();
src/AURA.Core/Configuration/ModulesConfiguration.cs:20:    /// Flags persistentes dos módulos. Módulos do núcleo (browser/modules) não
src/AURA.Core/Configuration/ModulesConfiguration.cs:23:    public class ModuleFlags
src/AURA.Core/Configuration/ModulesConfiguration.cs:37:        public bool Plugins { get; set; }
src/AURA.Core/Configuration/ModulesConfiguration.cs:57:                case "plugins": return Plugins;
src/AURA.Core/Configuration/ModulesConfiguration.cs:6:    /// Tracks which optional capability modules the user has chosen to
src/AURA.Core/Configuration/ModulesConfiguration.cs:7:    /// download and apply, persisted to config/modules.json.
src/AURA.Core/Configuration/ModulesConfiguration.cs:80:                case "plugins": Plugins = value; break;
src/AURA.Core/Configuration/ModulesConfiguration.cs:9:    public class ModulesConfiguration
src/AURA.Core/Events/AuraEvents.cs:58:    public sealed class ModuleStateChangedEvent : IEvent
src/AURA.Core/Events/AuraEvents.cs:60:        public string ModuleId { get; set; }
src/AURA.Core/Launchers/CellCommand.cs:8:    /// arguments, ready to be passed to SimulationRuntime.CreateCell.
src/AURA.Core/Launchers/ILauncher.cs:1:using AURA.Core.Runtime;
src/AURA.Core/Launchers/NodeLauncher.cs:7:    /// Runs Node.js files (.js, .mjs) inside a cell via the "node" runtime.
src/AURA.Core/Launchers/Runner.cs:12:    /// The user never deals with interpreters or runtimes directly.
src/AURA.Core/Launchers/Runner.cs:5:using AURA.Core.Runtime;
src/AURA.Core/Launchers/Runner.cs:62:        /// starts it inside a brand-new cell owned by the runtime.
src/AURA.Core/Launchers/Runner.cs:64:        public async System.Threading.Tasks.Task<Cell> RunAsync(SimulationRuntime runtime, string id, string filePath,
src/AURA.Core/Launchers/Runner.cs:67:            if (runtime == null)
src/AURA.Core/Launchers/Runner.cs:69:                throw new ArgumentNullException(nameof(runtime));
src/AURA.Core/Launchers/Runner.cs:88:            Cell cell = runtime.CreateCell(id, command.FileName, command.Arguments,
src/AURA.Core/Launchers/Runner.cs:91:            await runtime.StartCellAsync(cell.Id);
src/AURA.Core/Logging/ILogger.cs:4:    /// Minimal logging abstraction used across all AURA modules.
src/AURA.Core/Runtime/Cell.cs:36:        /// Root real da célula, definido pelo runtime no momento da criação
src/AURA.Core/Runtime/Cell.cs:38:        /// mesmo quando o runtime usa um root customizado (ex.: Android).
src/AURA.Core/Runtime/Cell.cs:46:            ? Path.Combine(SimulationRuntime.ExpandUserHome(SimulationRuntime.DefaultCellsRoot), Id)
src/AURA.Core/Runtime/Cell.cs:4:namespace AURA.Core.Runtime
src/AURA.Core/Runtime/CellState.cs:1:namespace AURA.Core.Runtime
src/AURA.Core/Runtime/CellStore.cs:10:    /// survive AURA restarts. On boot the runtime loads the index back and
src/AURA.Core/Runtime/CellStore.cs:11:    /// recovers live processes (see SimulationRuntime.LoadFromStoreAsync).
src/AURA.Core/Runtime/CellStore.cs:28:            _path = path ?? SimulationRuntime.ExpandUserHome("~/AURA/cells.json");
src/AURA.Core/Runtime/CellStore.cs:33:        /// <summary>Saves all runtime cells to disk (atomic replace).</summary>
src/AURA.Core/Runtime/CellStore.cs:34:        public void Save(SimulationRuntime runtime)
src/AURA.Core/Runtime/CellStore.cs:48:                        Cells = new System.Collections.Generic.List<Cell>(runtime.Cells),
src/AURA.Core/Runtime/CellStore.cs:6:namespace AURA.Core.Runtime
src/AURA.Core/Runtime/DirectoryCellBackend.cs:4:namespace AURA.Core.Runtime
src/AURA.Core/Runtime/ICellBackend.cs:1:namespace AURA.Core.Runtime
src/AURA.Core/Runtime/ICellBackend.cs:7:    /// rest of the runtime.
src/AURA.Core/Runtime/PluginWatcher.cs:101:                    _logger.Warning("Falha ao recarregar plugins: " + ex.Message);
src/AURA.Core/Runtime/PluginWatcher.cs:115:        private void TryLoadPlugin(string dllPath)
src/AURA.Core/Runtime/PluginWatcher.cs:119:                Assembly assembly = _context.LoadFromAssemblyPath(dllPath);
src/AURA.Core/Runtime/PluginWatcher.cs:121:                Type[] launcherTypes = assembly.GetTypes()
src/AURA.Core/Runtime/PluginWatcher.cs:126:                Type[] pluginTypes = assembly.GetTypes()
src/AURA.Core/Runtime/PluginWatcher.cs:128:                        && typeof(IPlugin).IsAssignableFrom(t))
src/AURA.Core/Runtime/PluginWatcher.cs:12:namespace AURA.Core.Runtime
src/AURA.Core/Runtime/PluginWatcher.cs:131:                if (launcherTypes.Length == 0 && pluginTypes.Length == 0)
src/AURA.Core/Runtime/PluginWatcher.cs:133:                    _logger.Warning("Plugin sem tipos conhecidos ignorado: " + Path.GetFileName(dllPath));
src/AURA.Core/Runtime/PluginWatcher.cs:143:                foreach (Type type in pluginTypes)
src/AURA.Core/Runtime/PluginWatcher.cs:145:                    IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
src/AURA.Core/Runtime/PluginWatcher.cs:146:                    plugin.Load();
src/AURA.Core/Runtime/PluginWatcher.cs:147:                    _plugins.Add(plugin);
src/AURA.Core/Runtime/PluginWatcher.cs:150:                _pluginPaths.Add(dllPath);
src/AURA.Core/Runtime/PluginWatcher.cs:154:                _logger.Warning("Plugin inválido: " + Path.GetFileName(dllPath) + " -> " + ex.Message);
src/AURA.Core/Runtime/PluginWatcher.cs:15:    /// Loads external assemblies ("plugins") from a plugins directory into a
src/AURA.Core/Runtime/PluginWatcher.cs:160:            foreach (IPlugin plugin in _plugins)
src/AURA.Core/Runtime/PluginWatcher.cs:164:                    plugin.Unload();
src/AURA.Core/Runtime/PluginWatcher.cs:168:                    _logger.Warning("Falha ao descarregar plugin: " + ex.Message);
src/AURA.Core/Runtime/PluginWatcher.cs:16:    /// collectible AssemblyLoadContext and watches that directory for changes.
src/AURA.Core/Runtime/PluginWatcher.cs:172:            _plugins.Clear();
src/AURA.Core/Runtime/PluginWatcher.cs:173:            _pluginPaths.Clear();
src/AURA.Core/Runtime/PluginWatcher.cs:186:                _logger.Warning("Falha ao descarregar contexto de plugins: " + ex.Message);
src/AURA.Core/Runtime/PluginWatcher.cs:18:    /// and the plugins are reloaded, enabling hot-reload of launchers and other
src/AURA.Core/Runtime/PluginWatcher.cs:21:    public sealed class PluginWatcher : IDisposable
src/AURA.Core/Runtime/PluginWatcher.cs:220:        /// Collectible load context so plugin assemblies can be released on
src/AURA.Core/Runtime/PluginWatcher.cs:221:        /// reload. Plugin dependencies are resolved from the plugins directory
src/AURA.Core/Runtime/PluginWatcher.cs:224:        private sealed class PluginLoadContext : AssemblyLoadContext
src/AURA.Core/Runtime/PluginWatcher.cs:226:            private readonly string _pluginsRoot;
src/AURA.Core/Runtime/PluginWatcher.cs:228:            public PluginLoadContext(string pluginsRoot)
src/AURA.Core/Runtime/PluginWatcher.cs:229:                : base("AURA.Plugins." + Guid.NewGuid().ToString("N"), isCollectible: true)
src/AURA.Core/Runtime/PluginWatcher.cs:231:                _pluginsRoot = pluginsRoot;
src/AURA.Core/Runtime/PluginWatcher.cs:234:            protected override Assembly Load(AssemblyName assemblyName)
src/AURA.Core/Runtime/PluginWatcher.cs:236:                if (assemblyName.Name == "AURA.Core")
src/AURA.Core/Runtime/PluginWatcher.cs:238:                    return typeof(ILauncher).Assembly;
src/AURA.Core/Runtime/PluginWatcher.cs:241:                string candidate = Path.Combine(_pluginsRoot, assemblyName.Name + ".dll");
src/AURA.Core/Runtime/PluginWatcher.cs:244:                    return LoadFromAssemblyPath(candidate);
src/AURA.Core/Runtime/PluginWatcher.cs:24:        private readonly string _pluginsRoot;
src/AURA.Core/Runtime/PluginWatcher.cs:27:        private readonly List<string> _pluginPaths = new List<string>();
src/AURA.Core/Runtime/PluginWatcher.cs:29:        private PluginLoadContext _context;
src/AURA.Core/Runtime/PluginWatcher.cs:31:        private List<IPlugin> _plugins = new List<IPlugin>();
src/AURA.Core/Runtime/PluginWatcher.cs:33:        public PluginWatcher(ILogger logger, string pluginsRoot = null)
src/AURA.Core/Runtime/PluginWatcher.cs:36:            _pluginsRoot = string.IsNullOrWhiteSpace(pluginsRoot)
src/AURA.Core/Runtime/PluginWatcher.cs:37:                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AURA", "plugins")
src/AURA.Core/Runtime/PluginWatcher.cs:38:                : ExpandHome(pluginsRoot);
src/AURA.Core/Runtime/PluginWatcher.cs:40:            Directory.CreateDirectory(_pluginsRoot);
src/AURA.Core/Runtime/PluginWatcher.cs:42:            _watcher = new FileSystemWatcher(_pluginsRoot)
src/AURA.Core/Runtime/PluginWatcher.cs:56:        /// <summary>Plugins root directory (created on demand).</summary>
src/AURA.Core/Runtime/PluginWatcher.cs:57:        public string PluginsRoot => _pluginsRoot;
src/AURA.Core/Runtime/PluginWatcher.cs:59:        /// <summary>Launchers discovered in the current plugins set.</summary>
src/AURA.Core/Runtime/PluginWatcher.cs:62:        /// <summary>Plugins implementing <see cref="IPlugin"/> discovered in the current set.</summary>
src/AURA.Core/Runtime/PluginWatcher.cs:63:        public IReadOnlyList<IPlugin> Plugins => _plugins;
src/AURA.Core/Runtime/PluginWatcher.cs:65:        /// <summary>Full paths of the plugin assemblies currently loaded.</summary>
src/AURA.Core/Runtime/PluginWatcher.cs:66:        public IReadOnlyList<string> PluginPaths => _pluginPaths;
src/AURA.Core/Runtime/PluginWatcher.cs:69:        /// Does an initial (re)load of all plugins. Call after plugin files are
src/AURA.Core/Runtime/PluginWatcher.cs:6:using System.Runtime.Loader;
src/AURA.Core/Runtime/PluginWatcher.cs:82:                    _plugins = new List<IPlugin>();
src/AURA.Core/Runtime/PluginWatcher.cs:84:                    string[] dlls = Directory.GetFiles(_pluginsRoot, "*.dll");
src/AURA.Core/Runtime/PluginWatcher.cs:90:                    _context = new PluginLoadContext(_pluginsRoot);
src/AURA.Core/Runtime/PluginWatcher.cs:93:                        TryLoadPlugin(dll);
src/AURA.Core/Runtime/PluginWatcher.cs:96:                    _logger.Info("Plugins carregados: " + string.Join(", ", _pluginPaths) +
src/AURA.Core/Runtime/ResourceLimits.cs:3:namespace AURA.Core.Runtime
src/AURA.Core/Runtime/SimulationRuntime.cs:13:namespace AURA.Core.Runtime
src/AURA.Core/Runtime/SimulationRuntime.cs:16:    /// The cell runtime (formerly "SimulationRuntime"). Each cell is backed
src/AURA.Core/Runtime/SimulationRuntime.cs:21:    public sealed class SimulationRuntime : IDisposable
src/AURA.Core/Runtime/SimulationRuntime.cs:252:                _logger.Warning("Pausa nativa não disponível no Windows neste runtime.");
src/AURA.Core/Runtime/SimulationRuntime.cs:283:                _logger.Warning("Retomada nativa não disponível no Windows neste runtime.");
src/AURA.Core/Runtime/SimulationRuntime.cs:355:        public async Task LoadFromStoreAsync()
src/AURA.Core/Runtime/SimulationRuntime.cs:42:        public SimulationRuntime(ILogger logger)
src/AURA.Core/Runtime/SimulationRuntime.cs:471:                throw new InvalidOperationException("Persistência desabilitada neste runtime.");
src/AURA.Core/Runtime/SimulationRuntime.cs:47:        public SimulationRuntime(ILogger logger, string cellsRoot, ICellBackend backend)
src/AURA.Core/Runtime/SimulationRuntime.cs:52:        public SimulationRuntime(ILogger logger, string cellsRoot, ICellBackend backend, bool persist)
src/AURA.Core/Runtime/SimulationRuntime.cs:7:using System.Runtime.InteropServices;
src/AURA.Core/Runtime/SimulationRuntime.cs:83:        /// EventBus opcional. Quando definido, o runtime publica
src/AURA.Installer/ArtifactAnalysisService.cs:11:    /// <summary>Null quando ainda não existe IDependencyAnalyzer registrado para o tipo identificado.</summary>
src/AURA.Installer/ArtifactAnalysisService.cs:27:    private readonly IReadOnlyDictionary<ArtifactType, IDependencyAnalyzer> _analyzers;
src/AURA.Installer/ArtifactAnalysisService.cs:32:        IEnumerable<IDependencyAnalyzer> analyzers,
src/AURA.Installer/ArtifactAnalysisService.cs:45:            new IDependencyAnalyzer[] { new PythonDependencyAnalyzer() });
src/AURA.Installer/ArtifactType.cs:13:    DotNetAssembly
src/AURA.Installer/EnvironmentSelectionResult.cs:14:    public bool RuntimeAvailable { get; set; }
src/AURA.Installer/EnvironmentSelectionResult.cs:16:    /// <summary>Nome do binário do runtime resolvido (ex.: "python3"), ou null se não encontrado.</summary>
src/AURA.Installer/EnvironmentSelectionResult.cs:17:    public string? RuntimeBinary { get; set; }
src/AURA.Installer/EnvironmentSelectionResult.cs:19:    /// <summary>Comandos sugeridos pra instalar o runtime, adequados ao ambiente detectado (Termux/Linux/Windows/macOS).</summary>
src/AURA.Installer/EnvironmentSelectionResult.cs:20:    public List<string> InstallRuntimeSuggestions { get; set; } = new();
src/AURA.Installer/EnvironmentSelectionResult.cs:32:    public bool ReadyToInstall => RuntimeAvailable && HasEnoughDiskSpace;
src/AURA.Installer/EnvironmentSelectionResult.cs:7:/// Inteligente: o runtime necessário já está disponível? E o disco aguenta
src/AURA.Installer/FileIdentifier.cs:32:        // 1) Assinatura PE ("MZ") -> DLL/assembly .NET.
src/AURA.Installer/FileIdentifier.cs:39:                Type = ArtifactType.DotNetAssembly,
src/AURA.Installer/IDependencyAnalyzer.cs:5:/// ele precisa para rodar (pacotes, runtimes). Uma implementação por
src/AURA.Installer/IDependencyAnalyzer.cs:8:public interface IDependencyAnalyzer
src/AURA.Installer/IEnvironmentSelector.cs:5:/// se o ambiente atual tem o runtime necessário e recursos suficientes pra
src/AURA.Installer/PythonDependencyAnalyzer.cs:100:                    modules.Add(RootOf(mod));
src/AURA.Installer/PythonDependencyAnalyzer.cs:105:                string fromModule = match.Groups["from"].Value;
src/AURA.Installer/PythonDependencyAnalyzer.cs:106:                if (fromModule.StartsWith('.'))
src/AURA.Installer/PythonDependencyAnalyzer.cs:110:                modules.Add(RootOf(fromModule));
src/AURA.Installer/PythonDependencyAnalyzer.cs:114:        return modules.Distinct(StringComparer.Ordinal).ToList();
src/AURA.Installer/PythonDependencyAnalyzer.cs:117:    private static string RootOf(string dottedModule)
src/AURA.Installer/PythonDependencyAnalyzer.cs:119:        int dot = dottedModule.IndexOf('.');
src/AURA.Installer/PythonDependencyAnalyzer.cs:120:        return dot < 0 ? dottedModule : dottedModule[..dot];
src/AURA.Installer/PythonDependencyAnalyzer.cs:12:public sealed class PythonDependencyAnalyzer : IDependencyAnalyzer
src/AURA.Installer/PythonDependencyAnalyzer.cs:57:        var rootModules = ExtractRootModules(source);
src/AURA.Installer/PythonDependencyAnalyzer.cs:59:        foreach (var module in rootModules)
src/AURA.Installer/PythonDependencyAnalyzer.cs:61:            if (PythonStdlibModules.IsStdlib(module))
src/AURA.Installer/PythonDependencyAnalyzer.cs:66:            if (KnownAliases.TryGetValue(module, out var packageName))
src/AURA.Installer/PythonDependencyAnalyzer.cs:75:                report.Dependencies.Add(module);
src/AURA.Installer/PythonDependencyAnalyzer.cs:76:                report.UnresolvedImports.Add(module);
src/AURA.Installer/PythonDependencyAnalyzer.cs:90:    private static List<string> ExtractRootModules(string source)
src/AURA.Installer/PythonDependencyAnalyzer.cs:92:        var modules = new List<string>();
src/AURA.Installer/PythonEnvironmentSelector.cs:2:using AURA.Modules.Executors;
src/AURA.Installer/PythonEnvironmentSelector.cs:41:        bool runtimeAvailable = _pythonExecutor.IsAvailable();
src/AURA.Installer/PythonEnvironmentSelector.cs:50:            RuntimeAvailable = runtimeAvailable,
src/AURA.Installer/PythonEnvironmentSelector.cs:51:            RuntimeBinary = runtimeAvailable ? _pythonExecutor.Name : null,
src/AURA.Installer/PythonEnvironmentSelector.cs:57:        if (!runtimeAvailable)
src/AURA.Installer/PythonEnvironmentSelector.cs:59:            result.InstallRuntimeSuggestions.AddRange(SuggestPythonInstallCommands());
src/AURA.Installer/PythonEnvironmentSelector.cs:9:/// pra checar o runtime e o <see cref="SystemAnalyzer"/> pra checar disco livre.
src/AURA.Installer/PythonInstaller.cs:2:using AURA.Modules.Executors;
src/AURA.Installer/PythonStdlibModules.cs:10:public static class PythonStdlibModules
src/AURA.Installer/PythonStdlibModules.cs:31:    public static bool IsStdlib(string rootModuleName) => Names.Contains(rootModuleName);
src/AURA.Memory/MemoryStore.cs:35:            _path = path ?? SimulationRuntime.ExpandUserHome("~/AURA/memory.json");
src/AURA.Memory/MemoryStore.cs:6:using AURA.Core.Runtime;
src/AURA.Memory/SolutionStore.cs:38:                SimulationRuntime.ExpandUserHome(
src/AURA.Memory/SolutionStore.cs:7:using AURA.Core.Runtime;
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:10:    public static class RuntimeConfig
src/AURA.Mobile/MainPage.cs:10:        private readonly List<(string? ModuleId, string Section, string Label, Page Page)> _entries;
src/AURA.Mobile/MainPage.cs:15:            ModuleManager manager,
src/AURA.Mobile/MainPage.cs:21:            ModulesPage modules,
src/AURA.Mobile/MainPage.cs:32:            events.Subscribe<ModuleStateChangedEvent>(_ =>
src/AURA.Mobile/MainPage.cs:3:using AURA.Modules;
src/AURA.Mobile/MainPage.cs:44:                (null, "Ferramentas", "Módulos", modules),
src/AURA.Mobile/MainPage.cs:90:            foreach (IGrouping<string, (string ModuleId, string Section, string Label, Page Page)> group
src/AURA.Mobile/MainPage.cs:94:                    .Where(e => e.ModuleId == null || _manager.IsApplied(e.ModuleId))
src/AURA.Mobile/MainPage.cs:9:        private readonly ModuleManager _manager;
src/AURA.Mobile/MauiProgram.cs:100:        builder.Services.AddSingleton<ModulesPage>();
src/AURA.Mobile/MauiProgram.cs:10:using AURA.Modules;
src/AURA.Mobile/MauiProgram.cs:11:using AURA.Modules.Executors;
src/AURA.Mobile/MauiProgram.cs:39:        // Configuração persistida (settings.json/modules.json na pasta privada do app).
src/AURA.Mobile/MauiProgram.cs:44:            .LoadModules(Path.Combine(configDir, "modules.json")));
src/AURA.Mobile/MauiProgram.cs:47:        // modules.json) e remove (desativa + limpa dados locais).
src/AURA.Mobile/MauiProgram.cs:48:        builder.Services.AddSingleton(sp => new ModuleManager(
src/AURA.Mobile/MauiProgram.cs:50:            Path.Combine(FileSystem.AppDataDirectory, "modules"),
src/AURA.Mobile/MauiProgram.cs:51:            Path.Combine(configDir, "modules.json"),
src/AURA.Mobile/MauiProgram.cs:7:using AURA.Core.Runtime;
src/AURA.Mobile/MauiProgram.cs:82:        // Runtime de células + runner ("AURA decide como rodar"), mesmo core do CLI.
src/AURA.Mobile/MauiProgram.cs:84:        builder.Services.AddSingleton(sp => new SimulationRuntime(
src/AURA.Mobile/Pages/AgentPage.xaml.cs:20:        RuntimeConfig.Apply(_client);
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:3:using AURA.Core.Runtime;
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:40:            "window.chrome=window.chrome||{loadTimes:function(){return{}},csi:function(){return{}},runtime:{}};" +
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:42:            "if(navigator.plugins&&navigator.plugins.length===0){" +
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:43:            "Object.defineProperty(navigator,'plugins',{get:function(){return [" +
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:44:            "{name:'Chrome PDF Plugin',filename:'internal-pdf-viewer',description:'Portable Document Format'}," +
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:460:                _runtime.CreateCell(id, "com.aura.webview", "browser-isolado");
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:46:            "{name:'Native Client',filename:'internal-nacl-plugin',description:''}]}});" +
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:491:                _runtime.DeleteCell(id);
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:52:        private readonly SimulationRuntime _runtime;
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:60:        public BrowserPage(ImageSearchPage imageSearch, SimulationRuntime runtime, EventBus events)
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:64:            _runtime = runtime;
src/AURA.Mobile/Pages/CellsPage.xaml.cs:108:        string log = _runtime.ReadCellLog(cell.Id, 300);
src/AURA.Mobile/Pages/CellsPage.xaml.cs:130:        _runtime.DeleteCell(cell.Id);
src/AURA.Mobile/Pages/CellsPage.xaml.cs:14:    public CellsPage(SimulationRuntime runtime, Runner runner, RunPage runPage)
src/AURA.Mobile/Pages/CellsPage.xaml.cs:17:        _runtime = runtime;
src/AURA.Mobile/Pages/CellsPage.xaml.cs:2:using AURA.Core.Runtime;
src/AURA.Mobile/Pages/CellsPage.xaml.cs:31:                await _runtime.LoadFromStoreAsync();
src/AURA.Mobile/Pages/CellsPage.xaml.cs:35:                AuraLog.Exception("CellsPage.LoadFromStore", ex);
src/AURA.Mobile/Pages/CellsPage.xaml.cs:3:using Cell = AURA.Core.Runtime.Cell;
src/AURA.Mobile/Pages/CellsPage.xaml.cs:44:        CellsView.ItemsSource = _runtime.Cells
src/AURA.Mobile/Pages/CellsPage.xaml.cs:58:            await _runtime.StartCellAsync(cell.Id);
src/AURA.Mobile/Pages/CellsPage.xaml.cs:75:        _runtime.StopCell(cell.Id);
src/AURA.Mobile/Pages/CellsPage.xaml.cs:86:        _runtime.PauseCell(cell.Id);
src/AURA.Mobile/Pages/CellsPage.xaml.cs:97:        _runtime.ResumeCell(cell.Id);
src/AURA.Mobile/Pages/CellsPage.xaml.cs:9:    private readonly SimulationRuntime _runtime;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:100:        _client.Options.TimeoutSeconds = RuntimeConfig.TimeoutSeconds;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:101:        _client.Options.ApiKey = RuntimeConfig.ApiKey;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:132:        RuntimeConfig.ApiKey = apiKey;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:137:            RuntimeConfig.Model = pm.Id;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:143:            RuntimeConfig.Provider = pi.Name;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:22:        RuntimeConfig.Apply(_client);
src/AURA.Mobile/Pages/ChatPage.xaml.cs:24:        string savedProvider = RuntimeConfig.Provider;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:25:        string savedModel = RuntimeConfig.Model;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:26:        ApiKeyEntry.Text = RuntimeConfig.ApiKey;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:99:        _client.Options.MaxTokens = RuntimeConfig.MaxTokens;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:3:using AURA.Modules.Executors;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:110:            $"Provedor: {RuntimeConfig.Provider}\n" +
src/AURA.Mobile/Pages/FixesPage.xaml.cs:114:            $"log_lines: {RuntimeConfig.LogLinesForAnalysis}\n" +
src/AURA.Mobile/Pages/FixesPage.xaml.cs:136:                        RuntimeConfig.Model = fix.Suggested;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:140:                        RuntimeConfig.Provider = fix.Suggested;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:145:                            RuntimeConfig.MaxTokens = tokens;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:152:                            RuntimeConfig.TimeoutSeconds = to;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:159:                            RuntimeConfig.LogLinesForAnalysis = lines;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:178:        RuntimeConfig.Apply(_client);
src/AURA.Mobile/Pages/FixesPage.xaml.cs:200:        RuntimeConfig.Apply(_client);
src/AURA.Mobile/Pages/FixesPage.xaml.cs:20:        RuntimeConfig.Apply(_client);
src/AURA.Mobile/Pages/FixesPage.xaml.cs:28:            $"Provedor: {RuntimeConfig.Provider} ({(RuntimeConfig.Provider.Length == 0 ? "padrão" : RuntimeConfig.Provider)})\n" +
src/AURA.Mobile/Pages/FixesPage.xaml.cs:32:            $"linhas de log analisadas: {RuntimeConfig.LogLinesForAnalysis}\n" +
src/AURA.Mobile/Pages/FixesPage.xaml.cs:53:        string log = AuraLog.ReadRecentLog(RuntimeConfig.LogLinesForAnalysis);
src/AURA.Mobile/Pages/LogsPage.xaml.cs:159:        RuntimeConfig.Apply(_client);
src/AURA.Mobile/Pages/LogsPage.xaml.cs:167:        string logContent = AuraLog.ReadRecentLog(RuntimeConfig.LogLinesForAnalysis);
src/AURA.Mobile/Pages/LogsPage.xaml.cs:21:        RuntimeConfig.Apply(_client);
src/AURA.Mobile/Pages/LogsPage.xaml.cs:76:        RuntimeConfig.Apply(_client);
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:107:            var rows = ModuleCatalog.GetAll().Select(m =>
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:111:                    return new ModuleRow { Module = m, StateText = "Núcleo (sempre ativo)" };
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:116:                    return new ModuleRow { Module = m, StateText = "Em breve" };
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:11:        public ModulesPage(ModuleManager manager)
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:121:                    return new ModuleRow { Module = m, StateText = "Aplicado", ActionText = "Remover", ShowAction = true };
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:126:                    return new ModuleRow { Module = m, StateText = "Baixado", ActionText = "Aplicar", ShowAction = true };
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:129:                return new ModuleRow { Module = m, StateText = "Disponível", ActionText = "Baixar", ShowAction = true };
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:132:            ModulesView.ItemsSource = rows;
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:154:            AuraLog.Info("ModulesPage: " + message);
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:26:            var row = (ModuleRow)button.CommandParameter;
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:2:using AURA.Modules;
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:32:                        await SetBusyAsync($"Baixando {row.Module.DisplayName}...");
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:33:                        await _manager.DownloadAsync(row.Module.Id);
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:34:                        await ShowStatus($"Módulo '{row.Module.DisplayName}' baixado. Toque em Aplicar para ativá-lo.");
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:37:                        _manager.Apply(row.Module.Id);
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:38:                        await ShowStatus($"Módulo '{row.Module.DisplayName}' aplicado.");
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:41:                        _manager.Remove(row.Module.Id);
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:42:                        await ShowStatus($"Módulo '{row.Module.DisplayName}' removido.");
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:48:                AuraLog.Exception("ModulesPage.Action " + row.Module.Id, ex);
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:59:            var pendentes = ModuleCatalog.GetDownloadable().Where(m => !_manager.IsDownloaded(m.Id)).ToList();
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:68:            foreach (ModuleInfo m in pendentes)
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:77:                    AuraLog.Exception("ModulesPage.DownloadAll " + m.Id, ex);
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:7:    public partial class ModulesPage : ContentPage
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:87:            var baixados = ModuleCatalog.GetDownloadable()
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:96:            foreach (ModuleInfo m in baixados)
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:9:        private readonly ModuleManager _manager;
src/AURA.Mobile/Pages/RunPage.xaml.cs:101:                await _runtime.StartCellAsync(cell.Id);
src/AURA.Mobile/Pages/RunPage.xaml.cs:105:                cell = await _runner.RunAsync(_runtime, id, _filePath!, args,
src/AURA.Mobile/Pages/RunPage.xaml.cs:13:    public RunPage(SimulationRuntime runtime, Runner runner)
src/AURA.Mobile/Pages/RunPage.xaml.cs:16:        _runtime = runtime;
src/AURA.Mobile/Pages/RunPage.xaml.cs:2:using AURA.Core.Runtime;
src/AURA.Mobile/Pages/RunPage.xaml.cs:3:using Cell = AURA.Core.Runtime.Cell;
src/AURA.Mobile/Pages/RunPage.xaml.cs:98:                cell = _runtime.CreateCell(id, exe, args,
src/AURA.Mobile/Pages/RunPage.xaml.cs:9:    private readonly SimulationRuntime _runtime;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:2:using AURA.Modules.Executors;
src/AURA.Mobile/Platforms/Android/AuraLog.cs:166:                // chegam aqui. Guardamos o handler anterior (do runtime) para delegar.
src/AURA.Mobile/Platforms/Android/AuraLog.cs:326:                // Delega para o handler original do runtime (mono/.NET) para manter
src/AURA.Mobile/Platforms/Android/AuraLog.cs:8:using Android.Runtime;
src/AURA.Mobile/Platforms/Android/MainApplication.cs:2:using Android.Runtime;
src/AURA.Mobile/ViewModels/ModuleRow.cs:10:    public sealed class ModuleRow
src/AURA.Mobile/ViewModels/ModuleRow.cs:12:        public ModuleInfo Module { get; init; }
src/AURA.Mobile/ViewModels/ModuleRow.cs:1:using AURA.Modules;
src/AURA.Mobile/ViewModels/ModuleRow.cs:6:    /// Linha exibida na Central de Módulos: envolve o ModuleInfo do catálogo
src/AURA.Modules/Executors/GitExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/NodeExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/ProcessExecutorBase.cs:5:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/PythonExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/Executors/ShellExecutor.cs:3:namespace AURA.Modules.Executors;
src/AURA.Modules/ModuleCatalog.cs:101:                Status = ModuleStatus.Implementado
src/AURA.Modules/ModuleCatalog.cs:103:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:109:                PackageUrl = PackageBase + "/memory/module.json",
src/AURA.Modules/ModuleCatalog.cs:125:                Difficulty = ModuleDifficulty.Basico,
src/AURA.Modules/ModuleCatalog.cs:127:                Status = ModuleStatus.Implementado
src/AURA.Modules/ModuleCatalog.cs:129:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:12:    ///  3. Planned — future modules (no code/package yet).
src/AURA.Modules/ModuleCatalog.cs:135:                PackageUrl = PackageBase + "/executors/module.json",
src/AURA.Modules/ModuleCatalog.cs:150:                Difficulty = ModuleDifficulty.Intermediario,
src/AURA.Modules/ModuleCatalog.cs:152:                Status = ModuleStatus.Implementado
src/AURA.Modules/ModuleCatalog.cs:154:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:15:    public static class ModuleCatalog
src/AURA.Modules/ModuleCatalog.cs:160:                PackageUrl = PackageBase + "/terminal/module.json",
src/AURA.Modules/ModuleCatalog.cs:174:                Difficulty = ModuleDifficulty.Intermediario,
src/AURA.Modules/ModuleCatalog.cs:176:                Status = ModuleStatus.Implementado
src/AURA.Modules/ModuleCatalog.cs:178:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:184:                PackageUrl = PackageBase + "/cells/module.json",
src/AURA.Modules/ModuleCatalog.cs:188:                Includes = new List<string> { "SimulationRuntime", "Runner" },
src/AURA.Modules/ModuleCatalog.cs:18:            "https://raw.githubusercontent.com/denilsonluiz3-sys/AURA_assistente/main/modules/packages";
src/AURA.Modules/ModuleCatalog.cs:199:                Difficulty = ModuleDifficulty.Avancado,
src/AURA.Modules/ModuleCatalog.cs:201:                Status = ModuleStatus.Implementado
src/AURA.Modules/ModuleCatalog.cs:203:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:209:                PackageUrl = PackageBase + "/logs/module.json",
src/AURA.Modules/ModuleCatalog.cs:20:        private static readonly List<ModuleInfo> Modules = new List<ModuleInfo>
src/AURA.Modules/ModuleCatalog.cs:223:                Difficulty = ModuleDifficulty.Basico,
src/AURA.Modules/ModuleCatalog.cs:225:                Status = ModuleStatus.Implementado
src/AURA.Modules/ModuleCatalog.cs:229:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:23:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:247:                Difficulty = ModuleDifficulty.Avancado,
src/AURA.Modules/ModuleCatalog.cs:249:                Status = ModuleStatus.Planejado
src/AURA.Modules/ModuleCatalog.cs:251:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:269:                Difficulty = ModuleDifficulty.Intermediario,
src/AURA.Modules/ModuleCatalog.cs:271:                Status = ModuleStatus.Planejado
src/AURA.Modules/ModuleCatalog.cs:273:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:275:                Id = "plugins",
src/AURA.Modules/ModuleCatalog.cs:276:                DisplayName = "Plugins",
src/AURA.Modules/ModuleCatalog.cs:279:                Includes = new List<string> { "Carregador de plugins", "API de extensão", "Repositório" },
src/AURA.Modules/ModuleCatalog.cs:282:                    "Definir a API pública de plugins (IPlugin)",
src/AURA.Modules/ModuleCatalog.cs:284:                    "Criar um repositório local de plugins instaláveis"
src/AURA.Modules/ModuleCatalog.cs:291:                Difficulty = ModuleDifficulty.Avancado,
src/AURA.Modules/ModuleCatalog.cs:293:                Status = ModuleStatus.Planejado
src/AURA.Modules/ModuleCatalog.cs:297:        public static List<ModuleInfo> GetAll()
src/AURA.Modules/ModuleCatalog.cs:299:            return Modules.ToList();
src/AURA.Modules/ModuleCatalog.cs:303:        public static List<ModuleInfo> GetCore()
src/AURA.Modules/ModuleCatalog.cs:305:            return Modules.Where(m => m.IsCore).ToList();
src/AURA.Modules/ModuleCatalog.cs:309:        public static List<ModuleInfo> GetDownloadable()
src/AURA.Modules/ModuleCatalog.cs:311:            return Modules.Where(m => !m.IsCore && !string.IsNullOrWhiteSpace(m.PackageUrl)).ToList();
src/AURA.Modules/ModuleCatalog.cs:314:        public static ModuleInfo GetById(string id)
src/AURA.Modules/ModuleCatalog.cs:321:            return Modules.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
src/AURA.Modules/ModuleCatalog.cs:32:                Difficulty = ModuleDifficulty.Basico,
src/AURA.Modules/ModuleCatalog.cs:34:                Status = ModuleStatus.Implementado
src/AURA.Modules/ModuleCatalog.cs:36:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:38:                Id = "modules",
src/AURA.Modules/ModuleCatalog.cs:44:                Includes = new List<string> { "ModuleManager", "ModuleCatalog" },
src/AURA.Modules/ModuleCatalog.cs:45:                Difficulty = ModuleDifficulty.Basico,
src/AURA.Modules/ModuleCatalog.cs:47:                Status = ModuleStatus.Implementado
src/AURA.Modules/ModuleCatalog.cs:51:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:57:                PackageUrl = PackageBase + "/system/module.json",
src/AURA.Modules/ModuleCatalog.cs:5:namespace AURA.Modules
src/AURA.Modules/ModuleCatalog.cs:73:                Difficulty = ModuleDifficulty.Basico,
src/AURA.Modules/ModuleCatalog.cs:75:                Status = ModuleStatus.Implementado
src/AURA.Modules/ModuleCatalog.cs:77:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:83:                PackageUrl = PackageBase + "/ai/module.json",
src/AURA.Modules/ModuleCatalog.cs:8:    /// Static catalog of AURA modules. Three groups:
src/AURA.Modules/ModuleCatalog.cs:99:                Difficulty = ModuleDifficulty.Intermediario,
src/AURA.Modules/ModuleDifficulty.cs:1:namespace AURA.Modules
src/AURA.Modules/ModuleDifficulty.cs:4:    /// Estimated implementation difficulty shown on the module detail screen.
src/AURA.Modules/ModuleDifficulty.cs:6:    public enum ModuleDifficulty
src/AURA.Modules/ModuleInfo.cs:16:    public sealed class ModuleInfo : IModule
src/AURA.Modules/ModuleInfo.cs:47:        public ModuleDifficulty Difficulty { get; set; }
src/AURA.Modules/ModuleInfo.cs:4:namespace AURA.Modules
src/AURA.Modules/ModuleInfo.cs:52:        public ModuleStatus Status { get; set; } = ModuleStatus.Planejado;
src/AURA.Modules/ModuleInfo.cs:7:    /// Describes one of AURA's capability modules shown in the
src/AURA.Modules/ModuleManager.cs:10:namespace AURA.Modules
src/AURA.Modules/ModuleManager.cs:110:            ModulesConfiguration config = LoadModules();
src/AURA.Modules/ModuleManager.cs:111:            config.Modules.SetEnabled(id, true);
src/AURA.Modules/ModuleManager.cs:112:            SaveModules(config);
src/AURA.Modules/ModuleManager.cs:114:            _events?.Publish(new ModuleStateChangedEvent { ModuleId = id, Applied = true });
src/AURA.Modules/ModuleManager.cs:120:            ModuleInfo info = ModuleCatalog.GetById(id);
src/AURA.Modules/ModuleManager.cs:131:            ModulesConfiguration config = LoadModules();
src/AURA.Modules/ModuleManager.cs:132:            config.Modules.SetEnabled(id, false);
src/AURA.Modules/ModuleManager.cs:133:            SaveModules(config);
src/AURA.Modules/ModuleManager.cs:142:            _events?.Publish(new ModuleStateChangedEvent { ModuleId = id, Applied = false });
src/AURA.Modules/ModuleManager.cs:145:        private ModulesConfiguration LoadModules()
src/AURA.Modules/ModuleManager.cs:147:            return _configLoader.LoadModules(_modulesPath);
src/AURA.Modules/ModuleManager.cs:14:    /// aplica (habilita em modules.json) e remove (desabilita + limpa os dados).
src/AURA.Modules/ModuleManager.cs:150:        private void SaveModules(ModulesConfiguration config)
src/AURA.Modules/ModuleManager.cs:152:            _configLoader.SaveModules(_modulesPath, config);
src/AURA.Modules/ModuleManager.cs:18:    public sealed class ModuleManager
src/AURA.Modules/ModuleManager.cs:22:        private readonly string _modulesPath;
src/AURA.Modules/ModuleManager.cs:27:        public ModuleManager(ILogger logger, string packagesDir, string modulesPath, EventBus events = null)
src/AURA.Modules/ModuleManager.cs:31:            _modulesPath = modulesPath;
src/AURA.Modules/ModuleManager.cs:37:        public string GetPackagePath(string id) => Path.Combine(_packagesDir, id, "module.json");
src/AURA.Modules/ModuleManager.cs:43:            ModulesConfiguration config = LoadModules();
src/AURA.Modules/ModuleManager.cs:44:            return config?.Modules != null && config.Modules.IsEnabled(id);
src/AURA.Modules/ModuleManager.cs:53:            ModuleInfo info = ModuleCatalog.GetById(id);
src/AURA.Modules/ModuleManager.cs:91:        /// <summary>Aplica (habilita) um módulo já baixado e persiste em modules.json.</summary>
src/AURA.Modules/ModuleManager.cs:94:            ModuleInfo info = ModuleCatalog.GetById(id);
src/AURA.Modules/ModuleStatus.cs:1:namespace AURA.Modules
src/AURA.Modules/ModuleStatus.cs:7:    public enum ModuleStatus
src/AURA.Modules/ModuleStatus.cs:9:        /// <summary>Código existe e está em uso (ex.: AI, Memory, Plugins).</summary>
src/AURA.Modules/Runtime/BinaryPath.cs:1:namespace AURA.Modules.Runtime;
src/AURA.Modules/Runtime/CompatibilityChecker.cs:13:    public CompatReport Check(RuntimeResolution runtime, DependencyReport deps, bool checkNetwork = false)
src/AURA.Modules/Runtime/CompatibilityChecker.cs:17:        // 1) Runtime
src/AURA.Modules/Runtime/CompatibilityChecker.cs:18:        if (runtime.Available && runtime.VersionSatisfied)
src/AURA.Modules/Runtime/CompatibilityChecker.cs:20:            report.RuntimeOk = true;
src/AURA.Modules/Runtime/CompatibilityChecker.cs:21:            report.Messages.Add($"Runtime '{runtime.Language}' OK: {runtime.Binary} {runtime.Version}");
src/AURA.Modules/Runtime/CompatibilityChecker.cs:25:            report.RuntimeOk = false;
src/AURA.Modules/Runtime/CompatibilityChecker.cs:26:            report.Messages.Add(!runtime.Available
src/AURA.Modules/Runtime/CompatibilityChecker.cs:27:                ? $"Runtime '{runtime.Language}' ausente. {runtime.Detail}"
src/AURA.Modules/Runtime/CompatibilityChecker.cs:28:                : $"Versão {runtime.Version} não satisfaz mínima {runtime.MinVersionRequired}");
src/AURA.Modules/Runtime/CompatibilityChecker.cs:2:using AURA.Abstractions.Runtime;
src/AURA.Modules/Runtime/CompatibilityChecker.cs:4:namespace AURA.Modules.Runtime;
src/AURA.Modules/Runtime/CompatibilityChecker.cs:7:/// Verifica ANTES de executar se o ambiente está pronto: runtime presente e
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:101:            if (string.IsNullOrEmpty(module) ||
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:102:                PythonStdlib.Contains(module) ||
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:103:                !seen.Add(module))
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:108:            string package = ImportToPackage.TryGetValue(module, out string? mapped)
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:110:                : module;
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:116:                RequiredBy = module,
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:12:public sealed class DependencyAnalyzer : IDependencyAnalyzer
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:3:using AURA.Abstractions.Runtime;
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:5:namespace AURA.Modules.Runtime;
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:99:            string module = (match.Groups[1].Value + match.Groups[2].Value)
src/AURA.Modules/Runtime/Installer.cs:11:public sealed class Installer : IRuntimeInstaller
src/AURA.Modules/Runtime/Installer.cs:13:    public InstallPlan BuildPlan(RuntimeResolution? runtime, DependencyReport? deps)
src/AURA.Modules/Runtime/Installer.cs:17:        // Runtime ausente → instalar primeiro (pip depende de python presente).
src/AURA.Modules/Runtime/Installer.cs:18:        if (runtime is not null && !runtime.Available && !string.IsNullOrWhiteSpace(runtime.InstallHint))
src/AURA.Modules/Runtime/Installer.cs:21:                What: $"runtime {runtime.Language}",
src/AURA.Modules/Runtime/Installer.cs:22:                Command: runtime.InstallHint,
src/AURA.Modules/Runtime/Installer.cs:23:                IsRuntime: true));
src/AURA.Modules/Runtime/Installer.cs:2:using AURA.Abstractions.Runtime;
src/AURA.Modules/Runtime/Installer.cs:35:                plan.Steps.Add(new InstallStep(What: dep.Name, Command: dep.InstallCommand, IsRuntime: false));
src/AURA.Modules/Runtime/Installer.cs:4:namespace AURA.Modules.Runtime;
src/AURA.Modules/Runtime/LanguageDetector.cs:11:public sealed class LanguageDetector : IRuntimeDetector
src/AURA.Modules/Runtime/LanguageDetector.cs:3:using AURA.Abstractions.Runtime;
src/AURA.Modules/Runtime/LanguageDetector.cs:5:namespace AURA.Modules.Runtime;
src/AURA.Modules/Runtime/LanguageDetector.cs:60:        if (RuntimeCatalog.Extensions.TryGetValue(ext, out string? byExtension))
src/AURA.Modules/Runtime/RuntimeCatalog.cs:11:public static class RuntimeCatalog
src/AURA.Modules/Runtime/RuntimeCatalog.cs:3:namespace AURA.Modules.Runtime;
src/AURA.Modules/Runtime/RuntimeCatalog.cs:43:    /// <summary>Linguagens de dados/documentos que não têm runtime executável.</summary>
src/AURA.Modules/Runtime/RuntimeCatalog.cs:44:    public static readonly HashSet<string> NonRuntimeLanguages = new()
src/AURA.Modules/Runtime/RuntimeCatalog.cs:6:/// Registro central de linguagens suportadas e seus runtimes. Equivalente a
src/AURA.Modules/Runtime/RuntimeCatalog.cs:7:/// <c>RUNTIME_DEFS</c>/<c>NON_RUNTIME_LANGUAGES</c> do protótipo Python.
src/AURA.Modules/Runtime/RuntimeManager.cs:105:        report.Compat = _compatChecker.Check(report.Runtime, report.Deps);
src/AURA.Modules/Runtime/RuntimeManager.cs:113:        report.Plan = _installer.BuildPlan(report.Runtime, report.Deps);
src/AURA.Modules/Runtime/RuntimeManager.cs:125:            // Re-resolver runtime após instalar
src/AURA.Modules/Runtime/RuntimeManager.cs:126:            report.Runtime = _resolver.Resolve(language);
src/AURA.Modules/Runtime/RuntimeManager.cs:127:            report.Compat = _compatChecker.Check(report.Runtime, report.Deps);
src/AURA.Modules/Runtime/RuntimeManager.cs:12:public sealed class RuntimeManager : IRuntimeManager
src/AURA.Modules/Runtime/RuntimeManager.cs:14:    private readonly IRuntimeDetector _detector;
src/AURA.Modules/Runtime/RuntimeManager.cs:15:    private readonly IRuntimeResolver _resolver;
src/AURA.Modules/Runtime/RuntimeManager.cs:163:        report.Runtime = _resolver.Resolve(language);
src/AURA.Modules/Runtime/RuntimeManager.cs:165:        report.Syntax = _syntaxValidator.Validate(filePath, language, report.Runtime.Binary);
src/AURA.Modules/Runtime/RuntimeManager.cs:166:        report.Compat = _compatChecker.Check(report.Runtime, report.Deps);
src/AURA.Modules/Runtime/RuntimeManager.cs:167:        report.Plan = _installer.BuildPlan(report.Runtime, report.Deps);
src/AURA.Modules/Runtime/RuntimeManager.cs:16:    private readonly IDependencyAnalyzer _analyzer;
src/AURA.Modules/Runtime/RuntimeManager.cs:182:        RuntimeResolution runtime = report.Runtime!;
src/AURA.Modules/Runtime/RuntimeManager.cs:183:        if (!runtime.Available)
src/AURA.Modules/Runtime/RuntimeManager.cs:188:                StandardError = $"Runtime '{runtime.Language}' indisponível. {runtime.Detail}",
src/AURA.Modules/Runtime/RuntimeManager.cs:192:        var executor = new RuntimeProcessExecutor(runtime);
src/AURA.Modules/Runtime/RuntimeManager.cs:198:                StandardError = $"Binário '{runtime.Binary}' não encontrado no PATH.",
src/AURA.Modules/Runtime/RuntimeManager.cs:19:    private readonly IRuntimeInstaller _installer;
src/AURA.Modules/Runtime/RuntimeManager.cs:202:        (string fileName, List<string> commandArgs) = BuildCommand(runtime, report.File, args);
src/AURA.Modules/Runtime/RuntimeManager.cs:21:    public RuntimeManager(
src/AURA.Modules/Runtime/RuntimeManager.cs:222:        RuntimeResolution runtime, string filePath, IReadOnlyList<string>? args)
src/AURA.Modules/Runtime/RuntimeManager.cs:225:        string binary = runtime.Binary ?? runtime.Language;
src/AURA.Modules/Runtime/RuntimeManager.cs:227:        switch (runtime.Language)
src/AURA.Modules/Runtime/RuntimeManager.cs:22:        IRuntimeDetector? detector = null,
src/AURA.Modules/Runtime/RuntimeManager.cs:23:        IRuntimeResolver? resolver = null,
src/AURA.Modules/Runtime/RuntimeManager.cs:24:        IDependencyAnalyzer? analyzer = null,
src/AURA.Modules/Runtime/RuntimeManager.cs:27:        IRuntimeInstaller? installer = null)
src/AURA.Modules/Runtime/RuntimeManager.cs:2:using AURA.Abstractions.Runtime;
src/AURA.Modules/Runtime/RuntimeManager.cs:30:        _resolver = resolver ?? new RuntimeResolver();
src/AURA.Modules/Runtime/RuntimeManager.cs:31:        _analyzer = analyzer ?? new DependencyAnalyzer();
src/AURA.Modules/Runtime/RuntimeManager.cs:4:namespace AURA.Modules.Runtime;
src/AURA.Modules/Runtime/RuntimeManager.cs:62:        // Linguagens sem runtime executável (dados/documentos)
src/AURA.Modules/Runtime/RuntimeManager.cs:63:        if (RuntimeCatalog.NonRuntimeLanguages.Contains(language))
src/AURA.Modules/Runtime/RuntimeManager.cs:71:        // [2] Resolução do runtime
src/AURA.Modules/Runtime/RuntimeManager.cs:72:        report.Runtime = _resolver.Resolve(language);
src/AURA.Modules/Runtime/RuntimeManager.cs:73:        report.Steps.Add("runtime");
src/AURA.Modules/Runtime/RuntimeManager.cs:74:        report.Log($"Runtime: {FirstNonEmpty(report.Runtime.Detail, report.Runtime.InstallHint, "—")}");
src/AURA.Modules/Runtime/RuntimeManager.cs:7:/// Orquestra o pipeline completo do Runtime/Installer Inteligente:
src/AURA.Modules/Runtime/RuntimeManager.cs:83:        if (report.Deps.Runtimes.Count > 0)
src/AURA.Modules/Runtime/RuntimeManager.cs:85:            report.Log("Runtimes exigidos: " + string.Join(", ", report.Deps.Runtimes.Select(d => d.Name)));
src/AURA.Modules/Runtime/RuntimeManager.cs:89:        report.Syntax = _syntaxValidator.Validate(filePath, language, report.Runtime.Binary);
src/AURA.Modules/Runtime/RuntimeManager.cs:8:/// identifica → resolve runtime → analisa deps → valida sintaxe → checa
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:12:public sealed class RuntimeProcessExecutor : ProcessExecutorBase
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:14:    private readonly RuntimeResolution _runtime;
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:17:    public RuntimeProcessExecutor(RuntimeResolution runtime)
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:19:        _runtime = runtime;
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:20:        _language = runtime.Language;
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:23:    public override string Name => "runtime-" + _language;
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:25:    public override bool IsAvailable() => _runtime.Available;
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:2:using AURA.Abstractions.Runtime;
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:3:using AURA.Modules.Executors;
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:5:namespace AURA.Modules.Runtime;
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:8:/// Executa o programa com o runtime resolvido, reutilizando a base
src/AURA.Modules/Runtime/RuntimeResolver.cs:12:public sealed class RuntimeResolver : IRuntimeResolver
src/AURA.Modules/Runtime/RuntimeResolver.cs:14:    public RuntimeResolution Resolve(string language)
src/AURA.Modules/Runtime/RuntimeResolver.cs:16:        if (RuntimeCatalog.NonRuntimeLanguages.Contains(language))
src/AURA.Modules/Runtime/RuntimeResolver.cs:18:            return new RuntimeResolution
src/AURA.Modules/Runtime/RuntimeResolver.cs:22:                Detail = "linguagem sem runtime executável (dado/documento)",
src/AURA.Modules/Runtime/RuntimeResolver.cs:26:        if (!RuntimeCatalog.Languages.TryGetValue(language, out RuntimeCatalog.LanguageDefinition? definition))
src/AURA.Modules/Runtime/RuntimeResolver.cs:28:            return new RuntimeResolution
src/AURA.Modules/Runtime/RuntimeResolver.cs:32:                Detail = $"runtime não catalogado para '{language}'",
src/AURA.Modules/Runtime/RuntimeResolver.cs:36:        var result = new RuntimeResolution
src/AURA.Modules/Runtime/RuntimeResolver.cs:4:using AURA.Abstractions.Runtime;
src/AURA.Modules/Runtime/RuntimeResolver.cs:59:            result.Detail = $"Runtime '{language}' não encontrado no PATH. " +
src/AURA.Modules/Runtime/RuntimeResolver.cs:6:namespace AURA.Modules.Runtime;
src/AURA.Modules/Runtime/RuntimeResolver.cs:9:/// Resolve o runtime de uma linguagem: procura o binário no PATH, obtém a
src/AURA.Modules/Runtime/SyntaxValidator.cs:2:using AURA.Abstractions.Runtime;
src/AURA.Modules/Runtime/SyntaxValidator.cs:4:namespace AURA.Modules.Runtime;
src/AURA.SystemInfo/SystemAnalyzer.cs:20:                OperatingSystem = RuntimeInformation.OSDescription,
src/AURA.SystemInfo/SystemAnalyzer.cs:21:                Architecture = RuntimeInformation.OSArchitecture.ToString(),
src/AURA.SystemInfo/SystemAnalyzer.cs:3:using System.Runtime.InteropServices;
```

## 13. Installer
```
src/AURA.Installer/ArtifactAnalysisService.cs:11:    /// <summary>Null quando ainda não existe IDependencyAnalyzer registrado para o tipo identificado.</summary>
src/AURA.Installer/ArtifactAnalysisService.cs:12:    public DependencyReport? Dependencies { get; init; }
src/AURA.Installer/ArtifactAnalysisService.cs:1:namespace AURA.Installer;
src/AURA.Installer/ArtifactAnalysisService.cs:27:    private readonly IReadOnlyDictionary<ArtifactType, IDependencyAnalyzer> _analyzers;
src/AURA.Installer/ArtifactAnalysisService.cs:32:        IEnumerable<IDependencyAnalyzer> analyzers,
src/AURA.Installer/ArtifactAnalysisService.cs:45:            new IDependencyAnalyzer[] { new PythonDependencyAnalyzer() });
src/AURA.Installer/ArtifactIdentification.cs:1:namespace AURA.Installer;
src/AURA.Installer/ArtifactType.cs:1:namespace AURA.Installer;
src/AURA.Installer/DependencyReport.cs:1:namespace AURA.Installer;
src/AURA.Installer/DependencyReport.cs:8:public sealed class DependencyReport
src/AURA.Installer/EnvironmentSelectionResult.cs:14:    public bool RuntimeAvailable { get; set; }
src/AURA.Installer/EnvironmentSelectionResult.cs:16:    /// <summary>Nome do binário do runtime resolvido (ex.: "python3"), ou null se não encontrado.</summary>
src/AURA.Installer/EnvironmentSelectionResult.cs:17:    public string? RuntimeBinary { get; set; }
src/AURA.Installer/EnvironmentSelectionResult.cs:19:    /// <summary>Comandos sugeridos pra instalar o runtime, adequados ao ambiente detectado (Termux/Linux/Windows/macOS).</summary>
src/AURA.Installer/EnvironmentSelectionResult.cs:20:    public List<string> InstallRuntimeSuggestions { get; set; } = new();
src/AURA.Installer/EnvironmentSelectionResult.cs:32:    public bool ReadyToInstall => RuntimeAvailable && HasEnoughDiskSpace;
src/AURA.Installer/EnvironmentSelectionResult.cs:3:namespace AURA.Installer;
src/AURA.Installer/EnvironmentSelectionResult.cs:7:/// Inteligente: o runtime necessário já está disponível? E o disco aguenta
src/AURA.Installer/EnvironmentSelectionService.cs:1:namespace AURA.Installer;
src/AURA.Installer/EnvironmentSelectionService.cs:24:    public async Task<EnvironmentSelectionResult?> SelectAsync(ArtifactType type, DependencyReport dependencies, CancellationToken cancellationToken = default)
src/AURA.Installer/EnvironmentSelectionService.cs:5:/// um DependencyReport da Etapa 2 em mãos e só quer saber se o ambiente
src/AURA.Installer/FileIdentifier.cs:3:namespace AURA.Installer;
src/AURA.Installer/IDependencyAnalyzer.cs:12:    Task<DependencyReport> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default);
src/AURA.Installer/IDependencyAnalyzer.cs:1:namespace AURA.Installer;
src/AURA.Installer/IDependencyAnalyzer.cs:5:/// ele precisa para rodar (pacotes, runtimes). Uma implementação por
src/AURA.Installer/IDependencyAnalyzer.cs:8:public interface IDependencyAnalyzer
src/AURA.Installer/IEnvironmentSelector.cs:12:    Task<EnvironmentSelectionResult> SelectAsync(DependencyReport dependencies, CancellationToken cancellationToken = default);
src/AURA.Installer/IEnvironmentSelector.cs:1:namespace AURA.Installer;
src/AURA.Installer/IEnvironmentSelector.cs:5:/// se o ambiente atual tem o runtime necessário e recursos suficientes pra
src/AURA.Installer/IFileIdentifier.cs:1:namespace AURA.Installer;
src/AURA.Installer/IInstaller.cs:12:    Task<InstallationResult> InstallAsync(DependencyReport dependencies, bool dryRun = true, CancellationToken cancellationToken = default);
src/AURA.Installer/IInstaller.cs:1:namespace AURA.Installer;
src/AURA.Installer/IInstaller.cs:8:public interface IInstaller
src/AURA.Installer/InstallationResult.cs:1:namespace AURA.Installer;
src/AURA.Installer/InstallationResult.cs:24:    public static InstallationResult NothingToInstall(ArtifactType type, bool dryRun) => new()
src/AURA.Installer/InstallationResult.cs:7:public sealed class InstallationResult
src/AURA.Installer/InstallationService.cs:10:    private readonly IReadOnlyDictionary<ArtifactType, IInstaller> _installers;
src/AURA.Installer/InstallationService.cs:12:    public InstallationService(IEnumerable<IInstaller> installers)
src/AURA.Installer/InstallationService.cs:14:        _installers = installers.ToDictionary(i => i.SupportedType);
src/AURA.Installer/InstallationService.cs:17:    public static InstallationService CreateDefault()
src/AURA.Installer/InstallationService.cs:19:        return new InstallationService(new IInstaller[] { new PythonInstaller() });
src/AURA.Installer/InstallationService.cs:1:namespace AURA.Installer;
src/AURA.Installer/InstallationService.cs:22:    /// <summary>Null quando ainda não existe IInstaller registrado para o tipo.</summary>
src/AURA.Installer/InstallationService.cs:23:    public async Task<InstallationResult?> InstallAsync(ArtifactType type, DependencyReport dependencies, bool dryRun = true, CancellationToken cancellationToken = default)
src/AURA.Installer/InstallationService.cs:25:        if (!_installers.TryGetValue(type, out var installer))
src/AURA.Installer/InstallationService.cs:30:        return await installer.InstallAsync(dependencies, dryRun, cancellationToken);
src/AURA.Installer/InstallationService.cs:8:public sealed class InstallationService
src/AURA.Installer/PythonDependencyAnalyzer.cs:12:public sealed class PythonDependencyAnalyzer : IDependencyAnalyzer
src/AURA.Installer/PythonDependencyAnalyzer.cs:3:namespace AURA.Installer;
src/AURA.Installer/PythonDependencyAnalyzer.cs:40:    public async Task<DependencyReport> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default)
src/AURA.Installer/PythonDependencyAnalyzer.cs:42:        var report = new DependencyReport { ArtifactType = ArtifactType.Python };
src/AURA.Installer/PythonDependencyAnalyzer.cs:61:            if (PythonStdlibModules.IsStdlib(module))
src/AURA.Installer/PythonEnvironmentSelector.cs:102:            "sudo apt install python3 python3-pip   # Debian/Ubuntu",
src/AURA.Installer/PythonEnvironmentSelector.cs:103:            "sudo dnf install python3 python3-pip   # Fedora",
src/AURA.Installer/PythonEnvironmentSelector.cs:19:    private const double PerDependencyMb = 30.0;
src/AURA.Installer/PythonEnvironmentSelector.cs:38:    public Task<EnvironmentSelectionResult> SelectAsync(DependencyReport dependencies, CancellationToken cancellationToken = default)
src/AURA.Installer/PythonEnvironmentSelector.cs:41:        bool runtimeAvailable = _pythonExecutor.IsAvailable();
src/AURA.Installer/PythonEnvironmentSelector.cs:43:        double estimatedMb = BaseOverheadMb + (dependencies.Dependencies.Count * PerDependencyMb);
src/AURA.Installer/PythonEnvironmentSelector.cs:50:            RuntimeAvailable = runtimeAvailable,
src/AURA.Installer/PythonEnvironmentSelector.cs:51:            RuntimeBinary = runtimeAvailable ? _pythonExecutor.Name : null,
src/AURA.Installer/PythonEnvironmentSelector.cs:57:        if (!runtimeAvailable)
src/AURA.Installer/PythonEnvironmentSelector.cs:59:            result.InstallRuntimeSuggestions.AddRange(SuggestPythonInstallCommands());
src/AURA.Installer/PythonEnvironmentSelector.cs:5:namespace AURA.Installer;
src/AURA.Installer/PythonEnvironmentSelector.cs:80:    private static List<string> SuggestPythonInstallCommands()
src/AURA.Installer/PythonEnvironmentSelector.cs:86:            return new List<string> { "pkg install python" };
src/AURA.Installer/PythonEnvironmentSelector.cs:96:            return new List<string> { "brew install python3" };
src/AURA.Installer/PythonEnvironmentSelector.cs:9:/// pra checar o runtime e o <see cref="SystemAnalyzer"/> pra checar disco livre.
src/AURA.Installer/PythonInstaller.cs:12:public sealed class PythonInstaller : IInstaller
src/AURA.Installer/PythonInstaller.cs:16:    public PythonInstaller() : this(new PythonExecutor()) { }
src/AURA.Installer/PythonInstaller.cs:19:    public PythonInstaller(IToolExecutor pythonExecutor)
src/AURA.Installer/PythonInstaller.cs:26:    public async Task<InstallationResult> InstallAsync(DependencyReport dependencies, bool dryRun = true, CancellationToken cancellationToken = default)
src/AURA.Installer/PythonInstaller.cs:30:            return InstallationResult.NothingToInstall(ArtifactType.Python, dryRun);
src/AURA.Installer/PythonInstaller.cs:33:        string commandText = $"python -m pip install {string.Join(" ", dependencies.Dependencies)}";
src/AURA.Installer/PythonInstaller.cs:35:        var result = new InstallationResult
src/AURA.Installer/PythonInstaller.cs:4:namespace AURA.Installer;
src/AURA.Installer/PythonInstaller.cs:60:            Arguments = new List<string> { "pip", "install" }.Concat(dependencies.Dependencies).ToList(),
src/AURA.Installer/PythonInstaller.cs:72:            result.Notes.Add($"pip install terminou com código {execResult.ExitCode}.");
src/AURA.Installer/PythonInstaller.cs:7:/// Instala dependências Python via "python -m pip install ..." reaproveitando
src/AURA.Installer/PythonStdlibModules.cs:10:public static class PythonStdlibModules
src/AURA.Installer/PythonStdlibModules.cs:1:namespace AURA.Installer;
src/AURA.Installer/PythonStdlibModules.cs:5:/// Python 3 — usado pra não sugerir "pip install os" por engano. Não é
```

## 14. Plataformas
```
```

## 15. Possíveis pontos desconectados
```
AgentSession                   5 arquivos
MemoryStore                    7 arquivos
SolutionStore                  2 arquivos
AgentManager                   6 arquivos
PluginWatcher                  2 arquivos
ModuleManager                  5 arquivos
DependencyAnalyzer             6 arquivos
OpenRouterClient               12 arquivos
```

## 16. TODO / FIXME / NOT IMPLEMENTED
```
src/AURA.AI/AgentTools/WorkspaceAgentTool.cs:7:    /// Base para ferramentas que operam em arquivos: garante que todo caminho
src/AURA.Installer/IInstaller.cs:5:/// encontradas na Etapa 2. dryRun=true por padrão em todo o pipeline —
src/AURA.Installer/ArtifactAnalysisService.cs:21:/// configuração, execução, gerenciamento) entram como novos métodos/serviços
src/AURA.Installer/PythonDependencyAnalyzer.cs:31:        ["Crypto"] = "pycryptodome",
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:54:        bool confirmed = await DisplayAlert("Limpar memória", "Apagar todo o histórico persistido?", "Apagar", "Cancelar");
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:81:            await ShowStatus($"{ok}/{pendentes.Count} baixados. Toque em 'Aplicar todos' para ativá-los.");
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:468:                    "Navegação isolada ativa (" + id + ").\n\nNada de cookies, histórico ou cache fica salvo: ao fechar a célula (aqui ou em Células), todos os dados de navegação são apagados.",
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:864:                ? "Endereço .onion exige Tor ativo.\n\nAbra o Orbot e ative o modo VPN (a conexão de todo o aparelho passa pelo Tor — então o WebView consegue acessar .onion). Depois toque em 'Abrir Orbot' e tente de novo."
src/AURA.Mobile/Pages/CellsPage.xaml.cs:121:            "Excluir '" + cell.Id + "' e todos os seus dados?",
src/AURA.Mobile/Diagnostics/SearchCatalog.cs:22:    /// Todos usam URLs públicas (sem API key) para abrir direto no WebView.
src/AURA.Mobile/Platforms/Android/AuraLog.cs:31:    /// Nenhum método lança: logging nunca pode derrubar o app.
src/AURA.Modules/Executors/GitExecutor.cs:28:    // Métodos de conveniência (CreateBranchAsync, CommitAsync, DiffAsync, etc.)
src/AURA.Modules/Executors/ProcessExecutorBase.cs:8:/// Base compartilhada por todos os executores que rodam um processo externo
```

## 17. Código potencialmente duplicado por nomes
```
DependencyReport
IDependencyAnalyzer
```

## 18. Projetos da solução
```
Project(s)
----------
src/AURA.Abstractions/AURA.Abstractions.csproj
src/AURA.Agents/AURA.Agents.csproj
src/AURA.AI/AURA.AI.csproj
src/AURA.CLI/AURA.CLI.csproj
src/AURA.Core/AURA.Core.csproj
src/AURA.Installer/AURA.Installer.csproj
src/AURA.Memory/AURA.Memory.csproj
src/AURA.Modules/AURA.Modules.csproj
src/AURA.Network/AURA.Network.csproj
src/AURA.SystemInfo/AURA.SystemInfo.csproj
src/AURA.Windows/AURA.Windows.csproj
tests/AURA.Tests/AURA.Tests.csproj
```

## 19. Build de diagnóstico
```
  Determining projects to restore...
  All projects are up-to-date for restore.
/data/data/com.termux/files/home/AURA/src/AURA.Network/NetworkStatus.cs(12,23): warning CS8618: Non-nullable property 'LocalIpAddress' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Network/AURA.Network.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Abstractions/Execution/ExecutionRequest.cs(19,23): warning CS8618: Non-nullable property 'WorkingDirectory' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Abstractions/AURA.Abstractions.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Network/NetworkStatus.cs(16,23): warning CS8618: Non-nullable property 'Message' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Network/AURA.Network.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Network/NetworkManager.cs(41,37): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Network/AURA.Network.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/SystemDiagnosticsResult.cs(8,23): warning CS8618: Non-nullable property 'OperatingSystem' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/AURA.SystemInfo.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/SystemDiagnosticsResult.cs(10,23): warning CS8618: Non-nullable property 'Architecture' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/AURA.SystemInfo.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/SystemDiagnosticsResult.cs(18,23): warning CS8618: Non-nullable property 'SystemDrive' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/AURA.SystemInfo.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/SystemAnalyzer.cs(103,31): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/AURA.SystemInfo.csproj]
  AURA.Abstractions -> /data/data/com.termux/files/home/AURA/src/AURA.Abstractions/bin/Debug/net10.0/AURA.Abstractions.dll
  AURA.Network -> /data/data/com.termux/files/home/AURA/src/AURA.Network/bin/Debug/net10.0/AURA.Network.dll
  AURA.SystemInfo -> /data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/bin/Debug/net10.0/AURA.SystemInfo.dll
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/CellCommand.cs(12,64): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/Runner.cs(65,32): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/Runner.cs(65,60): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/CellStore.cs(25,56): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(120,73): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(121,35): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(121,67): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(33,67): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(11,23): warning CS8618: Non-nullable property 'CellId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(13,23): warning CS8618: Non-nullable property 'From' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(15,23): warning CS8618: Non-nullable property 'To' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(60,22): warning CS8601: Possible null reference assignment. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(52,16): warning CS8618: Non-nullable field '_store' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(52,16): warning CS8618: Non-nullable property 'Events' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Bootstrap/AuraBootstrap.cs(35,16): warning CS8618: Non-nullable property 'Settings' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Bootstrap/AuraBootstrap.cs(35,16): warning CS8618: Non-nullable property 'Modules' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Logging/FileLogger.cs(19,32): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(13,23): warning CS8618: Non-nullable property 'Id' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(15,23): warning CS8618: Non-nullable property 'AppPath' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(17,23): warning CS8618: Non-nullable property 'Args' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(19,23): warning CS8618: Non-nullable property 'WorkingDirectory' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(31,23): warning CS8618: Non-nullable property 'TemplatePath' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(33,31): warning CS8618: Non-nullable property 'Limits' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(42,23): warning CS8618: Non-nullable property 'CellRoot' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/EventBus.cs(23,54): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/EventBus.cs(38,63): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(98,47): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(98,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/CellStore.cs(40,40): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(144,36): warning CS8601: Possible null reference assignment. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(145,26): warning CS8601: Possible null reference assignment. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/EventBus.cs(54,64): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/Runner.cs(46,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/Runner.cs(57,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(60,23): warning CS8618: Non-nullable property 'ModuleId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(26,23): warning CS8618: Non-nullable property 'Assistant' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(28,23): warning CS8618: Non-nullable property 'Question' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(30,23): warning CS8618: Non-nullable property 'Answer' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(32,23): warning CS8618: Non-nullable property 'CellId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(43,23): warning CS8618: Non-nullable property 'Executor' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(45,23): warning CS8618: Non-nullable property 'Command' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/CellStore.cs(86,46): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Configuration/ConfigLoader.cs(72,28): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Configuration/ConfigLoader.cs(76,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Configuration/ConfigLoader.cs(81,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Configuration/ConfigLoader.cs(89,36): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/PythonLauncher.cs(47,30): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/PythonLauncher.cs(50,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/PythonLauncher.cs(67,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(33,16): warning CS8618: Non-nullable field '_context' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/DependencyInjection/ServiceContainer.cs(32,50): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/DependencyInjection/ServiceContainer.cs(40,50): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/DependencyInjection/ServiceContainer.cs(46,50): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/Runner.cs(89,31): warning CS8604: Possible null reference argument for parameter 'workingDirectory' in 'Cell SimulationRuntime.CreateCell(string id, string appPath, string args = null, string templatePath = null, string workingDirectory = null, ResourceLimits? limits = null)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(139,42): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(140,36): warning CS8604: Possible null reference argument for parameter 'item' in 'void List<ILauncher>.Add(ILauncher item)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(145,38): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(146,21): warning CS8602: Dereference of a possibly null reference. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(190,28): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(247,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(434,58): warning CS8629: Nullable value type may be null. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(435,73): warning CS8604: Possible null reference argument for parameter 'line' in 'void SimulationRuntime.AppendLog(Cell cell, string line)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(436,72): warning CS8604: Possible null reference argument for parameter 'line' in 'void SimulationRuntime.AppendLog(Cell cell, string line)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(441,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(492,53): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(508,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(538,69): warning CS8604: Possible null reference argument for parameter 'line' in 'void SimulationRuntime.AppendLog(Cell cell, string line)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(539,68): warning CS8604: Possible null reference argument for parameter 'line' in 'void SimulationRuntime.AppendLog(Cell cell, string line)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(709,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
  AURA.Core -> /data/data/com.termux/files/home/AURA/src/AURA.Core/bin/Debug/net10.0/AURA.Core.dll
  AURA.Windows -> /data/data/com.termux/files/home/AURA/src/AURA.Windows/bin/Debug/net10.0/AURA.Windows.dll
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryStore.cs(32,58): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryEntry.cs(29,16): warning CS8618: Non-nullable property 'Role' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryEntry.cs(29,16): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryEntry.cs(29,16): warning CS8618: Non-nullable property 'CellId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryEntry.cs(29,16): warning CS8618: Non-nullable property 'Detail' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryStore.cs(108,43): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryStore.cs(120,32): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
  AURA.Memory -> /data/data/com.termux/files/home/AURA/src/AURA.Memory/bin/Debug/net10.0/AURA.Memory.dll
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleManager.cs(27,104): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(18,23): warning CS8618: Non-nullable property 'Id' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(20,23): warning CS8618: Non-nullable property 'DisplayName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(22,23): warning CS8618: Non-nullable property 'Icon' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(24,23): warning CS8618: Non-nullable property 'ShortDescription' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(30,23): warning CS8618: Non-nullable property 'PackageUrl' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(33,23): warning CS8618: Non-nullable property 'PackageVersion' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(39,29): warning CS8618: Non-nullable property 'Features' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(41,29): warning CS8618: Non-nullable property 'Includes' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(43,29): warning CS8618: Non-nullable property 'ImplementationSteps' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(45,29): warning CS8618: Non-nullable property 'AcquiredCapabilities' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(49,23): warning CS8618: Non-nullable property 'EstimatedTime' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleCatalog.cs(318,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleCatalog.cs(321,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleManager.cs(86,39): warning CS8604: Possible null reference argument for parameter 'path' in 'DirectoryInfo Directory.CreateDirectory(string path)'. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleManager.cs(135,26): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
  AURA.Modules -> /data/data/com.termux/files/home/AURA/src/AURA.Modules/bin/Debug/net10.0/AURA.Modules.dll
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(113,62): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(18,23): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(20,23): warning CS8618: Non-nullable property 'Executable' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(22,23): warning CS8618: Non-nullable property 'Description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(72,16): warning CS8618: Non-nullable property 'Events' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(105,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(146,31): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(199,55): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(249,29): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(259,30): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(273,33): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(315,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(320,30): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(323,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(340,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
  AURA.Agents -> /data/data/com.termux/files/home/AURA/src/AURA.Agents/bin/Debug/net10.0/AURA.Agents.dll
  AURA.AI -> /data/data/com.termux/files/home/AURA/src/AURA.AI/bin/Debug/net10.0/AURA.AI.dll
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(99,32): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(218,29): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(243,32): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(251,52): warning CS8604: Possible null reference argument for parameter 'id' in 'Task<Cell> Runner.RunAsync(SimulationRuntime runtime, string id, string filePath, string arguments = null, string templatePath = null, ResourceLimits? limits = null)'. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(251,70): warning CS8604: Possible null reference argument for parameter 'arguments' in 'Task<Cell> Runner.RunAsync(SimulationRuntime runtime, string id, string filePath, string arguments = null, string templatePath = null, ResourceLimits? limits = null)'. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(251,81): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(271,29): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(316,38): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(477,39): warning CS8604: Possible null reference argument for parameter 'path' in 'DirectoryInfo Directory.CreateDirectory(string path)'. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(562,29): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(588,83): warning CS8604: Possible null reference argument for parameter 'cellId' in 'Task<string> AgentManager.AskAsync(SimulationRuntime runtime, string question, string assistantName = "aichat", string cellId = null)'. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(26,42): warning CS8618: Non-nullable field '_runtime' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(27,31): warning CS8618: Non-nullable field '_runner' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(28,38): warning CS8618: Non-nullable field '_pluginWatcher' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(29,37): warning CS8618: Non-nullable field '_agentManager' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(30,32): warning CS8618: Non-nullable field '_logger' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(31,38): warning CS8618: Non-nullable field '_bootstrap' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(36,41): warning CS8618: Non-nullable field '_aiClient' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
  AURA.CLI -> /data/data/com.termux/files/home/AURA/src/AURA.CLI/bin/Debug/net10.0/AURA.CLI.dll

Build succeeded.

/data/data/com.termux/files/home/AURA/src/AURA.Network/NetworkStatus.cs(12,23): warning CS8618: Non-nullable property 'LocalIpAddress' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Network/AURA.Network.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Abstractions/Execution/ExecutionRequest.cs(19,23): warning CS8618: Non-nullable property 'WorkingDirectory' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Abstractions/AURA.Abstractions.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Network/NetworkStatus.cs(16,23): warning CS8618: Non-nullable property 'Message' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Network/AURA.Network.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Network/NetworkManager.cs(41,37): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Network/AURA.Network.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/SystemDiagnosticsResult.cs(8,23): warning CS8618: Non-nullable property 'OperatingSystem' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/AURA.SystemInfo.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/SystemDiagnosticsResult.cs(10,23): warning CS8618: Non-nullable property 'Architecture' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/AURA.SystemInfo.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/SystemDiagnosticsResult.cs(18,23): warning CS8618: Non-nullable property 'SystemDrive' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/AURA.SystemInfo.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/SystemAnalyzer.cs(103,31): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.SystemInfo/AURA.SystemInfo.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/CellCommand.cs(12,64): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/Runner.cs(65,32): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/Runner.cs(65,60): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/CellStore.cs(25,56): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(120,73): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(121,35): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(121,67): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(33,67): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(11,23): warning CS8618: Non-nullable property 'CellId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(13,23): warning CS8618: Non-nullable property 'From' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(15,23): warning CS8618: Non-nullable property 'To' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(60,22): warning CS8601: Possible null reference assignment. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(52,16): warning CS8618: Non-nullable field '_store' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(52,16): warning CS8618: Non-nullable property 'Events' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Bootstrap/AuraBootstrap.cs(35,16): warning CS8618: Non-nullable property 'Settings' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Bootstrap/AuraBootstrap.cs(35,16): warning CS8618: Non-nullable property 'Modules' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Logging/FileLogger.cs(19,32): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(13,23): warning CS8618: Non-nullable property 'Id' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(15,23): warning CS8618: Non-nullable property 'AppPath' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(17,23): warning CS8618: Non-nullable property 'Args' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(19,23): warning CS8618: Non-nullable property 'WorkingDirectory' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(31,23): warning CS8618: Non-nullable property 'TemplatePath' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(33,31): warning CS8618: Non-nullable property 'Limits' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/Cell.cs(42,23): warning CS8618: Non-nullable property 'CellRoot' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/EventBus.cs(23,54): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/EventBus.cs(38,63): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(98,47): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(98,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/CellStore.cs(40,40): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(144,36): warning CS8601: Possible null reference assignment. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(145,26): warning CS8601: Possible null reference assignment. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/EventBus.cs(54,64): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/Runner.cs(46,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/Runner.cs(57,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(60,23): warning CS8618: Non-nullable property 'ModuleId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(26,23): warning CS8618: Non-nullable property 'Assistant' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(28,23): warning CS8618: Non-nullable property 'Question' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(30,23): warning CS8618: Non-nullable property 'Answer' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(32,23): warning CS8618: Non-nullable property 'CellId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(43,23): warning CS8618: Non-nullable property 'Executor' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Events/AuraEvents.cs(45,23): warning CS8618: Non-nullable property 'Command' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/CellStore.cs(86,46): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Configuration/ConfigLoader.cs(72,28): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Configuration/ConfigLoader.cs(76,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Configuration/ConfigLoader.cs(81,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Configuration/ConfigLoader.cs(89,36): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/PythonLauncher.cs(47,30): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/PythonLauncher.cs(50,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/PythonLauncher.cs(67,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(33,16): warning CS8618: Non-nullable field '_context' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/DependencyInjection/ServiceContainer.cs(32,50): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/DependencyInjection/ServiceContainer.cs(40,50): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/DependencyInjection/ServiceContainer.cs(46,50): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Launchers/Runner.cs(89,31): warning CS8604: Possible null reference argument for parameter 'workingDirectory' in 'Cell SimulationRuntime.CreateCell(string id, string appPath, string args = null, string templatePath = null, string workingDirectory = null, ResourceLimits? limits = null)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(139,42): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(140,36): warning CS8604: Possible null reference argument for parameter 'item' in 'void List<ILauncher>.Add(ILauncher item)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(145,38): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(146,21): warning CS8602: Dereference of a possibly null reference. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(190,28): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/PluginWatcher.cs(247,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(434,58): warning CS8629: Nullable value type may be null. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(435,73): warning CS8604: Possible null reference argument for parameter 'line' in 'void SimulationRuntime.AppendLog(Cell cell, string line)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(436,72): warning CS8604: Possible null reference argument for parameter 'line' in 'void SimulationRuntime.AppendLog(Cell cell, string line)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(441,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(492,53): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(508,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(538,69): warning CS8604: Possible null reference argument for parameter 'line' in 'void SimulationRuntime.AppendLog(Cell cell, string line)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(539,68): warning CS8604: Possible null reference argument for parameter 'line' in 'void SimulationRuntime.AppendLog(Cell cell, string line)'. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Core/Runtime/SimulationRuntime.cs(709,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Core/AURA.Core.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryStore.cs(32,58): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryEntry.cs(29,16): warning CS8618: Non-nullable property 'Role' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryEntry.cs(29,16): warning CS8618: Non-nullable property 'Text' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryEntry.cs(29,16): warning CS8618: Non-nullable property 'CellId' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryEntry.cs(29,16): warning CS8618: Non-nullable property 'Detail' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryStore.cs(108,43): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Memory/MemoryStore.cs(120,32): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Memory/AURA.Memory.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleManager.cs(27,104): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(18,23): warning CS8618: Non-nullable property 'Id' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(20,23): warning CS8618: Non-nullable property 'DisplayName' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(22,23): warning CS8618: Non-nullable property 'Icon' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(24,23): warning CS8618: Non-nullable property 'ShortDescription' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(30,23): warning CS8618: Non-nullable property 'PackageUrl' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(33,23): warning CS8618: Non-nullable property 'PackageVersion' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(39,29): warning CS8618: Non-nullable property 'Features' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(41,29): warning CS8618: Non-nullable property 'Includes' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(43,29): warning CS8618: Non-nullable property 'ImplementationSteps' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(45,29): warning CS8618: Non-nullable property 'AcquiredCapabilities' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleInfo.cs(49,23): warning CS8618: Non-nullable property 'EstimatedTime' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleCatalog.cs(318,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleCatalog.cs(321,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleManager.cs(86,39): warning CS8604: Possible null reference argument for parameter 'path' in 'DirectoryInfo Directory.CreateDirectory(string path)'. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Modules/ModuleManager.cs(135,26): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Modules/AURA.Modules.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(113,62): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(18,23): warning CS8618: Non-nullable property 'Name' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(20,23): warning CS8618: Non-nullable property 'Executable' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(22,23): warning CS8618: Non-nullable property 'Description' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(72,16): warning CS8618: Non-nullable property 'Events' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the property as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(105,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(146,31): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(199,55): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(249,29): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(259,30): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(273,33): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(315,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(320,30): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(323,24): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.Agents/AgentManager.cs(340,20): warning CS8603: Possible null reference return. [/data/data/com.termux/files/home/AURA/src/AURA.Agents/AURA.Agents.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(99,32): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(218,29): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(243,32): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(251,52): warning CS8604: Possible null reference argument for parameter 'id' in 'Task<Cell> Runner.RunAsync(SimulationRuntime runtime, string id, string filePath, string arguments = null, string templatePath = null, ResourceLimits? limits = null)'. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(251,70): warning CS8604: Possible null reference argument for parameter 'arguments' in 'Task<Cell> Runner.RunAsync(SimulationRuntime runtime, string id, string filePath, string arguments = null, string templatePath = null, ResourceLimits? limits = null)'. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(251,81): warning CS8625: Cannot convert null literal to non-nullable reference type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(271,29): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(316,38): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(477,39): warning CS8604: Possible null reference argument for parameter 'path' in 'DirectoryInfo Directory.CreateDirectory(string path)'. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(562,29): warning CS8600: Converting null literal or possible null value to non-nullable type. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(588,83): warning CS8604: Possible null reference argument for parameter 'cellId' in 'Task<string> AgentManager.AskAsync(SimulationRuntime runtime, string question, string assistantName = "aichat", string cellId = null)'. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(26,42): warning CS8618: Non-nullable field '_runtime' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(27,31): warning CS8618: Non-nullable field '_runner' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(28,38): warning CS8618: Non-nullable field '_pluginWatcher' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(29,37): warning CS8618: Non-nullable field '_agentManager' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(30,32): warning CS8618: Non-nullable field '_logger' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(31,38): warning CS8618: Non-nullable field '_bootstrap' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
/data/data/com.termux/files/home/AURA/src/AURA.CLI/Program.cs(36,41): warning CS8618: Non-nullable field '_aiClient' must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring the field as nullable. [/data/data/com.termux/files/home/AURA/src/AURA.CLI/AURA.CLI.csproj]
    133 Warning(s)
    0 Error(s)

Time Elapsed 00:02:50.87
BUILD_EXIT_CODE=0
```

## 20. Classificação inicial

### Provável núcleo
- AURA.Core
- AURA.AI
- AURA.Agents
- AURA.Memory
- AURA.CLI
- AURA.Abstractions

### Infraestrutura a auditar
- AURA.Modules
- AURA.Installer
- AURA.SystemInfo
- AURA.Network
- AURA.Windows

### Componentes a verificar como módulos
- AURA.Mobile
- Browser
- VPN/Tor
- funcionalidades específicas de plataforma

### Regra
Nenhum componente será removido somente com base nesta classificação.
A decisão final deve usar referências e fluxo de runtime.
