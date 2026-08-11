# AURA — Contexto Técnico

> Relatório gerado automaticamente em: 2026-08-09 05:34:32 -0300
> Raiz: /data/data/com.termux/files/home/AURA


## Estado Git

```text
## feat/project-access...origin/feat/project-access
 M src/AURA.AI/AgentSession.cs
?? .aura/auditoria/
?? .aura/backup-agent-solution-20260808-162923/
?? .aura/backup-agent-solution-v2-20260808-163705/
?? .aura/backup-memory-auto/
?? .aura/backup-solutions-20260808-161907/
?? .aura/diagnostico/
?? .aura/memory-fix-v2-20260809-043306/
?? .aura/memory-fix-v2-20260809-045621/
?? CLAUDE.md
?? aura-diagnostico.txt
?? coleta_analise.txt
?? coleta_para_analise.sh
?? fix-agent-memory-v2.sh
?? memory.json
?? reports/
?? scripts/ativar-memoria-automatica.sh
?? scripts/auditoria-completa.sh
?? scripts/auditoria-fluxo-real.sh
?? scripts/aura-context.sh
?? scripts/aura-inspect.sh
?? scripts/instalar-memoria-solucoes.sh
?? scripts/integrar-solutionstore-agent-v2.sh
?? scripts/integrar-solutionstore-agent.sh
?? scripts/mapear-capacidades-aura.sh
```

**Branch:**
```text
feat/project-access
```

## Últimos commits

```text
2969a70 (HEAD -> feat/project-access, origin/feat/project-access) feat: conecta AgentSession ao conhecimento validado
489ff66 feat: adiciona memória de soluções validadas
05b398a Atualiza estado atual do AURA
64c5955 fix(agent): improve Ollama tool argument handling
8363fcc fix(mobile): disambiguate      Android Uri
```

## Estrutura de projetos

```text
```

## Arquivos C#

