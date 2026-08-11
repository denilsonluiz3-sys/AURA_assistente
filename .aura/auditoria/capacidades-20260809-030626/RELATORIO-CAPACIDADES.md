# AURA — MAPA DE CAPACIDADES REAIS

Data: Sun Aug  9 03:06:26 -03 2026
Branch: feat/project-access
Commit: 2969a70

Objetivo: identificar capacidades existentes, pontos de entrada, dependências e sinais de integração antes de remover ou duplicar código.

Este relatório é análise estática. Uma referência encontrada não prova que o fluxo foi executado com sucesso.

## 1. Entradas e orquestração

### AgentSession e loop
src/AURA.AI/AgentSession.cs:19:    public sealed class AgentSession
src/AURA.AI/AgentSession.cs:45:        public async Task<string> RunAsync(string userText,
src/AURA.AI/AgentSession.cs:58:                AgentChatResponse response = await _client.ChatToolsAsync(
src/AURA.AI/OpenRouterClient.cs:135:        public async Task<AgentChatResponse> ChatToolsAsync(
src/AURA.Core/Launchers/Runner.cs:64:        public async System.Threading.Tasks.Task<Cell> RunAsync(SimulationRuntime runtime, string id, string filePath,
src/AURA.CLI/Program.cs:251:            Cell cell = _runner.RunAsync(_runtime, cellId, filePath, arguments, null, limits.IsEmpty ? null : limits).GetAwaiter().GetResult();
src/AURA.CLI/Program.cs:432:            var session = new AgentSession(client, tools, systemPrompt);
src/AURA.CLI/Program.cs:448:                string answer = session.RunAsync(instruction).GetAwaiter().GetResult();
src/AURA.Mobile/Pages/AgentPage.xaml.cs:60:        _session = new AgentSession(_client, tools, systemPrompt);
src/AURA.Mobile/Pages/AgentPage.xaml.cs:122:            string answer = await _session!.RunAsync(text);
src/AURA.Mobile/Pages/RunPage.xaml.cs:105:                cell = await _runner.RunAsync(_runtime, id, _filePath!, args,
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:72:    Task<PipelineReport> RunAsync(
src/AURA.Modules/Executors/PythonExecutor.cs:24:        return RunAsync(binary, args, request, cancellationToken);
src/AURA.Modules/Executors/NodeExecutor.cs:23:        return RunAsync(binary, args, request, cancellationToken);
src/AURA.Modules/Executors/GitExecutor.cs:25:        return RunAsync(binary, args, request, cancellationToken);
src/AURA.Modules/Executors/ProcessExecutorBase.cs:20:    protected static async Task<ExecutionResult> RunAsync(
src/AURA.Modules/Executors/ShellExecutor.cs:23:        return RunAsync("/bin/sh", new[] { "-c", fullCommand }, request, cancellationToken);
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:31:        return RunAsync(request.Command, request.Arguments, request, cancellationToken);
src/AURA.Modules/Runtime/RuntimeManager.cs:37:    public async Task<PipelineReport> RunAsync(

### Pontos que criam AgentSession
src/AURA.CLI/Program.cs:432:            var session = new AgentSession(client, tools, systemPrompt);
src/AURA.Mobile/Pages/AgentPage.xaml.cs:60:        _session = new AgentSession(_client, tools, systemPrompt);


## 2. Ferramentas

### AgentTool
src/AURA.AI/AgentSession.cs:147:                return await tool.ExecuteAsync(
src/AURA.AI/AgentChat.cs:22:    public sealed class AgentToolCall
src/AURA.AI/AgentTool.cs:9:    public sealed class AgentToolParameter
src/AURA.AI/AgentTool.cs:17:    public sealed class AgentToolDefinition
src/AURA.AI/AgentTool.cs:32:    public abstract class AgentTool
src/AURA.AI/AgentTool.cs:36:        public abstract Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default);
src/AURA.AI/AgentTools/ShellAgentTool.cs:15:    public sealed class ShellAgentTool : AgentTool
src/AURA.AI/AgentTools/ShellAgentTool.cs:45:        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/FileTools.cs:11:    public sealed class ListDirTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:31:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/FileTools.cs:71:    public sealed class ReadFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:92:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/FileTools.cs:112:    public sealed class WriteFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:138:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/FileTools.cs:157:    public sealed class EditFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:188:        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
src/AURA.AI/AgentTools/WorkspaceAgentTool.cs:11:    public abstract class WorkspaceAgentTool : AgentTool
src/AURA.Installer/PythonInstaller.cs:64:        var execResult = await _pythonExecutor.ExecuteAsync(request, cancellationToken);
src/AURA.CLI/Program.cs:348:            ExecutionResult result = executor.ExecuteAsync(request).GetAwaiter().GetResult();
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:124:            ExecutionResult result = await _shell.ExecuteAsync(request);
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:83:            ExecutionResult result = await executor.ExecuteAsync(request);
src/AURA.Abstractions/Execution/IToolExecutor.cs:18:        Task<ExecutionResult> ExecuteAsync(
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:59:    Task<IReadOnlyList<string>> ExecuteAsync(
src/AURA.Modules/Executors/PythonExecutor.cs:16:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Executors/NodeExecutor.cs:15:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Executors/GitExecutor.cs:17:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Executors/ProcessExecutorBase.cs:13:public abstract class ProcessExecutorBase : IToolExecutor
src/AURA.Modules/Executors/ProcessExecutorBase.cs:17:    public abstract Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default);
src/AURA.Modules/Executors/ShellExecutor.cs:14:    public override Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
src/AURA.Modules/Runtime/Installer.cs:42:    public async Task<IReadOnlyList<string>> ExecuteAsync(
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:27:    public override Task<ExecutionResult> ExecuteAsync(
src/AURA.Modules/Runtime/RuntimeManager.cs:122:            IReadOnlyList<string> results = await _installer.ExecuteAsync(
src/AURA.Modules/Runtime/RuntimeManager.cs:139:        report.Outcome = await ExecuteAsync(report, args, timeout, workdir, cancellationToken);
src/AURA.Modules/Runtime/RuntimeManager.cs:175:    private static async Task<ExecutionOutcome> ExecuteAsync(
src/AURA.Modules/Runtime/RuntimeManager.cs:212:        ExecutionResult result = await executor.ExecuteAsync(request, cancellationToken);

### Ferramentas básicas
src/AURA.AI/AgentTools/ShellAgentTool.cs:31:            Name = "run_shell",
src/AURA.AI/AgentTools/FileTools.cs:19:            Name = "list_dir",
src/AURA.AI/AgentTools/FileTools.cs:79:            Name = "read_file",
src/AURA.AI/AgentTools/FileTools.cs:120:            Name = "write_file",
src/AURA.AI/AgentTools/FileTools.cs:165:            Name = "edit_file",


## 3. Memória

### MemoryStore
src/AURA.AI/AiAssistant.cs:31:            _memory.Append(MemoryEntry.Question(question));
src/AURA.AI/AiAssistant.cs:34:            _memory.Append(MemoryEntry.Answer(answer));
src/AURA.AI/OpenRouterClient.cs:472:                        sb.Append(text.GetString());
src/AURA.AI/AiAssistantService.cs:40:                memory.Append(MemoryEntry.Question(question));
src/AURA.AI/AiAssistantService.cs:71:                memory.Append(MemoryEntry.Answer(answer));
src/AURA.AI/AgentTools/ShellAgentTool.cs:107:                result.Append("stderr: ").AppendLine(stderr.ToString().TrimEnd());
src/AURA.Memory/MemoryStore.cs:19:    public sealed class MemoryStore
src/AURA.Core/Runtime/PluginWatcher.cs:172:            _plugins.Clear();
src/AURA.Core/Runtime/PluginWatcher.cs:173:            _pluginPaths.Clear();
src/AURA.Core/Runtime/SimulationRuntime.cs:697:            _cells.Clear();
src/AURA.Core/Runtime/SimulationRuntime.cs:698:            _processes.Clear();
src/AURA.Mobile/MainPage.cs:88:            Children.Clear();
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:57:        OutputStack.Children.Clear();
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:78:            OutputStack.Children.Clear();
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:33:            var entries = await Task.Run(() => _memoryStore.Read(64));
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:34:            Entries.Clear();
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:47:            Entries.Clear();
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:60:        await Task.Run(() => _memoryStore.Clear());
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:258:            TabBar.Children.Clear();
src/AURA.Mobile/Platforms/Android/AuraLog.cs:79:                            PendingBuffer.Clear();
src/AURA.Mobile/MauiProgram.cs:55:        builder.Services.AddSingleton(sp => new MemoryStore(
src/AURA.Mobile/MauiProgram.cs:119:                memory.Append(MemoryEntry.CellStateChange(evt.CellId, evt.To)));
src/AURA.Modules/Runtime/LanguageDetector.cs:141:            int count = reader.Read(buffer, 0, limit);
src/AURA.Modules/Runtime/LanguageDetector.cs:156:            int count = stream.Read(chunk, 0, chunk.Length);
src/AURA.Modules/Runtime/LanguageDetector.cs:175:            int count = stream.Read(buffer, 0, length);

### SolutionStore
src/AURA.AI/AgentSession.cs:37:            _solutionStore = new SolutionStore();
src/AURA.AI/AgentSession.cs:109:        private SolutionRule? TryGetKnownSolution(
src/AURA.AI/AgentSession.cs:117:            return _solutionStore.Find(
src/AURA.Memory/SolutionStore.cs:17:    public sealed class SolutionStore
src/AURA.Memory/SolutionStore.cs:71:        public void SaveValidated(SolutionRule rule)
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:50:            ProviderInfo provider = ProviderCatalog.Find(Provider);

### RequestContext
src/AURA.Memory/RequestContext.cs:11:    public sealed class RequestContext


## 4. Execução

### Executores
src/AURA.AI/AgentTools/ShellAgentTool.cs:63:            var psi = new ProcessStartInfo
src/AURA.AI/AgentTools/ShellAgentTool.cs:84:                process.Start();
src/AURA.Installer/PythonInstaller.cs:8:/// o PythonExecutor já existente (evita duplicar lógica de resolução de
src/AURA.Installer/PythonInstaller.cs:14:    private readonly IToolExecutor _pythonExecutor;
src/AURA.Installer/PythonInstaller.cs:16:    public PythonInstaller() : this(new PythonExecutor()) { }
src/AURA.Installer/PythonInstaller.cs:18:    /// <summary>Construtor para testes: permite injetar um executor falso.</summary>
src/AURA.Installer/PythonInstaller.cs:19:    public PythonInstaller(IToolExecutor pythonExecutor)
src/AURA.Installer/PythonInstaller.cs:21:        _pythonExecutor = pythonExecutor;
src/AURA.Installer/PythonInstaller.cs:49:        if (!_pythonExecutor.IsAvailable())
src/AURA.Installer/PythonInstaller.cs:64:        var execResult = await _pythonExecutor.ExecuteAsync(request, cancellationToken);
src/AURA.Installer/PythonEnvironmentSelector.cs:8:/// Etapa 3 para artefatos Python: reaproveita <see cref="PythonExecutor.IsAvailable"/>
src/AURA.Installer/PythonEnvironmentSelector.cs:21:    private readonly IToolExecutor _pythonExecutor;
src/AURA.Installer/PythonEnvironmentSelector.cs:25:        : this(new PythonExecutor(), () => new SystemAnalyzer().Analyze())
src/AURA.Installer/PythonEnvironmentSelector.cs:29:    /// <summary>Construtor para testes: permite injetar um executor e diagnósticos falsos.</summary>
src/AURA.Installer/PythonEnvironmentSelector.cs:30:    public PythonEnvironmentSelector(IToolExecutor pythonExecutor, Func<SystemDiagnosticsResult> diagnosticsProvider)
src/AURA.Installer/PythonEnvironmentSelector.cs:32:        _pythonExecutor = pythonExecutor;
src/AURA.Installer/PythonEnvironmentSelector.cs:41:        bool runtimeAvailable = _pythonExecutor.IsAvailable();
src/AURA.Installer/PythonEnvironmentSelector.cs:51:            RuntimeBinary = runtimeAvailable ? _pythonExecutor.Name : null,
src/AURA.Core/Configuration/ModulesConfiguration.cs:51:                case "executors": return Executors;
src/AURA.Core/Configuration/ModulesConfiguration.cs:74:                case "executors": Executors = value; break;
src/AURA.Core/Runtime/SimulationRuntime.cs:178:                process.Start();
src/AURA.Core/Runtime/SimulationRuntime.cs:521:            var psi = new ProcessStartInfo
src/AURA.Core/Events/AuraEvents.cs:41:    public sealed class ExecutorCompletedEvent : IEvent
src/AURA.CLI/Program.cs:32:        private static readonly ShellExecutor Shell = new();
src/AURA.CLI/Program.cs:33:        private static readonly GitExecutor Git = new();
src/AURA.CLI/Program.cs:34:        private static readonly PythonExecutor Python = new();
src/AURA.CLI/Program.cs:35:        private static readonly NodeExecutor Node = new();
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:8:    private readonly ShellExecutor _shell;
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:13:    public TerminalPage(ShellExecutor shell)
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:7:public partial class ExecutorsPage : ContentPage
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:9:    private readonly ShellExecutor _shell;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:10:    private readonly GitExecutor _git;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:11:    private readonly PythonExecutor _python;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:12:    private readonly NodeExecutor _node;
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:15:    public ExecutorsPage(ShellExecutor shell, GitExecutor git, PythonExecutor python, NodeExecutor node, EventBus events)
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:139:public class ExecutorStatus
src/AURA.Mobile/MauiProgram.cs:77:        builder.Services.AddSingleton<ShellExecutor>();
src/AURA.Mobile/MauiProgram.cs:78:        builder.Services.AddSingleton<GitExecutor>();
src/AURA.Mobile/MauiProgram.cs:79:        builder.Services.AddSingleton<PythonExecutor>();
src/AURA.Mobile/MauiProgram.cs:80:        builder.Services.AddSingleton<NodeExecutor>();
src/AURA.Modules/Executors/PythonExecutor.cs:10:public sealed class PythonExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/NodeExecutor.cs:9:public sealed class NodeExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/GitExecutor.cs:11:public sealed class GitExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/ProcessExecutorBase.cs:13:public abstract class ProcessExecutorBase : IToolExecutor
src/AURA.Modules/Executors/ProcessExecutorBase.cs:26:        var psi = new ProcessStartInfo
src/AURA.Modules/Executors/ProcessExecutorBase.cs:56:            process.Start();
src/AURA.Modules/Executors/ShellExecutor.cs:8:public sealed class ShellExecutor : ProcessExecutorBase
src/AURA.Modules/ModuleCatalog.cs:139:                Includes = new List<string> { "ShellExecutor", "GitExecutor", "PythonExecutor", "NodeExecutor" },
src/AURA.Modules/Runtime/SyntaxValidator.cs:40:            var psi = new ProcessStartInfo
src/AURA.Modules/Runtime/SyntaxValidator.cs:52:            process.Start();
src/AURA.Modules/Runtime/Installer.cs:83:        var psi = new ProcessStartInfo
src/AURA.Modules/Runtime/Installer.cs:97:            process.Start();
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:12:public sealed class RuntimeProcessExecutor : ProcessExecutorBase
src/AURA.Modules/Runtime/CompatibilityChecker.cs:81:            var psi = new ProcessStartInfo
src/AURA.Modules/Runtime/CompatibilityChecker.cs:95:            process.Start();
src/AURA.Modules/Runtime/RuntimeResolver.cs:80:            var psi = new ProcessStartInfo
src/AURA.Modules/Runtime/RuntimeResolver.cs:96:            if (!process.Start()) return string.Empty;

### Launchers e Runtime
src/AURA.Memory/MemoryStore.cs:35:            _path = path ?? SimulationRuntime.ExpandUserHome("~/AURA/memory.json");
src/AURA.Memory/SolutionStore.cs:38:                SimulationRuntime.ExpandUserHome(
src/AURA.Core/Launchers/Runner.cs:14:    public sealed class Runner
src/AURA.Core/Launchers/Runner.cs:64:        public async System.Threading.Tasks.Task<Cell> RunAsync(SimulationRuntime runtime, string id, string filePath,
src/AURA.Core/Launchers/NodeLauncher.cs:10:    public sealed class NodeLauncher : ILauncher
src/AURA.Core/Launchers/CellCommand.cs:8:    /// arguments, ready to be passed to SimulationRuntime.CreateCell.
src/AURA.Core/Launchers/DllLauncher.cs:9:    public sealed class DllLauncher : ILauncher
src/AURA.Core/Launchers/PythonLauncher.cs:10:    public sealed class PythonLauncher : ILauncher
src/AURA.Core/Launchers/GoLauncher.cs:10:    public sealed class GoLauncher : ILauncher
src/AURA.Core/Launchers/JarLauncher.cs:10:    public sealed class JarLauncher : ILauncher
src/AURA.Core/Runtime/SimulationRuntime.cs:16:    /// The cell runtime (formerly "SimulationRuntime"). Each cell is backed
src/AURA.Core/Runtime/SimulationRuntime.cs:21:    public sealed class SimulationRuntime : IDisposable
src/AURA.Core/Runtime/SimulationRuntime.cs:39:        private readonly CellStore _store;
src/AURA.Core/Runtime/SimulationRuntime.cs:42:        public SimulationRuntime(ILogger logger)
src/AURA.Core/Runtime/SimulationRuntime.cs:47:        public SimulationRuntime(ILogger logger, string cellsRoot, ICellBackend backend)
src/AURA.Core/Runtime/SimulationRuntime.cs:52:        public SimulationRuntime(ILogger logger, string cellsRoot, ICellBackend backend, bool persist)
src/AURA.Core/Runtime/SimulationRuntime.cs:60:            _store = persist ? new CellStore(_logger, GetStorePath(_cellsRoot)) : null;
src/AURA.Core/Runtime/CellStore.cs:11:    /// recovers live processes (see SimulationRuntime.LoadFromStoreAsync).
src/AURA.Core/Runtime/CellStore.cs:13:    public sealed class CellStore
src/AURA.Core/Runtime/CellStore.cs:25:        public CellStore(ILogger logger, string path = null)
src/AURA.Core/Runtime/CellStore.cs:28:            _path = path ?? SimulationRuntime.ExpandUserHome("~/AURA/cells.json");
src/AURA.Core/Runtime/CellStore.cs:34:        public void Save(SimulationRuntime runtime)
src/AURA.Core/Runtime/CellStore.cs:46:                    var document = new CellStoreDocument
src/AURA.Core/Runtime/CellStore.cs:86:                CellStoreDocument document = JsonSerializer.Deserialize<CellStoreDocument>(json, Options);
src/AURA.Core/Runtime/CellStore.cs:97:        private sealed class CellStoreDocument
src/AURA.Core/Runtime/Cell.cs:46:            ? Path.Combine(SimulationRuntime.ExpandUserHome(SimulationRuntime.DefaultCellsRoot), Id)
src/AURA.CLI/Program.cs:26:        private static SimulationRuntime _runtime;
src/AURA.CLI/Program.cs:59:            _runtime = new SimulationRuntime(_logger);
src/AURA.Agents/AgentManager.cs:112:        public async Task<string> AskAsync(SimulationRuntime runtime, string question,
src/AURA.Agents/AgentManager.cs:175:        public Cell StartAssistantCell(SimulationRuntime runtime, string id, string assistantName = "aichat")
src/AURA.Agents/AgentManager.cs:205:        private static async Task WaitFinishedAsync(SimulationRuntime runtime, Cell cell)
src/AURA.Mobile/Pages/RunPage.xaml.cs:9:    private readonly SimulationRuntime _runtime;
src/AURA.Mobile/Pages/RunPage.xaml.cs:13:    public RunPage(SimulationRuntime runtime, Runner runner)
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:52:        private readonly SimulationRuntime _runtime;
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:60:        public BrowserPage(ImageSearchPage imageSearch, SimulationRuntime runtime, EventBus events)
src/AURA.Mobile/Pages/CellsPage.xaml.cs:9:    private readonly SimulationRuntime _runtime;
src/AURA.Mobile/Pages/CellsPage.xaml.cs:14:    public CellsPage(SimulationRuntime runtime, Runner runner, RunPage runPage)
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:10:    public static class RuntimeConfig
src/AURA.Mobile/MauiProgram.cs:84:        builder.Services.AddSingleton(sp => new SimulationRuntime(
src/AURA.Abstractions/Runtime/RuntimeModels.cs:25:public sealed class RuntimeResolution
src/AURA.Modules/ModuleCatalog.cs:188:                Includes = new List<string> { "SimulationRuntime", "Runner" },
src/AURA.Modules/Runtime/LanguageDetector.cs:11:public sealed class LanguageDetector : IRuntimeDetector
src/AURA.Modules/Runtime/Installer.cs:11:public sealed class Installer : IRuntimeInstaller
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:12:public sealed class RuntimeProcessExecutor : ProcessExecutorBase
src/AURA.Modules/Runtime/RuntimeCatalog.cs:11:public static class RuntimeCatalog
src/AURA.Modules/Runtime/RuntimeManager.cs:12:public sealed class RuntimeManager : IRuntimeManager
src/AURA.Modules/Runtime/RuntimeResolver.cs:12:public sealed class RuntimeResolver : IRuntimeResolver


## 5. Análise e diagnóstico

### Diagnóstico
src/AURA.SystemInfo/SystemDiagnosticsResult.cs:6:    public class SystemDiagnosticsResult
src/AURA.SystemInfo/SystemAnalyzer.cs:8:    /// Collects basic system diagnostics (OS, architecture, CPU, RAM, disk)
src/AURA.SystemInfo/SystemAnalyzer.cs:12:    public sealed class SystemAnalyzer
src/AURA.SystemInfo/SystemAnalyzer.cs:16:        public SystemDiagnosticsResult Analyze()
src/AURA.SystemInfo/SystemAnalyzer.cs:18:            var result = new SystemDiagnosticsResult
src/AURA.SystemInfo/SystemAnalyzer.cs:37:        private static void ReadMemory(SystemDiagnosticsResult result)
src/AURA.SystemInfo/SystemAnalyzer.cs:59:        private static void ReadWindowsMemory(SystemDiagnosticsResult result)
src/AURA.SystemInfo/SystemAnalyzer.cs:71:        private static void ReadLinuxMemory(SystemDiagnosticsResult result)
src/AURA.SystemInfo/SystemAnalyzer.cs:99:        private static void ReadDisk(SystemDiagnosticsResult result)
src/AURA.AI/AgentTools/ShellAgentTool.cs:2:using System.Diagnostics;
src/AURA.Installer/EnvironmentSelectionResult.cs:22:    public SystemDiagnosticsResult SystemDiagnostics { get; set; } = null!;
src/AURA.Installer/PythonEnvironmentSelector.cs:9:/// pra checar o runtime e o <see cref="SystemAnalyzer"/> pra checar disco livre.
src/AURA.Installer/PythonEnvironmentSelector.cs:22:    private readonly Func<SystemDiagnosticsResult> _diagnosticsProvider;
src/AURA.Installer/PythonEnvironmentSelector.cs:25:        : this(new PythonExecutor(), () => new SystemAnalyzer().Analyze())
src/AURA.Installer/PythonEnvironmentSelector.cs:30:    public PythonEnvironmentSelector(IToolExecutor pythonExecutor, Func<SystemDiagnosticsResult> diagnosticsProvider)
src/AURA.Installer/PythonEnvironmentSelector.cs:33:        _diagnosticsProvider = diagnosticsProvider;
src/AURA.Installer/PythonEnvironmentSelector.cs:40:        var diagnostics = _diagnosticsProvider();
src/AURA.Installer/PythonEnvironmentSelector.cs:44:        double freeMb = diagnostics.FreeDiskSpaceGb * 1024.0;
src/AURA.Installer/PythonEnvironmentSelector.cs:52:            SystemDiagnostics = diagnostics,
src/AURA.Core/Runtime/SimulationRuntime.cs:4:using System.Diagnostics;
src/AURA.CLI/Program.cs:135:                        PrintDiagnostics();
src/AURA.CLI/Program.cs:739:        private static void PrintDiagnostics()
src/AURA.CLI/Program.cs:741:            SystemDiagnosticsResult result = new SystemAnalyzer().Analyze();
src/AURA.Agents/AgentManager.cs:3:using System.Diagnostics;
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs:1:using AURA.Mobile.Diagnostics;
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs:7:        private readonly List<ImageSearchProvider> _providers = SearchCatalog.ImageProviders;
src/AURA.Mobile/Pages/LogsPage.xaml.cs:4:using AURA.Mobile.Diagnostics;
src/AURA.Mobile/Pages/AgentPage.xaml.cs:2:using AURA.Mobile.Diagnostics;
src/AURA.Mobile/Pages/AgentPage.xaml.cs:24:        WorkspaceLabel.Text = ProjectAccessService.StatusText + "\n" +
src/AURA.Mobile/Pages/AgentPage.xaml.cs:73:            bool linked = await ProjectAccessService.LinkAsync();
src/AURA.Mobile/Pages/AgentPage.xaml.cs:80:            WorkspaceLabel.Text = ProjectAccessService.StatusText + "\n" +
src/AURA.Mobile/Pages/AgentPage.xaml.cs:125:            if (ProjectAccessService.IsLinked)
src/AURA.Mobile/Pages/AgentPage.xaml.cs:127:                int synced = await ProjectAccessService.SyncBackAsync();
src/AURA.Mobile/Pages/ChatPage.xaml.cs:2:using AURA.Mobile.Diagnostics;
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:1:using AURA.Mobile.Diagnostics;
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:50:        private readonly List<SearchEngine> _engines = SearchCatalog.Engines;
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:555:            string[] names = SearchCatalog.ImageProviders.Select(p => p.Name).ToArray();
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:557:            ImageSearchProvider? provider = SearchCatalog.ImageProviders.FirstOrDefault(p => p.Name == chosen);
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:625:            string[] names = SearchCatalog.ImageProviders.Select(p => p.Name).ToArray();
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:627:            ImageSearchProvider? provider = SearchCatalog.ImageProviders.FirstOrDefault(p => p.Name == chosen);
src/AURA.Mobile/Pages/HomePage.xaml.cs:9:    private readonly SystemAnalyzer _systemAnalyzer;
src/AURA.Mobile/Pages/HomePage.xaml.cs:13:    public HomePage(SystemAnalyzer systemAnalyzer, NetworkManager networkManager, AgentManager agentManager)
src/AURA.Mobile/Pages/HomePage.xaml.cs:16:        _systemAnalyzer = systemAnalyzer;
src/AURA.Mobile/Pages/HomePage.xaml.cs:38:            var diagnostics = await Task.Run(() => _systemAnalyzer.Analyze());
src/AURA.Mobile/Pages/HomePage.xaml.cs:39:            OsLabel.Text = "SO: " + diagnostics.OperatingSystem;
src/AURA.Mobile/Pages/HomePage.xaml.cs:40:            CpuLabel.Text = "Arquitetura: " + diagnostics.Architecture + "  |  Núcleos: " + diagnostics.ProcessorCount;
src/AURA.Mobile/Pages/HomePage.xaml.cs:41:            RamLabel.Text = $"RAM: {diagnostics.TotalMemoryGb:0.0} GB total / {diagnostics.AvailableMemoryGb:0.0} GB livre";
src/AURA.Mobile/Pages/HomePage.xaml.cs:42:            DiskLabel.Text = $"Disco {diagnostics.SystemDrive}: {diagnostics.FreeDiskSpaceGb:0.0}/{diagnostics.TotalDiskSpaceGb:0.0} GB";
src/AURA.Mobile/Pages/FixesPage.xaml.cs:2:using AURA.Mobile.Diagnostics;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:9:    private List<FixProposal> _pending = new();
src/AURA.Mobile/Pages/FixesPage.xaml.cs:74:            _pending = FixProposalParser.Parse(answer);
src/AURA.Mobile/Pages/FixesPage.xaml.cs:129:        foreach (FixProposal fix in selected)
src/AURA.Mobile/Pages/FixesPage.xaml.cs:201:        _pending = new List<FixProposal>();
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:3:namespace AURA.Mobile.Diagnostics
src/AURA.Mobile/Diagnostics/FixProposal.cs:4:namespace AURA.Mobile.Diagnostics
src/AURA.Mobile/Diagnostics/FixProposal.cs:7:    public sealed class FixProposal
src/AURA.Mobile/Diagnostics/FixProposal.cs:20:    public static class FixProposalParser
src/AURA.Mobile/Diagnostics/FixProposal.cs:22:        public static List<FixProposal> Parse(string json)
src/AURA.Mobile/Diagnostics/FixProposal.cs:24:            var result = new List<FixProposal>();
src/AURA.Mobile/Diagnostics/FixProposal.cs:34:                        var proposal = new FixProposal
src/AURA.Mobile/Diagnostics/AgentWorkspace.cs:3:namespace AURA.Mobile.Diagnostics
src/AURA.Mobile/Diagnostics/AgentWorkspace.cs:17:        public static string ActiveRoot => ProjectAccessService.IsLinked
src/AURA.Mobile/Diagnostics/AgentWorkspace.cs:18:            ? ProjectAccessService.ProjectWorkspaceRoot
src/AURA.Mobile/Diagnostics/SearchCatalog.cs:3:namespace AURA.Mobile.Diagnostics
src/AURA.Mobile/Diagnostics/SearchCatalog.cs:24:    public static class SearchCatalog
src/AURA.Mobile/Diagnostics/ProjectAccessService.cs:8:namespace AURA.Mobile.Diagnostics;
src/AURA.Mobile/Diagnostics/ProjectAccessService.cs:16:public static class ProjectAccessService
src/AURA.Mobile/MauiProgram.cs:73:        builder.Services.AddSingleton<SystemAnalyzer>();
src/AURA.Modules/Executors/ProcessExecutorBase.cs:1:using System.Diagnostics;
src/AURA.Modules/ModuleCatalog.cs:31:                Includes = new List<string> { "WebView", "SearchCatalog", "VpnHelper" },
src/AURA.Modules/ModuleCatalog.cs:61:                Includes = new List<string> { "SystemAnalyzer", "NetworkManager" },
src/AURA.Modules/Runtime/SyntaxValidator.cs:1:using System.Diagnostics;
src/AURA.Modules/Runtime/Installer.cs:1:using System.Diagnostics;
src/AURA.Modules/Runtime/CompatibilityChecker.cs:1:using System.Diagnostics;
src/AURA.Modules/Runtime/RuntimeResolver.cs:1:using System.Diagnostics;

### Sistema/rede/Windows
src/AURA.SystemInfo/SystemAnalyzer.cs:12:    public sealed class SystemAnalyzer
src/AURA.Installer/PythonEnvironmentSelector.cs:9:/// pra checar o runtime e o <see cref="SystemAnalyzer"/> pra checar disco livre.
src/AURA.Installer/PythonEnvironmentSelector.cs:25:        : this(new PythonExecutor(), () => new SystemAnalyzer().Analyze())
src/AURA.Network/NetworkManager.cs:13:    public sealed class NetworkManager
src/AURA.CLI/Program.cs:741:            SystemDiagnosticsResult result = new SystemAnalyzer().Analyze();
src/AURA.CLI/Program.cs:751:            NetworkStatus status = new NetworkManager().CheckConnection();
src/AURA.Mobile/Pages/HomePage.xaml.cs:9:    private readonly SystemAnalyzer _systemAnalyzer;
src/AURA.Mobile/Pages/HomePage.xaml.cs:10:    private readonly NetworkManager _networkManager;
src/AURA.Mobile/Pages/HomePage.xaml.cs:13:    public HomePage(SystemAnalyzer systemAnalyzer, NetworkManager networkManager, AgentManager agentManager)
src/AURA.Mobile/Pages/HomePage.xaml.cs:16:        _systemAnalyzer = systemAnalyzer;
src/AURA.Mobile/Pages/HomePage.xaml.cs:17:        _networkManager = networkManager;
src/AURA.Mobile/Pages/HomePage.xaml.cs:38:            var diagnostics = await Task.Run(() => _systemAnalyzer.Analyze());
src/AURA.Mobile/Pages/HomePage.xaml.cs:44:            var network = await Task.Run(() => _networkManager.CheckConnection());
src/AURA.Mobile/MauiProgram.cs:73:        builder.Services.AddSingleton<SystemAnalyzer>();
src/AURA.Mobile/MauiProgram.cs:74:        builder.Services.AddSingleton<NetworkManager>();
src/AURA.Modules/ModuleCatalog.cs:61:                Includes = new List<string> { "SystemAnalyzer", "NetworkManager" },


## 6. Módulos e extensibilidade

### Módulos
src/AURA.Core/Configuration/ConfigLoader.cs:47:        public ModulesConfiguration LoadModules(string path)
src/AURA.Core/Bootstrap/AuraBootstrap.cs:61:            Modules = configLoader.LoadModules(ModulesPath);
src/AURA.CLI/Program.cs:774:            foreach (ModuleInfo m in ModuleCatalog.GetAll())
src/AURA.CLI/Program.cs:785:            foreach (ModuleInfo module in ModuleCatalog.GetAll())
src/AURA.Mobile/MainPage.cs:9:        private readonly ModuleManager _manager;
src/AURA.Mobile/MainPage.cs:15:            ModuleManager manager,
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:9:        private readonly ModuleManager _manager;
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:11:        public ModulesPage(ModuleManager manager)
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:59:            var pendentes = ModuleCatalog.GetDownloadable().Where(m => !_manager.IsDownloaded(m.Id)).ToList();
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:68:            foreach (ModuleInfo m in pendentes)
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:87:            var baixados = ModuleCatalog.GetDownloadable()
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:96:            foreach (ModuleInfo m in baixados)
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:107:            var rows = ModuleCatalog.GetAll().Select(m =>
src/AURA.Mobile/ViewModels/ModuleRow.cs:6:    /// Linha exibida na Central de Módulos: envolve o ModuleInfo do catálogo
src/AURA.Mobile/ViewModels/ModuleRow.cs:12:        public ModuleInfo Module { get; init; }
src/AURA.Mobile/MauiProgram.cs:44:            .LoadModules(Path.Combine(configDir, "modules.json")));
src/AURA.Mobile/MauiProgram.cs:48:        builder.Services.AddSingleton(sp => new ModuleManager(
src/AURA.Abstractions/Runtime/RuntimeModels.cs:130:/// Resultado do pipeline completo (RuntimeManager).
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:19:public interface IRuntimeResolver
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:70:public interface IRuntimeManager
src/AURA.Modules/ModuleManager.cs:18:    public sealed class ModuleManager
src/AURA.Modules/ModuleManager.cs:27:        public ModuleManager(ILogger logger, string packagesDir, string modulesPath, EventBus events = null)
src/AURA.Modules/ModuleManager.cs:43:            ModulesConfiguration config = LoadModules();
src/AURA.Modules/ModuleManager.cs:53:            ModuleInfo info = ModuleCatalog.GetById(id);
src/AURA.Modules/ModuleManager.cs:94:            ModuleInfo info = ModuleCatalog.GetById(id);
src/AURA.Modules/ModuleManager.cs:110:            ModulesConfiguration config = LoadModules();
src/AURA.Modules/ModuleManager.cs:120:            ModuleInfo info = ModuleCatalog.GetById(id);
src/AURA.Modules/ModuleManager.cs:131:            ModulesConfiguration config = LoadModules();
src/AURA.Modules/ModuleManager.cs:145:        private ModulesConfiguration LoadModules()
src/AURA.Modules/ModuleManager.cs:147:            return _configLoader.LoadModules(_modulesPath);
src/AURA.Modules/ModuleCatalog.cs:15:    public static class ModuleCatalog
src/AURA.Modules/ModuleCatalog.cs:20:        private static readonly List<ModuleInfo> Modules = new List<ModuleInfo>
src/AURA.Modules/ModuleCatalog.cs:23:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:36:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:44:                Includes = new List<string> { "ModuleManager", "ModuleCatalog" },
src/AURA.Modules/ModuleCatalog.cs:51:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:77:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:103:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:129:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:154:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:178:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:203:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:229:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:251:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:273:            new ModuleInfo
src/AURA.Modules/ModuleCatalog.cs:297:        public static List<ModuleInfo> GetAll()
src/AURA.Modules/ModuleCatalog.cs:303:        public static List<ModuleInfo> GetCore()
src/AURA.Modules/ModuleCatalog.cs:309:        public static List<ModuleInfo> GetDownloadable()
src/AURA.Modules/ModuleCatalog.cs:314:        public static ModuleInfo GetById(string id)
src/AURA.Modules/ModuleInfo.cs:16:    public sealed class ModuleInfo : IModule
src/AURA.Modules/Runtime/RuntimeManager.cs:12:public sealed class RuntimeManager : IRuntimeManager
src/AURA.Modules/Runtime/RuntimeManager.cs:15:    private readonly IRuntimeResolver _resolver;
src/AURA.Modules/Runtime/RuntimeManager.cs:21:    public RuntimeManager(
src/AURA.Modules/Runtime/RuntimeManager.cs:23:        IRuntimeResolver? resolver = null,
src/AURA.Modules/Runtime/RuntimeManager.cs:30:        _resolver = resolver ?? new RuntimeResolver();
src/AURA.Modules/Runtime/RuntimeResolver.cs:12:public sealed class RuntimeResolver : IRuntimeResolver

### Interfaces/extensibilidade
src/AURA.Core/Runtime/PluginWatcher.cs:21:    public sealed class PluginWatcher : IDisposable
src/AURA.Core/Runtime/PluginWatcher.cs:31:        private List<IPlugin> _plugins = new List<IPlugin>();
src/AURA.Core/Runtime/PluginWatcher.cs:33:        public PluginWatcher(ILogger logger, string pluginsRoot = null)
src/AURA.Core/Runtime/PluginWatcher.cs:62:        /// <summary>Plugins implementing <see cref="IPlugin"/> discovered in the current set.</summary>
src/AURA.Core/Runtime/PluginWatcher.cs:63:        public IReadOnlyList<IPlugin> Plugins => _plugins;
src/AURA.Core/Runtime/PluginWatcher.cs:82:                    _plugins = new List<IPlugin>();
src/AURA.Core/Runtime/PluginWatcher.cs:128:                        && typeof(IPlugin).IsAssignableFrom(t))
src/AURA.Core/Runtime/PluginWatcher.cs:145:                    IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
src/AURA.Core/Runtime/PluginWatcher.cs:160:            foreach (IPlugin plugin in _plugins)
src/AURA.Core/Abstractions/IModule.cs:10:    public interface IModule
src/AURA.Core/Abstractions/IPlugin.cs:7:    public interface IPlugin
src/AURA.Core/Abstractions/IAgent.cs:7:    public interface IAgent
src/AURA.Core/Abstractions/IService.cs:6:    public interface IService
src/AURA.CLI/Program.cs:28:        private static PluginWatcher _pluginWatcher;
src/AURA.CLI/Program.cs:61:            _pluginWatcher = new PluginWatcher(_logger);
src/AURA.CLI/Program.cs:62:            _runner = new Runner(_pluginWatcher.Launchers.Concat(
src/AURA.CLI/Program.cs:84:            _pluginWatcher.Dispose();
src/AURA.CLI/Program.cs:722:            Console.WriteLine("Plugins (" + _pluginWatcher.PluginsRoot + "):");
src/AURA.CLI/Program.cs:723:            string[] paths = _pluginWatcher.PluginPaths.ToArray();
src/AURA.CLI/Program.cs:735:            Console.WriteLine("Launchers de plugins : " + _pluginWatcher.Launchers.Count);
src/AURA.CLI/Program.cs:736:            Console.WriteLine("Plugins IPlugin      : " + _pluginWatcher.Plugins.Count);
src/AURA.Modules/ModuleCatalog.cs:282:                    "Definir a API pública de plugins (IPlugin)",
src/AURA.Modules/ModuleInfo.cs:16:    public sealed class ModuleInfo : IModule


## 7. IA e pesquisa

### Provedores IA
src/AURA.AI/AgentSession.cs:14:    /// Loop agêntico sobre o OpenRouterClient: envia a conversa com as
src/AURA.AI/AgentSession.cs:23:        private readonly OpenRouterClient _client;
src/AURA.AI/AgentSession.cs:30:        public AgentSession(OpenRouterClient client, IEnumerable<AgentTool> tools,
src/AURA.AI/AiAssistant.cs:15:    public sealed class AiAssistant
src/AURA.AI/AiAssistant.cs:17:        private readonly OpenRouterClient _client;
src/AURA.AI/AiAssistant.cs:21:        public AiAssistant(OpenRouterClient client, MemoryStore memory, ILogger? logger = null)
src/AURA.AI/OpenRouterClient.cs:35:    public sealed class OpenRouterClient
src/AURA.AI/OpenRouterClient.cs:41:        public OpenRouterClient(OpenRouterOptions options, ILogger? logger = null)
src/AURA.AI/ProviderCatalog.cs:25:    public static class ProviderCatalog
src/AURA.AI/AiAssistantService.cs:14:    /// <br/>Pipeline: Client App → (AiAssistant) → OpenRouterClient → OpenRouter API.
src/AURA.AI/AiAssistantService.cs:18:    public static class AiAssistantService
src/AURA.CLI/Program.cs:36:        private static OpenRouterClient _aiClient;
src/AURA.CLI/Program.cs:381:            OpenRouterClient client = EnsureAiClient(model);
src/AURA.CLI/Program.cs:417:            OpenRouterClient client = EnsureAiClient();
src/AURA.CLI/Program.cs:487:        private static OpenRouterClient EnsureAiClient(string? model = null)
src/AURA.CLI/Program.cs:507:                _aiClient = new OpenRouterClient(
src/AURA.CLI/Program.cs:537:            _aiClient = new OpenRouterClient(
src/AURA.Mobile/Pages/LogsPage.xaml.cs:10:    private readonly OpenRouterClient _client;
src/AURA.Mobile/Pages/LogsPage.xaml.cs:12:    public LogsPage(OpenRouterClient client)
src/AURA.Mobile/Pages/AgentPage.xaml.cs:8:    private readonly OpenRouterClient _client;
src/AURA.Mobile/Pages/AgentPage.xaml.cs:11:    public AgentPage(OpenRouterClient client)
src/AURA.Mobile/Pages/ChatPage.xaml.cs:8:    private readonly OpenRouterClient _client;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:11:    public ChatPage(OpenRouterClient client, AURA.Memory.MemoryStore memory)
src/AURA.Mobile/Pages/ChatPage.xaml.cs:30:            ProviderPicker.ItemsSource = ProviderCatalog.Providers;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:34:        for (int i = 0; i < ProviderCatalog.Providers.Count; i++)
src/AURA.Mobile/Pages/ChatPage.xaml.cs:36:            if (string.Equals(ProviderCatalog.Providers[i].Name, savedProvider, StringComparison.OrdinalIgnoreCase))
src/AURA.Mobile/Pages/ChatPage.xaml.cs:162:            var assistant = new AiAssistant(_client, _memory);
src/AURA.Mobile/Pages/FixesPage.xaml.cs:8:    private readonly OpenRouterClient _client;
src/AURA.Mobile/Pages/FixesPage.xaml.cs:11:    public FixesPage(OpenRouterClient client)
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:8:    /// imediatamente no OpenRouterClient.
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:48:        public static void Apply(OpenRouterClient client)
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:50:            ProviderInfo provider = ProviderCatalog.Find(Provider);
src/AURA.Mobile/MauiProgram.cs:60:        builder.Services.AddSingleton(sp => new OpenRouterClient(new OpenRouterOptions
src/AURA.Mobile/MauiProgram.cs:67:        builder.Services.AddSingleton<AiAssistant>();
src/AURA.Modules/ModuleCatalog.cs:87:                Includes = new List<string> { "OpenRouterClient", "AgentManager" },

### Pesquisa/Web
src/AURA.AI/AgentSession.cs:46:            HttpClient? httpClient = null, CancellationToken ct = default)
src/AURA.AI/AgentSession.cs:61:                    httpClient,
src/AURA.AI/AiAssistant.cs:29:            HttpClient? httpClient = null, CancellationToken ct = default)
src/AURA.AI/AiAssistant.cs:33:            string answer = await _client.ChatAsync(question, httpClient, ct).ConfigureAwait(false);
src/AURA.AI/OpenRouterClient.cs:90:            HttpClient? httpClient = null, CancellationToken ct = default, string? systemPrompt = null)
src/AURA.AI/OpenRouterClient.cs:94:            HttpClient client = httpClient ?? ResolveClient();
src/AURA.AI/OpenRouterClient.cs:138:            HttpClient? httpClient = null,
src/AURA.AI/OpenRouterClient.cs:240:            HttpClient client = httpClient ?? ResolveClient();
src/AURA.AI/OpenRouterClient.cs:365:        private HttpClient ResolveClient()
src/AURA.AI/OpenRouterClient.cs:367:            return new HttpClient
src/AURA.AI/AiAssistantService.cs:28:        public static async Task<string> AskAsync(string question, MemoryStore? memory = null, ILogger? logger = null, OpenRouterOptions? options = null, HttpClient? http = null)
src/AURA.AI/AiAssistantService.cs:43:            HttpClient client = http ?? new HttpClient();
src/AURA.Mobile/MainPage.cs:25:            BrowserPage browser,
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs:5:    public partial class ImageSearchPage : ContentPage
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs:7:        private readonly List<ImageSearchProvider> _providers = SearchCatalog.ImageProviders;
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs:9:        public ImageSearchPage()
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs:83:                AuraLog.Exception("ImageSearchPage.Upload", ex);
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs:103:            using var handler = new HttpClientHandler { AllowAutoRedirect = false };
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs:104:            using var http = new HttpClient(handler);
src/AURA.Mobile/Pages/LogsPage.xaml.cs:103:            using var handler = new HttpClientHandler
src/AURA.Mobile/Pages/LogsPage.xaml.cs:108:            using var http = new HttpClient(handler);
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:6:    /// Tudo é salvo em Preferences na hora; o BrowserPage reaplica ao voltar.
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:20:                IsToggled = Preferences.Default.Get(BrowserPage.JsEnabledKey, true),
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:23:            jsSwitch.Toggled += (s, e) => Preferences.Default.Set(BrowserPage.JsEnabledKey, e.Value);
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:28:                IsToggled = Preferences.Default.Get(BrowserPage.AdsEnabledKey, true),
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:31:            adsSwitch.Toggled += (s, e) => Preferences.Default.Set(BrowserPage.AdsEnabledKey, e.Value);
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:36:                IsToggled = Preferences.Default.Get(BrowserPage.StealthEnabledKey, true),
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:39:            stealthSwitch.Toggled += (s, e) => Preferences.Default.Set(BrowserPage.StealthEnabledKey, e.Value);
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:40:            stack.Add(Row("Anti-identificação (esconder WebView)", stealthSwitch));
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:44:                Text = "Anti-identificação: usa um User-Agent de Chrome comum (sem o marcador \"wv\") e mascara sinais de WebView, para o site não detectar um navegador embutido/espaço separado. Na célula isolada essa proteção é sempre forçada.",
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:70:            uaPicker.SelectedIndex = Math.Clamp(Preferences.Default.Get(BrowserPage.UserAgentModeKey, 0), 0, 2);
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:72:                Preferences.Default.Set(BrowserPage.UserAgentModeKey, uaPicker.SelectedIndex);
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:78:                Text = Preferences.Default.Get(BrowserPage.UserAgentCustomKey, string.Empty),
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:84:            customUaEntry.TextChanged += (s, e) => Preferences.Default.Set(BrowserPage.UserAgentCustomKey, e.NewTextValue);
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:98:                Text = Preferences.Default.Get(BrowserPage.HomeUrlKey, string.Empty),
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:104:            homeEntry.TextChanged += (s, e) => Preferences.Default.Set(BrowserPage.HomeUrlKey, e.NewTextValue);
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:9:    public partial class BrowserPage : ContentPage
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:34:        // Anti-fingerprint: o WebView padrão expõe "wv", Version/4.0, navigator.
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:36:        // detectar "WebView/clone/espaço separado". Mascara para parecer um
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:50:        private readonly List<SearchEngine> _engines = SearchCatalog.Engines;
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:51:        private readonly ImageSearchPage _imageSearch;
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:60:        public BrowserPage(ImageSearchPage imageSearch, SimulationRuntime runtime, EventBus events)
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:72:            AURA.Mobile.Platforms.Android.WebView.AuraWebViewHandler.ImageLongPress += OnImageLongPress;
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:162:            public WebView View { get; }
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:167:            public BrowserTab(int id, WebView view, string url)
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:177:            var view = new WebView
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:458:                // WebView roda no app). Ao excluir a célula, os dados de
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:460:                _runtime.CreateCell(id, "com.aura.webview", "browser-isolado");
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:527:                    (tab.View.Handler?.PlatformView as global::Android.Webkit.WebView)?.ClearCache(true);
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:555:            string[] names = SearchCatalog.ImageProviders.Select(p => p.Name).ToArray();
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:557:            ImageSearchProvider? provider = SearchCatalog.ImageProviders.FirstOrDefault(p => p.Name == chosen);
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:618:        private async void OnImageLongPress(global::Android.Webkit.WebView wv, string imageUrl)
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:625:            string[] names = SearchCatalog.ImageProviders.Select(p => p.Name).ToArray();
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:627:            ImageSearchProvider? provider = SearchCatalog.ImageProviders.FirstOrDefault(p => p.Name == chosen);
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:707:        private Android.Webkit.WebView? ActivePlatformView() =>
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:708:            _active?.View.Handler?.PlatformView as Android.Webkit.WebView;
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:710:        private sealed class AuraFindListener : Java.Lang.Object, Android.Webkit.WebView.IFindListener
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:728:        private void InjectAdBlocker(WebView view)
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:745:        private void InjectStealth(WebView view)
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:772:        private void ApplySettings(WebView view)
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:775:            var wv = view.Handler?.PlatformView as Android.Webkit.WebView;
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:793:            // Célula isolada força o mascaramento: nunca deixa o "wv" do WebView
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:794:            // vazar, para o site não identificar "WebView/espaço separado".
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:813:        /// aparelho (Build), sem os marcadores "wv" / "Version/4.0" do WebView.
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:864:                ? "Endereço .onion exige Tor ativo.\n\nAbra o Orbot e ative o modo VPN (a conexão de todo o aparelho passa pelo Tor — então o WebView consegue acessar .onion). Depois toque em 'Abrir Orbot' e tente de novo."
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:865:                : "Endereço .onion exige Tor, que não vem no Android.\n\nInstale o Orbot (Tor oficial): ele oferece modo VPN que roteia o app inteiro pela rede Tor, permitindo abrir .onion no navegador. O WebView da AURA não pode se conectar a .onion sem ele.";
src/AURA.Mobile/Diagnostics/SearchCatalog.cs:22:    /// Todos usam URLs públicas (sem API key) para abrir direto no WebView.
src/AURA.Mobile/Diagnostics/SearchCatalog.cs:24:    public static class SearchCatalog
src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs:1:namespace AURA.Mobile.Platforms.Android.WebView
src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs:5:    /// HitTestResult do WebView e, se for uma imagem, publica o evento
src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs:6:    /// ImageLongPress (consumido pela BrowserPage para buscar imagem reversa).
src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs:11:        private readonly Action<global::Android.Webkit.WebView> _onImage;
src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs:13:        public AuraLongClickListener(Action<global::Android.Webkit.WebView> onImage)
src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs:20:            if (v is not global::Android.Webkit.WebView wv)
src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs:53:                AURA.Mobile.AuraLog.Exception("WebView.LongPress", ex);
src/AURA.Mobile/Platforms/Android/WebView/AuraTouchListener.cs:1:namespace AURA.Mobile.Platforms.Android.WebView
src/AURA.Mobile/Platforms/Android/WebView/AuraTouchListener.cs:5:    /// de rolar do WebView. No ACTION_DOWN pede ao pai para não interceptar
src/AURA.Mobile/Platforms/Android/WebView/AuraTouchListener.cs:6:    /// (RequestDisallowInterceptTouchEvent) e devolve false, deixando o WebView
src/AURA.Mobile/Platforms/Android/WebView/AuraDownloadListener.cs:1:namespace AURA.Mobile.Platforms.Android.WebView
src/AURA.Mobile/Platforms/Android/WebView/AuraDownloadListener.cs:5:    /// navegador/app externo do aparelho, já que o WebView embutido não tem
src/AURA.Mobile/Platforms/Android/WebView/AuraDownloadListener.cs:37:                AURA.Mobile.AuraLog.Info("WebView: download/recurso aberto externamente: " + url);
src/AURA.Mobile/Platforms/Android/WebView/AuraDownloadListener.cs:41:                AURA.Mobile.AuraLog.Exception("WebView.Download", ex);
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:6:namespace AURA.Mobile.Platforms.Android.WebView
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:9:    /// Handler do WebView da AURA. Mantém o view/clients nativos do MAUI
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:10:    /// (MauiWebView implementa IWebViewDelegate e é quem de fato carrega a URL e
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:11:    /// dispara Navigating/Navigated), apenas endurece o WebView Android:
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:17:    public sealed class AuraWebViewHandler : WebViewHandler
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:21:        /// a URL da imagem (src). A BrowserPage usa para busca reversa.
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:23:        public static event Action<global::Android.Webkit.WebView, string>? ImageLongPress;
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:25:        static AuraWebViewHandler()
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:27:            Mapper.AppendToMapping("AuraWebViewSetup", MapAuraSetup);
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:30:        static void MapAuraSetup(IWebViewHandler handler, IWebView view)
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:32:            var webView = handler.PlatformView;
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:33:            if (webView == null)
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:40:                var settings = webView.Settings;
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:51:                webView.SetDownloadListener(new AuraDownloadListener());
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:53:                // Garante que o gesto de rolar chegue ao WebView.
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:54:                webView.OverScrollMode = OverScrollMode.Always;
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:55:                webView.SetOnTouchListener(new AuraTouchListener());
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:58:                webView.SetOnLongClickListener(new AuraLongClickListener(wv =>
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:69:                AURA.Mobile.AuraLog.Exception("WebView.HandlerSetup", ex);
src/AURA.Mobile/MauiProgram.cs:27:        // Handler Android do WebView: mantém o comportamento do MAUI e corrige
src/AURA.Mobile/MauiProgram.cs:28:        // rolagem + downloads + target=_blank (ver AuraWebViewHandler).
src/AURA.Mobile/MauiProgram.cs:30:            handlers.AddHandler<Microsoft.Maui.Controls.WebView, AURA.Mobile.Platforms.Android.WebView.AuraWebViewHandler>());
src/AURA.Mobile/MauiProgram.cs:104:        builder.Services.AddSingleton<BrowserPage>();
src/AURA.Mobile/MauiProgram.cs:105:        builder.Services.AddSingleton<ImageSearchPage>();
src/AURA.Modules/ModuleManager.cs:24:        private readonly HttpClient _http;
src/AURA.Modules/ModuleManager.cs:34:            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(40) };
src/AURA.Modules/ModuleCatalog.cs:31:                Includes = new List<string> { "WebView", "SearchCatalog", "VpnHelper" },


## 8. Mobile/DI

### DI
src/AURA.Mobile/Platforms/Android/MainApplication.cs:40:            MauiApp app = MauiProgram.CreateMauiApp();
src/AURA.Mobile/MauiProgram.cs:17:public static class MauiProgram
src/AURA.Mobile/MauiProgram.cs:21:        AuraLog.Info("MauiProgram.CreateMauiApp BEGIN");
src/AURA.Mobile/MauiProgram.cs:33:        AuraLog.Info("MauiProgram: builder created");
src/AURA.Mobile/MauiProgram.cs:36:        builder.Services.AddSingleton<ILogger, ConsoleLogger>();
src/AURA.Mobile/MauiProgram.cs:37:        builder.Services.AddSingleton<EventBus>();
src/AURA.Mobile/MauiProgram.cs:41:        builder.Services.AddSingleton(sp => new ConfigLoader(sp.GetRequiredService<ILogger>())
src/AURA.Mobile/MauiProgram.cs:43:        builder.Services.AddSingleton(sp => new ConfigLoader(sp.GetRequiredService<ILogger>())
src/AURA.Mobile/MauiProgram.cs:48:        builder.Services.AddSingleton(sp => new ModuleManager(
src/AURA.Mobile/MauiProgram.cs:49:            sp.GetRequiredService<ILogger>(),
src/AURA.Mobile/MauiProgram.cs:52:            sp.GetRequiredService<EventBus>()));
src/AURA.Mobile/MauiProgram.cs:55:        builder.Services.AddSingleton(sp => new MemoryStore(
src/AURA.Mobile/MauiProgram.cs:56:            sp.GetRequiredService<ILogger>(),
src/AURA.Mobile/MauiProgram.cs:60:        builder.Services.AddSingleton(sp => new OpenRouterClient(new OpenRouterOptions
src/AURA.Mobile/MauiProgram.cs:66:        }, sp.GetRequiredService<ILogger>()));
src/AURA.Mobile/MauiProgram.cs:67:        builder.Services.AddSingleton<AiAssistant>();
src/AURA.Mobile/MauiProgram.cs:69:        builder.Services.AddSingleton(sp => new AgentManager(sp.GetRequiredService<ILogger>())
src/AURA.Mobile/MauiProgram.cs:71:            Events = sp.GetRequiredService<EventBus>()
src/AURA.Mobile/MauiProgram.cs:73:        builder.Services.AddSingleton<SystemAnalyzer>();
src/AURA.Mobile/MauiProgram.cs:74:        builder.Services.AddSingleton<NetworkManager>();
src/AURA.Mobile/MauiProgram.cs:77:        builder.Services.AddSingleton<ShellExecutor>();
src/AURA.Mobile/MauiProgram.cs:78:        builder.Services.AddSingleton<GitExecutor>();
src/AURA.Mobile/MauiProgram.cs:79:        builder.Services.AddSingleton<PythonExecutor>();
src/AURA.Mobile/MauiProgram.cs:80:        builder.Services.AddSingleton<NodeExecutor>();
src/AURA.Mobile/MauiProgram.cs:84:        builder.Services.AddSingleton(sp => new SimulationRuntime(
src/AURA.Mobile/MauiProgram.cs:85:            sp.GetRequiredService<ILogger>(),
src/AURA.Mobile/MauiProgram.cs:89:            Events = sp.GetRequiredService<EventBus>()
src/AURA.Mobile/MauiProgram.cs:91:        builder.Services.AddSingleton<Runner>();
src/AURA.Mobile/MauiProgram.cs:94:        builder.Services.AddSingleton<MainPage>();
src/AURA.Mobile/MauiProgram.cs:95:        builder.Services.AddSingleton<HomePage>();
src/AURA.Mobile/MauiProgram.cs:96:        builder.Services.AddSingleton<ChatPage>();
src/AURA.Mobile/MauiProgram.cs:97:        builder.Services.AddSingleton<AgentPage>();
src/AURA.Mobile/MauiProgram.cs:98:        builder.Services.AddSingleton<MemoryPage>();
src/AURA.Mobile/MauiProgram.cs:99:        builder.Services.AddSingleton<ExecutorsPage>();
src/AURA.Mobile/MauiProgram.cs:100:        builder.Services.AddSingleton<ModulesPage>();
src/AURA.Mobile/MauiProgram.cs:101:        builder.Services.AddSingleton<LogsPage>();
src/AURA.Mobile/MauiProgram.cs:102:        builder.Services.AddSingleton<FixesPage>();
src/AURA.Mobile/MauiProgram.cs:103:        builder.Services.AddSingleton<TerminalPage>();
src/AURA.Mobile/MauiProgram.cs:104:        builder.Services.AddSingleton<BrowserPage>();
src/AURA.Mobile/MauiProgram.cs:105:        builder.Services.AddSingleton<ImageSearchPage>();
src/AURA.Mobile/MauiProgram.cs:106:        builder.Services.AddSingleton<CellsPage>();
src/AURA.Mobile/MauiProgram.cs:107:        builder.Services.AddSingleton<RunPage>();
src/AURA.Mobile/MauiProgram.cs:109:        AuraLog.Info("MauiProgram: services registered");
src/AURA.Mobile/MauiProgram.cs:116:            var bus = app.Services.GetRequiredService<EventBus>();
src/AURA.Mobile/MauiProgram.cs:117:            var memory = app.Services.GetRequiredService<MemoryStore>();
src/AURA.Mobile/MauiProgram.cs:123:            AuraLog.Exception("MauiProgram.MemoryEventSink", ex);
src/AURA.Mobile/MauiProgram.cs:126:        AuraLog.Info("MauiProgram.CreateMauiApp OK");

### Páginas
src/AURA.Mobile/MainPage.cs:7:    public class MainPage : TabbedPage
src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs:5:    public partial class ImageSearchPage : ContentPage
src/AURA.Mobile/Pages/LogsPage.xaml.cs:8:public partial class LogsPage : ContentPage
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:8:    public sealed class BrowserSettingsPage : ContentPage
src/AURA.Mobile/Pages/TerminalPage.xaml.cs:6:public partial class TerminalPage : ContentPage
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:6:public partial class MemoryPage : ContentPage
src/AURA.Mobile/Pages/AgentPage.xaml.cs:6:public partial class AgentPage : ContentPage
src/AURA.Mobile/Pages/RunPage.xaml.cs:7:public partial class RunPage : ContentPage
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:7:public partial class ExecutorsPage : ContentPage
src/AURA.Mobile/Pages/SectionPage.cs:7:public sealed class SectionPage : ContentPage
src/AURA.Mobile/Pages/ModulesPage.xaml.cs:7:    public partial class ModulesPage : ContentPage
src/AURA.Mobile/Pages/ChatPage.xaml.cs:6:public partial class ChatPage : ContentPage
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:9:    public partial class BrowserPage : ContentPage
src/AURA.Mobile/Pages/HomePage.xaml.cs:7:public partial class HomePage : ContentPage
src/AURA.Mobile/Pages/CellsPage.xaml.cs:7:public partial class CellsPage : ContentPage
src/AURA.Mobile/Pages/FixesPage.xaml.cs:6:public partial class FixesPage : ContentPage


## 9. Dependências entre projetos

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

## 10. Classes e interfaces

src/AURA.AI/AgentChat.cs:10:    public sealed class AgentMessage
src/AURA.AI/AgentChat.cs:22:    public sealed class AgentToolCall
src/AURA.AI/AgentChat.cs:33:    public sealed class AgentChatResponse
src/AURA.AI/AgentChat.cs:45:    public sealed class AgentStep
src/AURA.AI/AgentSession.cs:19:    public sealed class AgentSession
src/AURA.AI/AgentTool.cs:17:    public sealed class AgentToolDefinition
src/AURA.AI/AgentTool.cs:32:    public abstract class AgentTool
src/AURA.AI/AgentTool.cs:9:    public sealed class AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:112:    public sealed class WriteFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:11:    public sealed class ListDirTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:157:    public sealed class EditFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:71:    public sealed class ReadFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/ShellAgentTool.cs:15:    public sealed class ShellAgentTool : AgentTool
src/AURA.AI/AgentTools/WorkspaceAgentTool.cs:11:    public abstract class WorkspaceAgentTool : AgentTool
src/AURA.AI/AiAssistant.cs:15:    public sealed class AiAssistant
src/AURA.AI/AiAssistantService.cs:18:    public static class AiAssistantService
src/AURA.AI/OpenRouterClient.cs:20:    public sealed class OpenRouterOptions
src/AURA.AI/OpenRouterClient.cs:35:    public sealed class OpenRouterClient
src/AURA.AI/ProviderCatalog.cs:16:    public sealed class ProviderInfo
src/AURA.AI/ProviderCatalog.cs:25:    public static class ProviderCatalog
src/AURA.AI/ProviderCatalog.cs:5:    public sealed class ProviderModel
src/AURA.Abstractions/Execution/ExecutionRequest.cs:12:    public sealed class ExecutionRequest
src/AURA.Abstractions/Execution/ExecutionResult.cs:9:    public sealed class ExecutionResult
src/AURA.Abstractions/Execution/IToolExecutor.cs:11:    public interface IToolExecutor
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:10:public interface IRuntimeDetector
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:19:public interface IRuntimeResolver
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:28:public interface IDependencyAnalyzer
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:37:public interface ISyntaxValidator
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:46:public interface ICompatibilityChecker
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:55:public interface IRuntimeInstaller
src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs:70:public interface IRuntimeManager
src/AURA.Abstractions/Runtime/RuntimeModels.cs:104:public sealed class ExecutionOutcome
src/AURA.Abstractions/Runtime/RuntimeModels.cs:10:public sealed class Detection
src/AURA.Abstractions/Runtime/RuntimeModels.cs:132:public sealed class PipelineReport
src/AURA.Abstractions/Runtime/RuntimeModels.cs:25:public sealed class RuntimeResolution
src/AURA.Abstractions/Runtime/RuntimeModels.cs:41:public sealed class Dependency
src/AURA.Abstractions/Runtime/RuntimeModels.cs:55:public sealed class DependencyReport
src/AURA.Abstractions/Runtime/RuntimeModels.cs:67:public sealed class SyntaxResult
src/AURA.Abstractions/Runtime/RuntimeModels.cs:78:public sealed class CompatReport
src/AURA.Abstractions/Runtime/RuntimeModels.cs:89:public sealed record InstallStep(string What, string Command, bool IsRuntime);
src/AURA.Abstractions/Runtime/RuntimeModels.cs:94:public sealed class InstallPlan
src/AURA.Agents/AgentManager.cs:16:    public sealed class AgentInfo
src/AURA.Agents/AgentManager.cs:283:        private sealed class Definition
src/AURA.Agents/AgentManager.cs:36:    public sealed class AgentManager
src/AURA.CLI/Program.cs:24:    internal class Program
src/AURA.Core/Abstractions/IAgent.cs:7:    public interface IAgent
src/AURA.Core/Abstractions/ICommand.cs:7:    public interface ICommand
src/AURA.Core/Abstractions/IModule.cs:10:    public interface IModule
src/AURA.Core/Abstractions/IPlugin.cs:7:    public interface IPlugin
src/AURA.Core/Abstractions/IService.cs:6:    public interface IService
src/AURA.Core/Bootstrap/AuraBootstrap.cs:14:    public sealed class AuraBootstrap
src/AURA.Core/Configuration/AuraConfiguration.cs:6:    public class AuraConfiguration
src/AURA.Core/Configuration/ConfigLoader.cs:14:    public sealed class ConfigLoader
src/AURA.Core/Configuration/ModulesConfiguration.cs:23:    public class ModuleFlags
src/AURA.Core/Configuration/ModulesConfiguration.cs:9:    public class ModulesConfiguration
src/AURA.Core/DependencyInjection/ServiceContainer.cs:11:    public sealed class ServiceContainer
src/AURA.Core/Events/AuraEvents.cs:24:    public sealed class AssistantRespondedEvent : IEvent
src/AURA.Core/Events/AuraEvents.cs:41:    public sealed class ExecutorCompletedEvent : IEvent
src/AURA.Core/Events/AuraEvents.cs:58:    public sealed class ModuleStateChangedEvent : IEvent
src/AURA.Core/Events/AuraEvents.cs:9:    public sealed class CellStateChangedEvent : IEvent
src/AURA.Core/Events/EventBus.cs:9:    public sealed class EventBus
src/AURA.Core/Events/IEvent.cs:8:    public interface IEvent
src/AURA.Core/Launchers/CellCommand.cs:10:    public sealed class CellCommand
src/AURA.Core/Launchers/DllLauncher.cs:9:    public sealed class DllLauncher : ILauncher
src/AURA.Core/Launchers/GoLauncher.cs:10:    public sealed class GoLauncher : ILauncher
src/AURA.Core/Launchers/ILauncher.cs:10:    public interface ILauncher
src/AURA.Core/Launchers/JarLauncher.cs:10:    public sealed class JarLauncher : ILauncher
src/AURA.Core/Launchers/NodeLauncher.cs:10:    public sealed class NodeLauncher : ILauncher
src/AURA.Core/Launchers/PythonLauncher.cs:10:    public sealed class PythonLauncher : ILauncher
src/AURA.Core/Launchers/Runner.cs:14:    public sealed class Runner
src/AURA.Core/Logging/ConsoleLogger.cs:10:    public sealed class ConsoleLogger : ILogger
src/AURA.Core/Logging/FileLogger.cs:10:    public sealed class FileLogger : ILogger
src/AURA.Core/Logging/ILogger.cs:6:    public interface ILogger
src/AURA.Core/Runtime/Cell.cs:11:    public sealed class Cell
src/AURA.Core/Runtime/CellState.cs:8:    public enum CellState
src/AURA.Core/Runtime/CellStore.cs:13:    public sealed class CellStore
src/AURA.Core/Runtime/CellStore.cs:97:        private sealed class CellStoreDocument
src/AURA.Core/Runtime/DirectoryCellBackend.cs:12:    public sealed class DirectoryCellBackend : ICellBackend
src/AURA.Core/Runtime/ICellBackend.cs:9:    public interface ICellBackend
src/AURA.Core/Runtime/PluginWatcher.cs:21:    public sealed class PluginWatcher : IDisposable
src/AURA.Core/Runtime/PluginWatcher.cs:224:        private sealed class PluginLoadContext : AssemblyLoadContext
src/AURA.Core/Runtime/ResourceLimits.cs:10:    public sealed class ResourceLimits
src/AURA.Core/Runtime/SimulationRuntime.cs:21:    public sealed class SimulationRuntime : IDisposable
src/AURA.Core/VersionInfo.cs:8:    public static class VersionInfo
src/AURA.Installer/ArtifactAnalysisService.cs:24:public sealed class ArtifactAnalysisService
src/AURA.Installer/ArtifactAnalysisService.cs:7:public sealed class ArtifactAnalysisResult
src/AURA.Installer/ArtifactIdentification.cs:6:public sealed class ArtifactIdentification
src/AURA.Installer/ArtifactType.cs:8:public enum ArtifactType
src/AURA.Installer/DependencyReport.cs:8:public sealed class DependencyReport
src/AURA.Installer/EnvironmentSelectionResult.cs:10:public sealed class EnvironmentSelectionResult
src/AURA.Installer/EnvironmentSelectionService.cs:9:public sealed class EnvironmentSelectionService
src/AURA.Installer/FileIdentifier.cs:10:public sealed class FileIdentifier : IFileIdentifier
src/AURA.Installer/IDependencyAnalyzer.cs:8:public interface IDependencyAnalyzer
src/AURA.Installer/IEnvironmentSelector.cs:8:public interface IEnvironmentSelector
src/AURA.Installer/IFileIdentifier.cs:8:public interface IFileIdentifier
src/AURA.Installer/IInstaller.cs:8:public interface IInstaller
src/AURA.Installer/InstallationResult.cs:7:public sealed class InstallationResult
src/AURA.Installer/InstallationService.cs:8:public sealed class InstallationService
src/AURA.Installer/PythonDependencyAnalyzer.cs:12:public sealed class PythonDependencyAnalyzer : IDependencyAnalyzer
src/AURA.Installer/PythonEnvironmentSelector.cs:12:public sealed class PythonEnvironmentSelector : IEnvironmentSelector
src/AURA.Installer/PythonInstaller.cs:12:public sealed class PythonInstaller : IInstaller
src/AURA.Installer/PythonStdlibModules.cs:10:public static class PythonStdlibModules
src/AURA.Memory/MemoryEntry.cs:12:    public sealed class MemoryEntry
src/AURA.Memory/MemoryEntry.cs:6:    public enum MemoryKind
src/AURA.Memory/MemoryStore.cs:144:        private sealed class MemoryDocument
src/AURA.Memory/MemoryStore.cs:19:    public sealed class MemoryStore
src/AURA.Memory/RequestContext.cs:11:    public sealed class RequestContext
src/AURA.Memory/SolutionRule.cs:12:    public sealed class SolutionRule
src/AURA.Memory/SolutionStore.cs:17:    public sealed class SolutionStore
src/AURA.Mobile/Diagnostics/AgentWorkspace.cs:9:    public static class AgentWorkspace
src/AURA.Mobile/Diagnostics/FixProposal.cs:20:    public static class FixProposalParser
src/AURA.Mobile/Diagnostics/FixProposal.cs:7:    public sealed class FixProposal
src/AURA.Mobile/Diagnostics/ProjectAccessService.cs:16:public static class ProjectAccessService
src/AURA.Mobile/Diagnostics/RuntimeConfig.cs:10:    public static class RuntimeConfig
src/AURA.Mobile/Diagnostics/SearchCatalog.cs:12:    public sealed class ImageSearchProvider
src/AURA.Mobile/Diagnostics/SearchCatalog.cs:24:    public static class SearchCatalog
src/AURA.Mobile/Diagnostics/SearchCatalog.cs:5:    public sealed class SearchEngine
src/AURA.Mobile/MainPage.cs:7:    public class MainPage : TabbedPage
src/AURA.Mobile/MauiProgram.cs:17:public static class MauiProgram
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:159:        private sealed class BrowserTab
src/AURA.Mobile/Pages/BrowserPage.xaml.cs:710:        private sealed class AuraFindListener : Java.Lang.Object, Android.Webkit.WebView.IFindListener
src/AURA.Mobile/Pages/BrowserSettingsPage.cs:8:    public sealed class BrowserSettingsPage : ContentPage
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:139:public class ExecutorStatus
src/AURA.Mobile/Pages/SectionPage.cs:7:public sealed class SectionPage : ContentPage
src/AURA.Mobile/Platforms/Android/AuraLog.cs:312:        private sealed class AuraUncaughtExceptionHandler : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
src/AURA.Mobile/Platforms/Android/AuraLog.cs:33:    public static class AuraLog
src/AURA.Mobile/Platforms/Android/MainActivity.cs:13:public class MainActivity : MauiAppCompatActivity
src/AURA.Mobile/Platforms/Android/MainApplication.cs:7:public class MainApplication : MauiApplication
src/AURA.Mobile/Platforms/Android/StoragePermissionHelper.cs:8:public static class StoragePermissionHelper
src/AURA.Mobile/Platforms/Android/VpnHelper.cs:12:    public static class VpnHelper
src/AURA.Mobile/Platforms/Android/WebView/AuraDownloadListener.cs:8:    public sealed class AuraDownloadListener : Java.Lang.Object, global::Android.Webkit.IDownloadListener
src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs:9:    public sealed class AuraLongClickListener : Java.Lang.Object, global::Android.Views.View.IOnLongClickListener
src/AURA.Mobile/Platforms/Android/WebView/AuraTouchListener.cs:9:    public sealed class AuraTouchListener : Java.Lang.Object, global::Android.Views.View.IOnTouchListener
src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs:17:    public sealed class AuraWebViewHandler : WebViewHandler
src/AURA.Mobile/ViewModels/ModuleRow.cs:10:    public sealed class ModuleRow
src/AURA.Modules/Executors/GitExecutor.cs:11:public sealed class GitExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/NodeExecutor.cs:9:public sealed class NodeExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/ProcessExecutorBase.cs:13:public abstract class ProcessExecutorBase : IToolExecutor
src/AURA.Modules/Executors/PythonExecutor.cs:10:public sealed class PythonExecutor : ProcessExecutorBase
src/AURA.Modules/Executors/ShellExecutor.cs:8:public sealed class ShellExecutor : ProcessExecutorBase
src/AURA.Modules/ModuleCatalog.cs:15:    public static class ModuleCatalog
src/AURA.Modules/ModuleDifficulty.cs:6:    public enum ModuleDifficulty
src/AURA.Modules/ModuleInfo.cs:16:    public sealed class ModuleInfo : IModule
src/AURA.Modules/ModuleManager.cs:18:    public sealed class ModuleManager
src/AURA.Modules/ModuleStatus.cs:7:    public enum ModuleStatus
src/AURA.Modules/Runtime/BinaryPath.cs:7:public static class BinaryPath
src/AURA.Modules/Runtime/CompatibilityChecker.cs:11:public sealed class CompatibilityChecker : ICompatibilityChecker
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:12:public sealed class DependencyAnalyzer : IDependencyAnalyzer
src/AURA.Modules/Runtime/Installer.cs:11:public sealed class Installer : IRuntimeInstaller
src/AURA.Modules/Runtime/LanguageDetector.cs:11:public sealed class LanguageDetector : IRuntimeDetector
src/AURA.Modules/Runtime/RuntimeCatalog.cs:11:public static class RuntimeCatalog
src/AURA.Modules/Runtime/RuntimeCatalog.cs:13:    public sealed record LanguageDefinition(
src/AURA.Modules/Runtime/RuntimeManager.cs:12:public sealed class RuntimeManager : IRuntimeManager
src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs:12:public sealed class RuntimeProcessExecutor : ProcessExecutorBase
src/AURA.Modules/Runtime/RuntimeResolver.cs:12:public sealed class RuntimeResolver : IRuntimeResolver
src/AURA.Modules/Runtime/SyntaxValidator.cs:11:public sealed class SyntaxValidator : ISyntaxValidator
src/AURA.Network/NetworkManager.cs:13:    public sealed class NetworkManager
src/AURA.Network/NetworkStatus.cs:6:    public class NetworkStatus
src/AURA.SystemInfo/SystemAnalyzer.cs:12:    public sealed class SystemAnalyzer
src/AURA.SystemInfo/SystemDiagnosticsResult.cs:6:    public class SystemDiagnosticsResult

## 11. Índice inicial de componentes

- **AI/orquestração**: 8 arquivos candidatos
- **Memória**: 5 arquivos candidatos
- **Ferramentas**: 3 arquivos candidatos
- **Execução**: 36 arquivos candidatos
- **Módulos**: 31 arquivos candidatos
- **Diagnóstico**: 7 arquivos candidatos
- **Mobile**: 33 arquivos candidatos

## 12. Classificação para decisão

| Categoria | Critério | Ação |
|---|---|---|
| 🟢 Núcleo | Participa do fluxo usuário → AgentSession → ferramenta/execução | manter |
| 🟡 Capacidade | Existe e possui referências, mas ainda não foi validada em execução | testar |
| 🔵 Infraestrutura | Runtime, DI, logging, módulos, configuração ou plataforma | manter até provar redundância |
| 🟠 Isolado | Não foram encontradas chamadas/referências claras | investigar |
| 🔴 Redundante | Outra implementação comprovadamente substitui a capacidade | candidato a remoção |

## Próxima arquitetura

A AURA deve evoluir para:

usuário
→ AgentSession
→ memória/soluções conhecidas
→ capacidades/ferramentas
→ execução
→ verificação
→ solução validada

A IA decide e raciocina quando necessário; a AURA fornece as capacidades e executa.

## Regra

Não remover código apenas porque parece não utilizado. Primeiro provar por busca estática + teste mínimo que a capacidade é redundante.