```text
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

## Arquitetura AURA.AI

```text
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
```

## Símbolos principais

```text
src/AURA.SystemInfo/SystemDiagnosticsResult.cs:6:    public class SystemDiagnosticsResult
src/AURA.SystemInfo/SystemAnalyzer.cs:12:    public sealed class SystemAnalyzer
src/AURA.SystemInfo/SystemAnalyzer.cs:124:        private struct MEMORYSTATUSEX
src/AURA.AI/AgentSession.cs:19:    public sealed class AgentSession
src/AURA.AI/AgentSession.cs:34:        public AgentSession(OpenRouterClient client, IEnumerable<AgentTool> tools,
src/AURA.AI/AiAssistant.cs:15:    public sealed class AiAssistant
src/AURA.AI/OpenRouterClient.cs:20:    public sealed class OpenRouterOptions
src/AURA.AI/OpenRouterClient.cs:35:    public sealed class OpenRouterClient
src/AURA.AI/AgentChat.cs:10:    public sealed class AgentMessage
src/AURA.AI/AgentChat.cs:22:    public sealed class AgentToolCall
src/AURA.AI/AgentChat.cs:33:    public sealed class AgentChatResponse
src/AURA.AI/AgentChat.cs:45:    public sealed class AgentStep
src/AURA.AI/ProviderCatalog.cs:5:    public sealed class ProviderModel
src/AURA.AI/ProviderCatalog.cs:16:    public sealed class ProviderInfo
src/AURA.AI/ProviderCatalog.cs:25:    public static class ProviderCatalog
src/AURA.AI/AiAssistantService.cs:18:    public static class AiAssistantService
src/AURA.AI/AgentTool.cs:9:    public sealed class AgentToolParameter
src/AURA.AI/AgentTool.cs:17:    public sealed class AgentToolDefinition
src/AURA.AI/AgentTool.cs:32:    public abstract class AgentTool
src/AURA.AI/AgentTools/ShellAgentTool.cs:15:    public sealed class ShellAgentTool : AgentTool
src/AURA.AI/AgentTools/FileTools.cs:11:    public sealed class ListDirTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:71:    public sealed class ReadFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:112:    public sealed class WriteFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:157:    public sealed class EditFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/WorkspaceAgentTool.cs:11:    public abstract class WorkspaceAgentTool : AgentTool
src/AURA.Installer/ArtifactType.cs:8:public enum ArtifactType
src/AURA.Installer/IEnvironmentSelector.cs:8:public interface IEnvironmentSelector
src/AURA.Installer/IFileIdentifier.cs:8:public interface IFileIdentifier
src/AURA.Installer/IDependencyAnalyzer.cs:8:public interface IDependencyAnalyzer
src/AURA.Installer/IInstaller.cs:8:public interface IInstaller
src/AURA.Installer/PythonInstaller.cs:12:public sealed class PythonInstaller : IInstaller
src/AURA.Installer/DependencyReport.cs:8:public sealed class DependencyReport
src/AURA.Installer/EnvironmentSelectionService.cs:9:public sealed class EnvironmentSelectionService
src/AURA.Installer/EnvironmentSelectionService.cs:13:    public EnvironmentSelectionService(IEnumerable<IEnvironmentSelector> selectors)
src/AURA.Installer/EnvironmentSelectionResult.cs:10:public sealed class EnvironmentSelectionResult
src/AURA.Installer/ArtifactAnalysisService.cs:7:public sealed class ArtifactAnalysisResult
src/AURA.Installer/ArtifactAnalysisService.cs:24:public sealed class ArtifactAnalysisService
src/AURA.Installer/ArtifactIdentification.cs:6:public sealed class ArtifactIdentification
src/AURA.Installer/PythonDependencyAnalyzer.cs:12:public sealed class PythonDependencyAnalyzer : IDependencyAnalyzer
src/AURA.Installer/PythonEnvironmentSelector.cs:12:public sealed class PythonEnvironmentSelector : IEnvironmentSelector
src/AURA.Installer/PythonStdlibModules.cs:10:public static class PythonStdlibModules
src/AURA.Installer/FileIdentifier.cs:10:public sealed class FileIdentifier : IFileIdentifier
src/AURA.Installer/InstallationResult.cs:7:public sealed class InstallationResult
src/AURA.Installer/InstallationService.cs:8:public sealed class InstallationService
src/AURA.Installer/InstallationService.cs:12:    public InstallationService(IEnumerable<IInstaller> installers)
src/AURA.Memory/SolutionRule.cs:12:    public sealed class SolutionRule
src/AURA.Memory/RequestContext.cs:11:    public sealed class RequestContext
src/AURA.Memory/MemoryEntry.cs:6:    public enum MemoryKind
src/AURA.Memory/MemoryEntry.cs:12:    public sealed class MemoryEntry
src/AURA.Memory/MemoryStore.cs:19:    public sealed class MemoryStore
src/AURA.Memory/MemoryStore.cs:144:        private sealed class MemoryDocument
src/AURA.Memory/SolutionStore.cs:17:    public sealed class SolutionStore
src/AURA.Core/Configuration/ModulesConfiguration.cs:9:    public class ModulesConfiguration
src/AURA.Core/Configuration/ModulesConfiguration.cs:23:    public class ModuleFlags
src/AURA.Core/Configuration/ConfigLoader.cs:14:    public sealed class ConfigLoader
src/AURA.Core/Configuration/ConfigLoader.cs:65:        private T Load<T>(string path) where T : class
src/AURA.Core/Configuration/AuraConfiguration.cs:6:    public class AuraConfiguration
src/AURA.Core/Bootstrap/AuraBootstrap.cs:14:    public sealed class AuraBootstrap
src/AURA.Core/DependencyInjection/ServiceContainer.cs:11:    public sealed class ServiceContainer
src/AURA.Core/Logging/FileLogger.cs:10:    public sealed class FileLogger : ILogger
src/AURA.Core/Logging/ILogger.cs:6:    public interface ILogger
src/AURA.Core/Logging/ConsoleLogger.cs:10:    public sealed class ConsoleLogger : ILogger
src/AURA.Core/Launchers/Runner.cs:14:    public sealed class Runner
src/AURA.Core/Launchers/Runner.cs:30:        public Runner(IEnumerable<ILauncher> launchers)
src/AURA.Core/Launchers/Runner.cs:96:        private IEnumerable<string> SupportedExtensions()
src/AURA.Core/Launchers/NodeLauncher.cs:10:    public sealed class NodeLauncher : ILauncher
src/AURA.Core/Launchers/CellCommand.cs:10:    public sealed class CellCommand
src/AURA.Core/Launchers/DllLauncher.cs:9:    public sealed class DllLauncher : ILauncher
src/AURA.Core/Launchers/PythonLauncher.cs:10:    public sealed class PythonLauncher : ILauncher
src/AURA.Core/Launchers/GoLauncher.cs:10:    public sealed class GoLauncher : ILauncher
src/AURA.Core/Launchers/JarLauncher.cs:10:    public sealed class JarLauncher : ILauncher
src/AURA.Core/Launchers/ILauncher.cs:10:    public interface ILauncher
src/AURA.Core/VersionInfo.cs:8:    public static class VersionInfo
src/AURA.Core/Runtime/PluginWatcher.cs:21:    public sealed class PluginWatcher : IDisposable
src/AURA.Core/Runtime/PluginWatcher.cs:224:        private sealed class PluginLoadContext : AssemblyLoadContext
src/AURA.Core/Runtime/CellState.cs:8:    public enum CellState
src/AURA.Core/Runtime/SimulationRuntime.cs:21:    public sealed class SimulationRuntime : IDisposable
src/AURA.Core/Runtime/CellStore.cs:13:    public sealed class CellStore
src/AURA.Core/Runtime/CellStore.cs:97:        private sealed class CellStoreDocument
src/AURA.Core/Runtime/Cell.cs:11:    public sealed class Cell
src/AURA.Core/Runtime/DirectoryCellBackend.cs:12:    public sealed class DirectoryCellBackend : ICellBackend
src/AURA.Core/Runtime/ICellBackend.cs:9:    public interface ICellBackend
src/AURA.Core/Runtime/ResourceLimits.cs:10:    public sealed class ResourceLimits
src/AURA.Core/Abstractions/IModule.cs:10:    public interface IModule
src/AURA.Core/Abstractions/ICommand.cs:7:    public interface ICommand
src/AURA.Core/Abstractions/IPlugin.cs:7:    public interface IPlugin
src/AURA.Core/Abstractions/IAgent.cs:7:    public interface IAgent
src/AURA.Core/Abstractions/IService.cs:6:    public interface IService
src/AURA.Core/Events/EventBus.cs:9:    public sealed class EventBus
src/AURA.Core/Events/AuraEvents.cs:9:    public sealed class CellStateChangedEvent : IEvent
src/AURA.Core/Events/AuraEvents.cs:24:    public sealed class AssistantRespondedEvent : IEvent
src/AURA.Core/Events/AuraEvents.cs:41:    public sealed class ExecutorCompletedEvent : IEvent
src/AURA.Core/Events/AuraEvents.cs:58:    public sealed class ModuleStateChangedEvent : IEvent
src/AURA.Core/Events/IEvent.cs:8:    public interface IEvent
src/AURA.Network/NetworkManager.cs:13:    public sealed class NetworkManager
src/AURA.Network/NetworkStatus.cs:6:    public class NetworkStatus
src/AURA.CLI/Program.cs:24:    internal class Program
src/AURA.Agents/AgentManager.cs:16:    public sealed class AgentInfo
src/AURA.Agents/AgentManager.cs:36:    public sealed class AgentManager
src/AURA.Agents/AgentManager.cs:72:        public AgentManager(ILogger logger, IEnumerable<AgentInfo> assistants)
src/AURA.Agents/AgentManager.cs:283:        private sealed class Definition
src/AURA.Mobile/MainPage.cs:7:    public class MainPage : TabbedPage
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs:5:    public partial class ImageSearchPage : ContentPage
src/AURA.Mobile/Pages/LogsPage.xaml.cs:8:public partial class LogsPage : ContentPage
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:8:    public sealed class BrowserSettingsPage : ContentPage
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:6:public partial class TerminalPage : ContentPage
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:6:public partial class MemoryPage : ContentPage
src/AURA.Mobile/Pages/AgentPage.xaml.cs:6:public partial class AgentPage : ContentPage
src/AURA.Mobile/Pages/RunPage.xaml.cs:7:public partial class RunPage : ContentPage
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:7:public partial class ExecutorsPage : ContentPage
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:139:public class ExecutorStatus
src/AURA.Mobile/Pages/SectionPage.cs:7:public sealed class SectionPage : ContentPage
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:7:    public partial class ModulesPage : ContentPage
src/AURA.Mobile/Pages/ChatPage.xaml.cs:6:public partial class ChatPage : ContentPage
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:9:    public partial class BrowserPage : ContentPage
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:159:        private sealed class BrowserTab
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:710:        private sealed class AuraFindListener : Java.Lang.Object, Android.Webkit.WebView.IFindListener
src/AURA.Mobile/Pages/HomePage.xaml.cs:7:public partial class HomePage : ContentPage
src/AURA.Mobile/Pages/CellsPage.xaml.cs:7:public partial class CellsPage : ContentPage
src/AURA.Mobile/Pages/FixesPage.xaml.cs:6:public partial class FixesPage : ContentPage
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:10:    public static class RuntimeConfig
src/AURA.Mobile/Diagnostics/FixProposal.cs:7:    public sealed class FixProposal
src/AURA.Mobile/Diagnostics/FixProposal.cs:20:    public static class FixProposalParser
src/AURA.Mobile/Diagnostics/AgentWorkspace.cs:9:    public static class AgentWorkspace
src/AURA.Mobile/Diagnostics/SearchCatalog.cs:5:    public sealed class SearchEngine
src/AURA.Mobile/Diagnostics/SearchCatalog.cs:12:    public sealed class ImageSearchProvider
src/AURA.Mobile/Diagnostics/SearchCatalog.cs:24:    public static class SearchCatalog
src/AURA.Mobile/Diagnostics/ProjectAccessService.cs:16:public static class ProjectAccessService
src/AURA.Mobile/Diagnostics/ProjectAccessService.cs:160:    private static IEnumerable<DocumentEntry> QueryChildren(ContentResolver resolver,
src/AURA.Mobile/Diagnostics/ProjectAccessService.cs:228:    private readonly record struct DocumentEntry(string Id, string Name, string MimeType);
src/AURA.Mobile/ViewModels/ModuleRow.cs:10:    public sealed class ModuleRow
src/AURA.Mobile/Platforms/Android/StoragePermissionHelper.cs:8:public static class StoragePermissionHelper
src/AURA.Mobile/Platforms/Android/MainApplication.cs:7:public class MainApplication : MauiApplication
src/AURA.Mobile/Platforms/Android/AuraLog.cs:33:    public static class AuraLog
src/AURA.Mobile/Platforms/Android/AuraLog.cs:312:        private sealed class AuraUncaughtExceptionHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
src/AURA.Mobile/Platforms/Android/VpnHelper.cs:12:    public static class VpnHelper
src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs:9:    public sealed class AuraLongClickListener : Java.Lang.Object, global::Android.Views.View.IOnLongClickListener
src/AURA.Mobile/Platforms/Android/WebView/AuraTouchListener.cs:9:    public sealed class AuraTouchListener : Java.Lang.Object, global::Android.Views.View.IOnTouchListener
src/AURA.Mobile/Platforms/Android/WebView/AuraDownloadListener.cs:8:    public sealed class AuraDownloadListener : Java.Lang.Object, global::Android.Webkit.IDownloadListener
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:17:    public sealed class AuraWebViewHandler : WebViewHandler
src/AURA.Mobile/Platforms/Android/MainActivity.cs:13:public class MainActivity : MauiAppCompatActivity
src/AURA.Mobile/MauiProgram.cs:17:public static class MauiProgram
src/AURA.Mobile/App.xaml.cs:5:public partial class App : Application
src/AURA.Abstractions/Execution/ExecutionRequest.cs:12:    public sealed class ExecutionRequest
src/AURA.Abstractions/Execution/IToolExecutor.cs:11:    public interface IToolExecutor
src/AURA.Abstractions/Execution/ExecutionResult.cs:9:    public sealed class ExecutionResult
src/AURA.Abstractions/Runtime/RuntimeModels.cs:10:public sealed class Detection
src/AURA.Abstractions/Runtime/RuntimeModels.cs:25:public sealed class RuntimeResolution
src/AURA.Abstractions/Runtime/RuntimeModels.cs:41:public sealed class Dependency
src/AURA.Abstractions/Runtime/RuntimeModels.cs:55:public sealed class DependencyReport
src/AURA.Abstractions/Runtime/RuntimeModels.cs:67:public sealed class SyntaxResult
src/AURA.Abstractions/Runtime/RuntimeModels.cs:78:public sealed class CompatReport
src/AURA.Abstractions/Runtime/RuntimeModels.cs:89:public sealed record InstallStep(string What, string Command, bool IsRuntime);
src/AURA.Abstractions/Runtime/RuntimeModels.cs:94:public sealed class InstallPlan
src/AURA.Abstractions/Runtime/RuntimeModels.cs:104:public sealed class ExecutionOutcome
src/AURA.Abstractions/Runtime/RuntimeModels.cs:132:public sealed class PipelineReport
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:10:public interface IRuntimeDetector
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:19:public interface IRuntimeResolver
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:28:public interface IDependencyAnalyzer
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:37:public interface ISyntaxValidator
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:46:public interface ICompatibilityChecker
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:55:public interface IRuntimeInstaller
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:70:public interface IRuntimeManager
src/AURA.Modules/Executors/PythonExecutor.cs:10:public sealed class PythonExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/NodeExecutor.cs:9:public sealed class NodeExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/GitExecutor.cs:11:public sealed class GitExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/ProcessExecutorBase.cs:13:public abstract class ProcessExecutorBase : IToolExecutor
src/AURA.Modules/Executors/ShellExecutor.cs:8:public sealed class ShellExecutor : ProcessExecutorBase
src/AURA.Modules/ModuleManager.cs:18:    public sealed class ModuleManager
src/AURA.Modules/ModuleCatalog.cs:15:    public static class ModuleCatalog
src/AURA.Modules/ModuleInfo.cs:16:    public sealed class ModuleInfo : IModule
src/AURA.Modules/ModuleDifficulty.cs:6:    public enum ModuleDifficulty
src/AURA.Modules/ModuleStatus.cs:7:    public enum ModuleStatus
src/AURA.Modules/Runtime/LanguageDetector.cs:11:public sealed class LanguageDetector : IRuntimeDetector
src/AURA.Modules/Runtime/BinaryPath.cs:7:public static class BinaryPath
src/AURA.Modules/Runtime/SyntaxValidator.cs:11:public sealed class SyntaxValidator : ISyntaxValidator
src/AURA.Modules/Runtime/Installer.cs:11:public sealed class Installer : IRuntimeInstaller
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:12:public sealed class RuntimeProcessExecutor : ProcessExecutorBase
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:12:public sealed class DependencyAnalyzer : IDependencyAnalyzer
src/AURA.Modules/Runtime/RuntimeCatalog.cs:11:public static class RuntimeCatalog
src/AURA.Modules/Runtime/RuntimeCatalog.cs:13:    public sealed record LanguageDefinition(
src/AURA.Modules/Runtime/RuntimeManager.cs:12:public sealed class RuntimeManager : IRuntimeManager
src/AURA.Modules/Runtime/CompatibilityChecker.cs:11:public sealed class CompatibilityChecker : ICompatibilityChecker
src/AURA.Modules/Runtime/RuntimeResolver.cs:12:public sealed class RuntimeResolver : IRuntimeResolver
```

## Fluxo de IA

```text
src/AURA.AI/AgentSession.cs:14:    /// Loop agêntico sobre o OpenRouterClient: envia a conversa com as
src/AURA.AI/AgentSession.cs:19:    public sealed class AgentSession
src/AURA.AI/AgentSession.cs:23:        private readonly OpenRouterClient _client;
src/AURA.AI/AgentSession.cs:25:        private readonly List<AgentTool> _tools;
src/AURA.AI/AgentSession.cs:34:        public AgentSession(OpenRouterClient client, IEnumerable<AgentTool> tools,
src/AURA.AI/AgentSession.cs:38:            _tools = (tools ?? Enumerable.Empty<AgentTool>()).ToList();
src/AURA.AI/AgentSession.cs:88:                AgentChatResponse response = await _client.ChatToolsAsync(
src/AURA.AI/AgentSession.cs:111:                    foreach (AgentToolCall call in response.ToolCalls)
src/AURA.AI/AgentSession.cs:156:            AgentToolCall call,
src/AURA.AI/AgentSession.cs:159:            AgentTool? tool = _tools.FirstOrDefault(
src/AURA.AI/AiAssistant.cs:15:    public sealed class AiAssistant
src/AURA.AI/AiAssistant.cs:17:        private readonly OpenRouterClient _client;
src/AURA.AI/AiAssistant.cs:21:        public AiAssistant(OpenRouterClient client, MemoryStore memory, ILogger? logger = null)
src/AURA.AI/AiAssistant.cs:33:            string answer = await _client.ChatAsync(question, httpClient, ct).ConfigureAwait(false);
src/AURA.AI/OpenRouterClient.cs:18:    /// o config do aichat (OpenRouter, qwen/qwen-plus).
src/AURA.AI/OpenRouterClient.cs:20:    public sealed class OpenRouterOptions
src/AURA.AI/OpenRouterClient.cs:22:        public string Provider { get; set; } = "openrouter";
src/AURA.AI/OpenRouterClient.cs:32:    /// Cliente mínimo para OpenRouter chat completions. Construa a requisição
src/AURA.AI/OpenRouterClient.cs:33:    /// (testável sem rede) com BuildRequest; execute com ChatAsync.
src/AURA.AI/OpenRouterClient.cs:35:    public sealed class OpenRouterClient
src/AURA.AI/OpenRouterClient.cs:39:        public OpenRouterOptions Options { get; }
src/AURA.AI/OpenRouterClient.cs:41:        public OpenRouterClient(OpenRouterOptions options, ILogger? logger = null)
src/AURA.AI/OpenRouterClient.cs:72:            if (!string.Equals(Options.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
src/AURA.AI/OpenRouterClient.cs:89:        public async Task<string> ChatAsync(string question,
src/AURA.AI/OpenRouterClient.cs:133:        /// modelo; o AgentSession executa as chamadas e faz o loop.
src/AURA.AI/OpenRouterClient.cs:135:        public async Task<AgentChatResponse> ChatToolsAsync(
src/AURA.AI/OpenRouterClient.cs:137:            List<AgentToolDefinition>? tools = null,
src/AURA.AI/OpenRouterClient.cs:174:                        foreach (AgentToolCall tc in m.ToolCalls)
src/AURA.AI/OpenRouterClient.cs:188:                        mo["tool_calls"] = calls;
src/AURA.AI/OpenRouterClient.cs:200:                foreach (AgentToolDefinition t in tools)
src/AURA.AI/OpenRouterClient.cs:203:                    foreach (KeyValuePair<string, AgentToolParameter> p in t.Parameters)
src/AURA.AI/OpenRouterClient.cs:281:                        var calls = new List<AgentToolCall>();
src/AURA.AI/OpenRouterClient.cs:282:                        if (msg.TryGetProperty("tool_calls", out JsonElement toolCalls))
src/AURA.AI/OpenRouterClient.cs:295:                                calls.Add(new AgentToolCall
src/AURA.AI/OpenRouterClient.cs:304:                        // Ollama/Qwen pequeno pode retornar a chamada de ferramenta
src/AURA.AI/OpenRouterClient.cs:305:                        // como JSON no campo content, em vez de usar tool_calls.
src/AURA.AI/OpenRouterClient.cs:308:                            List<AgentToolCall>? textCalls = TryParseTextToolCall(content);
src/AURA.AI/OpenRouterClient.cs:339:            if (string.Equals(Options.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
src/AURA.AI/OpenRouterClient.cs:344:                        "Endpoint do Ollama não configurado.");
src/AURA.AI/OpenRouterClient.cs:353:                    "ApiKey do provedor LLM não configurada. Defina OpenRouterOptions.ApiKey.");
src/AURA.AI/OpenRouterClient.cs:374:        private static List<AgentToolCall>? TryParseTextToolCall(string? content)
src/AURA.AI/OpenRouterClient.cs:426:                return new List<AgentToolCall>
src/AURA.AI/OpenRouterClient.cs:428:                    new AgentToolCall
src/AURA.AI/OpenRouterClient.cs:430:                        Id = "ollama-tool-" + Guid.NewGuid().ToString("N"),
src/AURA.AI/AgentChat.cs:7:    /// (roles: system | user | assistant | tool). Em tool_calls o conteúdo é
src/AURA.AI/AgentChat.cs:18:        public List<AgentToolCall>? ToolCalls { get; set; }
src/AURA.AI/AgentChat.cs:22:    public sealed class AgentToolCall
src/AURA.AI/AgentChat.cs:39:        public List<AgentToolCall>? ToolCalls { get; set; }
src/AURA.AI/AgentChat.cs:44:    /// <summary>Evento emitido pelo AgentSession a cada ferramenta executada (para a UI).</summary>
src/AURA.AI/ProviderCatalog.cs:25:    public static class ProviderCatalog
src/AURA.AI/ProviderCatalog.cs:37:                    Name = "OpenRouter",
src/AURA.AI/ProviderCatalog.cs:38:                    BaseUrl = "https://openrouter.ai/api/v1/chat/completions",
src/AURA.AI/ProviderCatalog.cs:46:                        new() { Id = "openrouter/free", Label = "Auto (qualquer grátis)", Category = "Grátis", IsFree = true },
src/AURA.AI/ProviderCatalog.cs:93:                    Name = "Ollama (local)",
src/AURA.AI/AiAssistantService.cs:14:    /// <br/>Pipeline: Client App → (AiAssistant) → OpenRouterClient → OpenRouter API.
src/AURA.AI/AiAssistantService.cs:18:    public static class AiAssistantService
src/AURA.AI/AiAssistantService.cs:20:        public static readonly OpenRouterOptions DefaultOptions = new OpenRouterOptions
src/AURA.AI/AiAssistantService.cs:22:            ApiKey = Environment.GetEnvironmentVariable("AURA_OPENROUTER_KEY") ?? "ollama",
src/AURA.AI/AiAssistantService.cs:28:        public static async Task<string> AskAsync(string question, MemoryStore? memory = null, ILogger? logger = null, OpenRouterOptions? options = null, HttpClient? http = null)
src/AURA.AI/AiAssistantService.cs:34:            OpenRouterOptions opt = options ?? DefaultOptions;
src/AURA.AI/AiAssistantService.cs:36:                throw new InvalidOperationException("API key não configurada. Defina a variável de ambiente AURA_OPENROUTER_KEY.");
src/AURA.AI/AgentTool.cs:9:    public sealed class AgentToolParameter
src/AURA.AI/AgentTool.cs:17:    public sealed class AgentToolDefinition
src/AURA.AI/AgentTool.cs:23:        public Dictionary<string, AgentToolParameter> Parameters { get; } = new();
src/AURA.AI/AgentTool.cs:32:    public abstract class AgentTool
src/AURA.AI/AgentTool.cs:34:        public abstract AgentToolDefinition Definition { get; }
src/AURA.AI/AgentTools/ShellAgentTool.cs:15:    public sealed class ShellAgentTool : AgentTool
src/AURA.AI/AgentTools/ShellAgentTool.cs:23:        public ShellAgentTool(string workspaceRoot)
src/AURA.AI/AgentTools/ShellAgentTool.cs:29:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/ShellAgentTool.cs:36:                ["command"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:11:    public sealed class ListDirTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:17:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:23:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:71:    public sealed class ReadFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:77:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:83:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:112:    public sealed class WriteFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:118:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:124:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:129:                ["content"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:157:    public sealed class EditFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:163:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:169:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:174:                ["old_text"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:179:                ["new_text"] = new AgentToolParameter
src/AURA.AI/AgentTools/WorkspaceAgentTool.cs:11:    public abstract class WorkspaceAgentTool : AgentTool
src/AURA.AI/AgentTools/WorkspaceAgentTool.cs:13:        protected WorkspaceAgentTool(string workspaceRoot)
src/AURA.CLI/Program.cs:36:        private static OpenRouterClient _aiClient;
src/AURA.CLI/Program.cs:381:            OpenRouterClient client = EnsureAiClient(model);
src/AURA.CLI/Program.cs:387:                string answer = client.ChatAsync(question).GetAwaiter().GetResult();
src/AURA.CLI/Program.cs:417:            OpenRouterClient client = EnsureAiClient();
src/AURA.CLI/Program.cs:421:            var tools = new System.Collections.Generic.List<AgentTool>
src/AURA.CLI/Program.cs:427:                new ShellAgentTool(workspace)
src/AURA.CLI/Program.cs:432:            var session = new AgentSession(client, tools, systemPrompt);
src/AURA.CLI/Program.cs:465:                Console.WriteLine("     Ou defina a variável OPENROUTER_API_KEY.");
src/AURA.CLI/Program.cs:487:        private static OpenRouterClient EnsureAiClient(string? model = null)
src/AURA.CLI/Program.cs:501:                ?? "ollama";
src/AURA.CLI/Program.cs:505:            if (provider == "ollama")
src/AURA.CLI/Program.cs:507:                _aiClient = new OpenRouterClient(
src/AURA.CLI/Program.cs:508:                    new OpenRouterOptions
src/AURA.CLI/Program.cs:510:                        Provider = "ollama",
src/AURA.CLI/Program.cs:524:                Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
src/AURA.CLI/Program.cs:537:            _aiClient = new OpenRouterClient(
src/AURA.CLI/Program.cs:538:                new OpenRouterOptions
src/AURA.CLI/Program.cs:540:                    Provider = "openrouter",
src/AURA.CLI/Program.cs:546:                    AppReference = "AURA-Ollama"
src/AURA.CLI/Program.cs:808:            Console.WriteLine("  chat \"pergunta\"          Pergunta direta à IA (OpenRouter) [--model x]");
src/AURA.Agents/AgentManager.cs:53:                    Description = "aichat CLI (OpenRouter)",
src/AURA.Mobile/Pages/LogsPage.xaml.cs:10:    private readonly OpenRouterClient _client;
src/AURA.Mobile/Pages/LogsPage.xaml.cs:12:    public LogsPage(OpenRouterClient client)
src/AURA.Mobile/Pages/LogsPage.xaml.cs:86:            sb.AppendLine($"Chave OpenRouter: {(hasKey ? "configurada (" + _client.Options.ApiKey.Length + " chars)" : "AUSENTE — defina na aba Assistente")}");
src/AURA.Mobile/Pages/LogsPage.xaml.cs:102:            // 2. DNS/HTTPS até a base da OpenRouter.
src/AURA.Mobile/Pages/LogsPage.xaml.cs:121:            string modelEcho = await _client.ChatAsync(
src/AURA.Mobile/Pages/LogsPage.xaml.cs:163:            LogViewer.Text = "Configure a chave OpenRouter na aba Assistente primeiro.";
src/AURA.Mobile/Pages/LogsPage.xaml.cs:189:            string analysis = await _client.ChatAsync(logContent, systemPrompt: systemPrompt);
src/AURA.Mobile/Pages/AgentPage.xaml.cs:8:    private readonly OpenRouterClient _client;
src/AURA.Mobile/Pages/AgentPage.xaml.cs:9:    private AgentSession? _session;
src/AURA.Mobile/Pages/AgentPage.xaml.cs:11:    public AgentPage(OpenRouterClient client)
src/AURA.Mobile/Pages/AgentPage.xaml.cs:40:        var tools = new List<AgentTool>
src/AURA.Mobile/Pages/AgentPage.xaml.cs:46:            new ShellAgentTool(root)
src/AURA.Mobile/Pages/AgentPage.xaml.cs:60:        _session = new AgentSession(_client, tools, systemPrompt);
src/AURA.Mobile/Pages/ChatPage.xaml.cs:8:    private readonly OpenRouterClient _client;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:11:    public ChatPage(OpenRouterClient client, AURA.Memory.MemoryStore memory)
src/AURA.Mobile/Pages/ChatPage.xaml.cs:30:            ProviderPicker.ItemsSource = ProviderCatalog.Providers;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:34:        for (int i = 0; i < ProviderCatalog.Providers.Count; i++)
src/AURA.Mobile/Pages/ChatPage.xaml.cs:36:            if (string.Equals(ProviderCatalog.Providers[i].Name, savedProvider, StringComparison.OrdinalIgnoreCase))
src/AURA.Mobile/Pages/ChatPage.xaml.cs:147:        if (string.IsNullOrWhiteSpace(apiKey) && (_client.Options.BaseUrl.Contains("openrouter") ||
src/AURA.Mobile/Pages/ChatPage.xaml.cs:162:            var assistant = new AiAssistant(_client, _memory);
src/AURA.Mobile/Pages/FixesPage.xaml.cs:8:    private readonly OpenRouterClient _client;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:11:    public FixesPage(OpenRouterClient client)
src/AURA.Mobile/Pages/FixesPage.xaml.cs:63:            "openrouter/free, openai/gpt-oss-20b:free, google/gemma-4-26b-a4b-it:free, " +
src/AURA.Mobile/Pages/FixesPage.xaml.cs:73:            string answer = await _client.ChatAsync(question, systemPrompt: systemPrompt);
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:8:    /// imediatamente no OpenRouterClient.
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:48:        public static void Apply(OpenRouterClient client)
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:50:            ProviderInfo provider = ProviderCatalog.Find(Provider);
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:54:            // cai para o primeiro do provedor (evita mandar ID de outra API, ex. Groq na OpenRouter).
src/AURA.Mobile/MauiProgram.cs:59:        // IA (OpenRouter) — mesma stack do AURA.AI usado no CLI.
src/AURA.Mobile/MauiProgram.cs:60:        builder.Services.AddSingleton(sp => new OpenRouterClient(new OpenRouterOptions
src/AURA.Mobile/MauiProgram.cs:63:            BaseUrl = "https://openrouter.ai/api/v1/chat/completions",
src/AURA.Mobile/MauiProgram.cs:67:        builder.Services.AddSingleton<AiAssistant>();
src/AURA.Modules/ModuleCatalog.cs:82:                ShortDescription = "Assistente inteligente: chat com a IA (OpenRouter) e agente de arquivos com ferramentas.",
src/AURA.Modules/ModuleCatalog.cs:87:                Includes = new List<string> { "OpenRouterClient", "AgentManager" },
src/AURA.Modules/ModuleCatalog.cs:90:                    "Chat direto com modelo OpenRouter",
```

## Referências entre componentes

```text
src/AURA.AI/AgentSession.cs:88:                AgentChatResponse response = await _client.ChatToolsAsync(
src/AURA.AI/AiAssistant.cs:33:            string answer = await _client.ChatAsync(question, httpClient, ct).ConfigureAwait(false);
src/AURA.AI/OpenRouterClient.cs:20:    public sealed class OpenRouterOptions
src/AURA.AI/OpenRouterClient.cs:33:    /// (testável sem rede) com BuildRequest; execute com ChatAsync.
src/AURA.AI/OpenRouterClient.cs:39:        public OpenRouterOptions Options { get; }
src/AURA.AI/OpenRouterClient.cs:41:        public OpenRouterClient(OpenRouterOptions options, ILogger? logger = null)
src/AURA.AI/OpenRouterClient.cs:89:        public async Task<string> ChatAsync(string question,
src/AURA.AI/OpenRouterClient.cs:135:        public async Task<AgentChatResponse> ChatToolsAsync(
src/AURA.AI/OpenRouterClient.cs:295:                                calls.Add(new AgentToolCall
src/AURA.AI/OpenRouterClient.cs:353:                    "ApiKey do provedor LLM não configurada. Defina OpenRouterOptions.ApiKey.");
src/AURA.AI/OpenRouterClient.cs:428:                    new AgentToolCall
src/AURA.AI/AiAssistantService.cs:20:        public static readonly OpenRouterOptions DefaultOptions = new OpenRouterOptions
src/AURA.AI/AiAssistantService.cs:28:        public static async Task<string> AskAsync(string question, MemoryStore? memory = null, ILogger? logger = null, OpenRouterOptions? options = null, HttpClient? http = null)
src/AURA.AI/AiAssistantService.cs:34:            OpenRouterOptions opt = options ?? DefaultOptions;
src/AURA.AI/AgentTools/ShellAgentTool.cs:29:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/ShellAgentTool.cs:36:                ["command"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:17:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:23:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:77:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:83:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:118:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:124:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:129:                ["content"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:163:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:169:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:174:                ["old_text"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:179:                ["new_text"] = new AgentToolParameter
src/AURA.CLI/Program.cs:387:                string answer = client.ChatAsync(question).GetAwaiter().GetResult();
src/AURA.CLI/Program.cs:432:            var session = new AgentSession(client, tools, systemPrompt);
src/AURA.CLI/Program.cs:507:                _aiClient = new OpenRouterClient(
src/AURA.CLI/Program.cs:508:                    new OpenRouterOptions
src/AURA.CLI/Program.cs:537:            _aiClient = new OpenRouterClient(
src/AURA.CLI/Program.cs:538:                new OpenRouterOptions
src/AURA.Mobile/Pages/LogsPage.xaml.cs:121:            string modelEcho = await _client.ChatAsync(
src/AURA.Mobile/Pages/LogsPage.xaml.cs:189:            string analysis = await _client.ChatAsync(logContent, systemPrompt: systemPrompt);
src/AURA.Mobile/Pages/AgentPage.xaml.cs:60:        _session = new AgentSession(_client, tools, systemPrompt);
src/AURA.Mobile/Pages/FixesPage.xaml.cs:73:            string answer = await _client.ChatAsync(question, systemPrompt: systemPrompt);
src/AURA.Mobile/MauiProgram.cs:60:        builder.Services.AddSingleton(sp => new OpenRouterClient(new OpenRouterOptions
```

## Providers

```text
src/AURA.AI/AgentSession.cs:14:    /// Loop agêntico sobre o OpenRouterClient: envia a conversa com as
src/AURA.AI/AgentSession.cs:23:        private readonly OpenRouterClient _client;
src/AURA.AI/AgentSession.cs:34:        public AgentSession(OpenRouterClient client, IEnumerable<AgentTool> tools,
src/AURA.AI/AiAssistant.cs:17:        private readonly OpenRouterClient _client;
src/AURA.AI/AiAssistant.cs:21:        public AiAssistant(OpenRouterClient client, MemoryStore memory, ILogger? logger = null)
src/AURA.AI/OpenRouterClient.cs:18:    /// o config do aichat (OpenRouter, qwen/qwen-plus).
src/AURA.AI/OpenRouterClient.cs:20:    public sealed class OpenRouterOptions
src/AURA.AI/OpenRouterClient.cs:22:        public string Provider { get; set; } = "openrouter";
src/AURA.AI/OpenRouterClient.cs:23:        public string ApiKey { get; set; } = string.Empty;
src/AURA.AI/OpenRouterClient.cs:32:    /// Cliente mínimo para OpenRouter chat completions. Construa a requisição
src/AURA.AI/OpenRouterClient.cs:35:    public sealed class OpenRouterClient
src/AURA.AI/OpenRouterClient.cs:39:        public OpenRouterOptions Options { get; }
src/AURA.AI/OpenRouterClient.cs:41:        public OpenRouterClient(OpenRouterOptions options, ILogger? logger = null)
src/AURA.AI/OpenRouterClient.cs:64:                model = Options.Model,
src/AURA.AI/OpenRouterClient.cs:72:            if (!string.Equals(Options.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
src/AURA.AI/OpenRouterClient.cs:76:                    "Bearer " + Options.ApiKey);
src/AURA.AI/OpenRouterClient.cs:92:            EnsureValidApiKey();
src/AURA.AI/OpenRouterClient.cs:142:            EnsureValidApiKey();
src/AURA.AI/OpenRouterClient.cs:242:            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + Options.ApiKey);
src/AURA.AI/OpenRouterClient.cs:304:                        // Ollama/Qwen pequeno pode retornar a chamada de ferramenta
src/AURA.AI/OpenRouterClient.cs:337:        private void EnsureValidApiKey()
src/AURA.AI/OpenRouterClient.cs:339:            if (string.Equals(Options.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
src/AURA.AI/OpenRouterClient.cs:344:                        "Endpoint do Ollama não configurado.");
src/AURA.AI/OpenRouterClient.cs:350:            if (string.IsNullOrWhiteSpace(Options.ApiKey))
src/AURA.AI/OpenRouterClient.cs:353:                    "ApiKey do provedor LLM não configurada. Defina OpenRouterOptions.ApiKey.");
src/AURA.AI/OpenRouterClient.cs:356:            if (Options.ApiKey.Length > 200 ||
src/AURA.AI/OpenRouterClient.cs:357:                Options.ApiKey.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
src/AURA.AI/OpenRouterClient.cs:430:                        Id = "ollama-tool-" + Guid.NewGuid().ToString("N"),
src/AURA.AI/AgentChat.cs:6:    /// Uma mensagem da conversa do agente, no protocolo OpenAI-compatível
src/AURA.AI/ProviderCatalog.cs:37:                    Name = "OpenRouter",
src/AURA.AI/ProviderCatalog.cs:38:                    BaseUrl = "https://openrouter.ai/api/v1/chat/completions",
src/AURA.AI/ProviderCatalog.cs:46:                        new() { Id = "openrouter/free", Label = "Auto (qualquer grátis)", Category = "Grátis", IsFree = true },
src/AURA.AI/ProviderCatalog.cs:47:                        new() { Id = "openai/gpt-oss-20b:free", Label = "GPT-OSS 20B", Category = "Grátis", IsFree = true },
src/AURA.AI/ProviderCatalog.cs:48:                        new() { Id = "google/gemma-4-26b-a4b-it:free", Label = "Gemma 4 26B", Category = "Grátis", IsFree = true },
src/AURA.AI/ProviderCatalog.cs:55:                    Name = "Groq (grátis)",
src/AURA.AI/ProviderCatalog.cs:56:                    BaseUrl = "https://api.groq.com/openai/v1/chat/completions",
src/AURA.AI/ProviderCatalog.cs:70:                    BaseUrl = "https://api.cerebras.ai/v1/chat/completions",
src/AURA.AI/ProviderCatalog.cs:81:                    Name = "Google Gemini",
src/AURA.AI/ProviderCatalog.cs:82:                    BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
src/AURA.AI/ProviderCatalog.cs:93:                    Name = "Ollama (local)",
src/AURA.AI/ProviderCatalog.cs:94:                    BaseUrl = "http://localhost:11434/v1/chat/completions",
src/AURA.AI/AiAssistantService.cs:14:    /// <br/>Pipeline: Client App → (AiAssistant) → OpenRouterClient → OpenRouter API.
src/AURA.AI/AiAssistantService.cs:20:        public static readonly OpenRouterOptions DefaultOptions = new OpenRouterOptions
src/AURA.AI/AiAssistantService.cs:22:            ApiKey = Environment.GetEnvironmentVariable("AURA_OPENROUTER_KEY") ?? "ollama",
src/AURA.AI/AiAssistantService.cs:23:            BaseUrl = "http://127.0.0.1:11434/v1/chat/completions",
src/AURA.AI/AiAssistantService.cs:24:            Model = "qwen2.5-coder:1.5b"
src/AURA.AI/AiAssistantService.cs:28:        public static async Task<string> AskAsync(string question, MemoryStore? memory = null, ILogger? logger = null, OpenRouterOptions? options = null, HttpClient? http = null)
src/AURA.AI/AiAssistantService.cs:34:            OpenRouterOptions opt = options ?? DefaultOptions;
src/AURA.AI/AiAssistantService.cs:35:            if (string.IsNullOrWhiteSpace(opt.ApiKey))
src/AURA.AI/AiAssistantService.cs:36:                throw new InvalidOperationException("API key não configurada. Defina a variável de ambiente AURA_OPENROUTER_KEY.");
src/AURA.AI/AiAssistantService.cs:46:                model = opt.Model,
src/AURA.AI/AiAssistantService.cs:53:            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opt.ApiKey);
```

## Ferramentas do agente

```text
```

## Configuração

```text
```

## Testes

```text
```

## Resumo de tamanho

```text
Projetos:
12
Arquivos C#:
129
Linhas C#:
13674
```

## Observações

- Este relatório não inclui valores de .
- Não deve conter tokens, senhas ou chaves privadas.
-  é um artefato temporário de análise.
- A coleta não modifica o código-fonte.
