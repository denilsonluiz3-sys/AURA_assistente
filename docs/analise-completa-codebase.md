# AURA Codebase - Análise Completa (2026-08-12)

## 1. Estrutura de Diretórios (src/ e tests/)

```
src/
├── AURA.Abstractions/
│   ├── AURA.Abstractions.csproj
│   ├── Execution/
│   │   ├── ExecutionRequest.cs
│   │   ├── ExecutionResult.cs
│   │   └── IToolExecutor.cs
│   └── Runtime/
│       ├── RuntimeInterfaces.cs
│       └── RuntimeModels.cs
│
├── AURA.Agents/
│   ├── AURA.Agents.csproj
│   ├── AgentManager.cs
│   ├── AIAgent.cs
│   ├── AutomationAgent.cs
│   └── MemoryAgent.cs
│
├── AURA.AI/
│   ├── AURA.AI.csproj
│   ├── AgentChat.cs
│   ├── AgentSession.cs
│   ├── AgentTool.cs
│   ├── AgentToolResult.cs
│   ├── AiAssistant.cs
│   ├── AiAssistantService.cs
│   ├── OpenRouterClient.cs
│   ├── ProviderCatalog.cs
│   ├── ToolRegistry.cs
│   ├── AgentTools/
│   │   ├── FileTools.cs
│   │   ├── SearchFilesTool.cs
│   │   ├── ShellAgentTool.cs
│   │   └── WorkspaceAgentTool.cs
│   └── Providers/
│       ├── AiApiFormat.cs
│       ├── ApiKeyProviderResolver.cs
│       ├── IAiProvider.cs
│       ├── IApiKeyProviderResolver.cs
│       ├── ProviderCredential.cs
│       ├── ProviderDetectionResult.cs
│       └── ProviderHealthResult.cs
│
├── AURA.CLI/
│   ├── AURA.CLI.csproj
│   ├── Program.cs
│   └── Program.cs.bak-openai
│
├── AURA.Core/
│   ├── AURA.Core.csproj
│   ├── VersionInfo.cs
│   ├── Abstractions/
│   │   ├── IAgent.cs
│   │   ├── ICommand.cs
│   │   ├── IModule.cs
│   │   ├── IPlugin.cs
│   │   └── IService.cs
│   ├── Bootstrap/
│   │   └── AuraBootstrap.cs
│   ├── Configuration/
│   │   ├── AuraConfiguration.cs
│   │   ├── ConfigLoader.cs
│   │   └── ModulesConfiguration.cs
│   ├── DependencyInjection/
│   │   └── ServiceContainer.cs
│   ├── Events/
│   │   ├── AuraEvents.cs
│   │   ├── EventBus.cs
│   │   └── IEvent.cs
│   ├── Launchers/
│   │   ├── CellCommand.cs
│   │   ├── DllLauncher.cs
│   │   ├── GoLauncher.cs
│   │   ├── ILauncher.cs
│   │   ├── JarLauncher.cs
│   │   ├── NodeLauncher.cs
│   │   ├── PythonLauncher.cs
│   │   └── Runner.cs
│   ├── Logging/
│   │   ├── ConsoleLogger.cs
│   │   ├── FileLogger.cs
│   │   └── ILogger.cs
│   └── Runtime/
│       ├── Cell.cs
│       ├── CellState.cs
│       ├── CellStore.cs
│       ├── DirectoryCellBackend.cs
│       ├── ICellBackend.cs
│       ├── PluginWatcher.cs
│       ├── ResourceLimits.cs
│       └── SimulationRuntime.cs
│
├── AURA.Installer/
│   ├── AURA.Installer.csproj
│   ├── ArtifactAnalysisService.cs
│   ├── ArtifactIdentification.cs
│   ├── ArtifactType.cs
│   ├── DependencyReport.cs
│   ├── EnvironmentSelectionResult.cs
│   ├── EnvironmentSelectionService.cs
│   ├── FileIdentifier.cs
│   ├── IDependencyAnalyzer.cs
│   ├── IEnvironmentSelector.cs
│   ├── IFileIdentifier.cs
│   ├── IInstaller.cs
│   ├── InstallationResult.cs
│   ├── InstallationService.cs
│   ├── PythonDependencyAnalyzer.cs
│   ├── PythonEnvironmentSelector.cs
│   ├── PythonInstaller.cs
│   └── PythonStdlibModules.cs
│
├── AURA.Memory/
│   ├── AURA.Memory.csproj
│   ├── MemoryEntry.cs
│   └── MemoryStore.cs
│
├── AURA.Mobile/
│   ├── AURA.Mobile.csproj
│   ├── App.xaml.cs
│   ├── MainPage.cs
│   ├── MauiProgram.cs
│   ├── RoleToColorConverter.cs
│   ├── Controls/
│   │   └── AiConfigView.cs
│   ├── Diagnostics/
│   │   ├── AgentWorkspace.cs
│   │   ├── FixProposal.cs
│   │   ├── ProjectAccessService.cs
│   │   ├── RuntimeConfig.cs
│   │   └── SearchCatalog.cs
│   ├── Pages/
│   │   ├── AgentPage.xaml.cs
│   │   ├── BrowserPage.xaml.cs
│   │   ├── BrowserSettingsPage.cs
│   │   ├── CellsPage.xaml.cs
│   │   ├── ChatPage.xaml.cs
│   │   ├── ExecutorsPage.xaml.cs
│   │   ├── FixesPage.xaml.cs
│   │   ├── HomePage.xaml.cs
│   │   ├── ImageSearchPage.xaml.cs
│   │   ├── LogsPage.xaml.cs
│   │   ├── MemoryPage.xaml.cs
│   │   ├── ModulesPage.xaml.cs
│   │   ├── RunPage.xaml.cs
│   │   ├── SectionPage.cs
│   │   └── TerminalPage.xaml.cs
│   ├── Platforms/Android/
│   │   ├── AuraLog.cs
│   │   ├── MainActivity.cs
│   │   ├── MainApplication.cs
│   │   ├── StoragePermissionHelper.cs
│   │   ├── VoiceFloatingButton.cs
│   │   ├── VpnHelper.cs
│   │   └── WebView/
│   │       ├── AuraDownloadListener.cs
│   │       └── AuraLongClickListener.cs
│   ├── Resources/Raw/
│   │   └── kokoro-config.json
│   ├── Speech/
│   │   ├── AndroidTtsSpeechService.cs
│   │   ├── HybridSpeechService.cs
│   │   ├── ISpeechService.cs
│   │   ├── KokoroPhonemizer.cs
│   │   ├── KokoroSpeechService.cs
│   │   ├── KokoroVocab.cs
│   │   └── VoiceAssistantService.cs
│   └── ViewModels/
│       └── ModuleRow.cs
│
├── AURA.Modules/
│   ├── AURA.Modules.csproj
│   ├── ModuleCatalog.cs
│   ├── ModuleDifficulty.cs
│   ├── ModuleInfo.cs
│   ├── ModuleManager.cs
│   ├── ModuleStatus.cs
│   ├── Executors/
│   │   ├── GitExecutor.cs
│   │   ├── NodeExecutor.cs
│   │   ├── ProcessExecutorBase.cs
│   │   ├── PythonExecutor.cs
│   │   └── ShellExecutor.cs
│   ├── Loja/
│   │   ├── LockHelper.cs
│   │   ├── LojaLocalResolver.cs
│   │   └── LojaUninstaller.cs
│   └── Runtime/
│       ├── BinaryPath.cs
│       ├── CompatibilityChecker.cs
│       ├── DependencyAnalyzer.cs
│       ├── Installer.cs
│       ├── LanguageDetector.cs
│       ├── RuntimeCatalog.cs
│       ├── RuntimeManager.cs
│       ├── RuntimeProcessExecutor.cs
│       ├── RuntimeResolver.cs
│       └── SyntaxValidator.cs
│
├── AURA.Network/
│   ├── AURA.Network.csproj
│   ├── NetworkManager.cs
│   └── NetworkStatus.cs
│
├── AURA.SystemInfo/
│   ├── AURA.SystemInfo.csproj
│   ├── SystemAnalyzer.cs
│   └── SystemDiagnosticsResult.cs
│
├── AURA.Windows/
│   ├── AURA.Windows.csproj
│   └── README.md
│
└── (no AURA.Mobile entry in .sln)

tests/
└── AURA.Tests/
    ├── AURA.Tests.csproj
    ├── AgentManagerTests.cs
    ├── AgentSessionReasoningTests.cs
    ├── AgentToolsTests.cs
    ├── ApiKeyProviderResolverTests.cs
    ├── ConcreteAgentsTests.cs
    ├── EndToEndRunTests.cs
    ├── ExecutorsTests.cs
    ├── InstallerTests.cs
    ├── LockHelperTests.cs
    ├── LojaLocalResolverTests.cs
    ├── ModuleCatalogTests.cs
    ├── ModuleFlowTests.cs
    ├── ModuleManagerTests.cs
    ├── ServiceContainerTests.cs
    ├── SystemAnalyzerTests.cs
    └── ToolRegistryTests.cs
```

---

## 2. Resumo de Cada Projeto

| Projeto | Tipo | LOC | Propósito |
|---------|------|-----|-----------|
| **AURA.Abstractions** | classlib | ~320 | Interfaces e modelos compartilhados: `IToolExecutor`, `ExecutionRequest/Result`, `IRuntimeDetector`, `IRuntimeResolver`, etc. Zero dependências. |
| **AURA.Core** | classlib | ~1,500 | Engine principal: cell runtime (`SimulationRuntime`), launchers (Python, Node, Java, .NET, Go), `Runner` (resolve launcher por extensão), logging, eventos (`EventBus`), DI (`ServiceContainer`), config, bootstrap, plugin hot-reload (`PluginWatcher`), abstrações (`IAgent`, `IPlugin`, `IModule`, `ICommand`, `IService`). |
| **AURA.CLI** | exe | ~940 | CLI interativa com 20+ comandos: `run`, `cells`, `cell`, `diagnostico`, `internet`, `agents`, `ask`, `chat`, `agent`, `exec`, `install`, `remove`, `update`, `modulos`, `config`, `launchers`, `plugins`, `aichave`, `persist`, `help`. |
| **AURA.AI** | classlib | ~2,050 | Integração LLM: `OpenRouterClient`, `AgentSession` (loop agentic com tool calling), `AgentChat`, `ToolRegistry`, `ProviderCatalog` (12 providers), `ApiKeyProviderResolver`, `AiAssistant`/`AiAssistantService`, agent tools (`ShellAgentTool`, `SearchFilesTool`, `FileTools`). |
| **AURA.Agents** | classlib | ~540 | Camada de abstração de agentes: `AIAgent` (wraps assistentes CLI externos), `AutomationAgent` (comandos shell), `MemoryAgent` (consulta memória). `AgentManager` orquestra aichat, termux-ai, opencode como células AURA. |
| **AURA.Modules** | classlib | ~2,100 | Sistema de módulos de capacidade: `ModuleCatalog` (12 módulos), `ModuleManager` (download/aplicar/remover), `LojaLocalResolver`, `LojaUninstaller`, `LockHelper`. Executors (`ShellExecutor`, `GitExecutor`, `PythonExecutor`, `NodeExecutor` via `ProcessExecutorBase`). Pipeline de runtime (`RuntimeManager`: detecção -> resolução -> dependências -> sintaxe -> compatibilidade -> instalar -> executar). |
| **AURA.Memory** | classlib | ~200 | Journal persistente de conversas/eventos. `MemoryStore` (append-only, JSON, atomic writes), `MemoryEntry` (Turn/CellEvent). |
| **AURA.Network** | classlib | ~85 | Verificação de conectividade: ping 8.8.8.8, IP local, `NetworkInterface.GetIsNetworkAvailable`. |
| **AURA.SystemInfo** | classlib | ~170 | Diagnóstico de sistema: SO, arquitetura, CPU, RAM (/proc/meminfo no Linux, kernel32 no Windows), disco. |
| **AURA.Installer** | classlib | ~700 | "Intelligent Installer": `FileIdentifier` (magic bytes + extensão + conteúdo), `PythonDependencyAnalyzer` (import scan + requirements.txt), `PythonEnvironmentSelector` (disco + runtime), `PythonInstaller` (pip dry-run/real), `ArtifactAnalysisService` (estágios 1-3), `InstallationService` (estágio 4). |
| **AURA.Windows** | classlib | 0 | Placeholder vazio para futuro "Assistente Windows". |
| **AURA.Mobile** | exe (MAUI Android) | ~1,500+ | App MAUI Android. Páginas: Home, Chat, Agent, Browser, Terminal, Logs, Fixes, Modules, Cells, Executors, Memory, ImageSearch, BrowserSettings, Run, Section. Voice assistant (Kokoro TTS via ONNX, Android TTS fallback, hybrid), VPN/Tor, WebView, diagnostics, module management. |
| **AURA.Tests** | xunit | ~3,500 | 16 arquivos de teste cobrindo todos os projetos. |

---

## 3. Funcionalidades Implementadas vs Incompletas

### Totalmente Implementadas

1. **Cell Runtime (SimulationRuntime)** - Ciclo de vida completo: criar, iniciar, parar, pausar (SIGSTOP), resume (SIGCONT), deletar, reciclar em crash, persistência JSON, recuperação de órfãos, limites de recursos via `prlimit`, logs por célula, captura concorrente de stdout/stderr.

2. **Sistema de Launchers** - `Runner` resolve `.py` (Python), `.jar` (Java), `.dll` (.NET), `.js`/`.mjs` (Node), `.go` (Go) por extensão.

3. **CLI Front-End** - REPL interativo com 20+ comandos, parse de argumentos, gerenciamento de células, consulta a agentes, install/remove/update de módulos, diagnóstico, network check.

4. **Plugin Hot-Reload** - `PluginWatcher` com `AssemblyLoadContext` coletável, file system watcher, descoberta dinâmica de `ILauncher` e `IPlugin`.

5. **Event Bus** - Pub/sub in-process com `EventBus`, eventos tipados: `CellStateChangedEvent`, `AssistantRespondedEvent`, `ExecutorCompletedEvent`, `ModuleStateChangedEvent`.

6. **Service Container** - Container DI mínimo com singleton instance e factory registration.

7. **Sistema de Configuração** - Configurações baseadas em JSON, auto-criação de arquivos default, saves atômicos.

8. **Logging** - `ILogger` com `ConsoleLogger` (colorido) e `FileLogger` (rolling file).

9. **System Diagnostics** - SO, arquitetura, CPU count, RAM, disco, check de requisitos mínimos.

10. **Network Check** - Ping 8.8.8.8, IP local, network availability.

11. **LLM Integration (OpenRouterClient)** - Chat completions com function calling, timeout, classificação de erro (401/402/429/5xx), preservação de reasoning details para Gemini.

12. **Agentic Loop (AgentSession)** - Loop multi-turn com tool calling (max 20 rounds), tool registry, streaming step events, persistência em memória.

13. **Agent Tools** - `ShellAgentTool`, `SearchFilesTool` (grep), `ListDirTool`, `ReadFileTool`, `WriteFileTool`, `EditFileTool` - todos com sandboxing de workspace (path traversal prevention).

14. **Tool Registry** - Register, resolve, try-register, definitions export.

15. **Provider Catalog** - 12 providers: OpenRouter, OpenAI, Google Gemini, Groq, Cerebras, xAI, DeepSeek, Mistral, Together AI, Ollama (local), Custom.

16. **API Key Provider Detection** - Determinística por prefixo, fallback ambíguo para preferred provider, probe de rede só com `AllowProbe=true`.

17. **External Assistant Integration** - `AgentManager` descobre e executa aichat, termux-ai, opencode como células AURA.

18. **Concrete Agents** - `MemoryAgent` (memória), `AutomationAgent` (shell), `AIAgent` (assistentes externos).

19. **Memory Store** - Journal append-only de turns e eventos, persistido em `~/AURA/memory.json`.

20. **Module Catalog** - 12 módulos com metadados. 2 core, 7 downloadáveis, 3 planejados.

21. **Module Manager** - Download (GitHub raw), apply (enable), remove (disable + uninstall).

22. **Local Package Store (Loja)** - `LojaLocalResolver` (instala de manifest local + payloads com file locking). `LojaUninstaller` (remove seguramente).

23. **Executors** - `ShellExecutor`, `GitExecutor`, `PythonExecutor`, `NodeExecutor` via `ProcessExecutorBase`.

24. **Runtime Pipeline (RuntimeManager)** - Pipeline completa: detect language -> resolve runtime -> analyze dependencies -> validate syntax -> check compatibility -> install -> execute.

25. **Intelligent Installer (AURA.Installer)** - Stage 1: `FileIdentifier`. Stage 2: `PythonDependencyAnalyzer`. Stage 3: `PythonEnvironmentSelector`. Stage 4: `PythonInstaller`.

26. **MAUI Android App (AURA.Mobile)** - App multi-página com voice assistant, browser, chat, agent, terminal, logs, fixes, modules, cells, executors, memory, image search, VPN/Tor, project access diagnostics.

### Incompletas/Stubbed

1. **AURA.Windows** - Placeholder vazio. Projeto referenciado pelo CLI mas sem código.
2. **Plugin System** - `IPlugin` definido, `PluginWatcher` carrega assemblies, mas nenhum plugin real existe.
3. **Automation Module** - ModuleFlags.Automation existe, módulo catalogado como Planejado.
4. **Windows Module** - ModuleFlags.Windows existe, módulo Planejado. Sem WMI, Registry, PowerShell.
5. **GitExecutor Convenience Methods** - Comentário: "CreateBranchAsync, CommitAsync, etc. ficam para quando o pipeline SelfDev estiver mais próximo."
6. **ICommand** - Interface nunca usada.
7. **IService** - Interface nunca usada.
8. **IModule** - Interface implementada por ModuleInfo mas nunca usada como tipo.
9. **Installer Pipeline Stages 4b-7** - Comentário: "stages 4 to 7 (install, configure, execute, manage) come as new methods/services in the next phases."
10. **AURA.CLI Program.cs.bak-openai** - Backup, não compila.

---

## 4. Grafo de Dependências

```
AURA.Abstractions (sem deps)
    |
    +-- AURA.Core (sem deps)
    |       |
    |       +-- AURA.Memory -> Core
    |       +-- AURA.AI -> Abstractions, Core, Memory
    |       +-- AURA.CLI -> Core, SystemInfo, Network, Modules, Agents, AI, Windows
    |       +-- AURA.Agents -> Abstractions, Core, Memory
    |       +-- AURA.Modules -> Abstractions, Core
    |       |       |
    |       |       +-- AURA.Installer -> Modules, SystemInfo
    |       +-- AURA.Windows -> Core
    |
    +-- AURA.Network (sem deps)
    +-- AURA.SystemInfo (sem deps)

AURA.Mobile -> Abstractions, Core, Modules, Agents, Memory, AI, Network, SystemInfo
AURA.Tests -> Abstractions, Agents, Core, SystemInfo, Network, Modules, AI, Installer, Memory
```

---

## 5. Código Morto/Não Referenciado

### Definido mas nunca referenciado:
1. **ICommand** (`AURA.Core.Abstractions`) - Interface nunca implementada ou usada.
2. **IService** (`AURA.Core.Abstractions`) - Nunca implementada. ServiceContainer não a usa.
3. **IPlugin** (`AURA.Core.Abstractions`) - PluginWatcher procura implementações, mas nenhuma existe.
4. **IModule** (`AURA.Core.Abstractions`) - Tecnicamente implementada por ModuleInfo, mas nunca usada como tipo.

### Arquivos órfãos:
1. **`AURA.Windows/`** - Projeto sem arquivos .cs.
2. **`Program.cs.bak-openai`** - Backup não compilado.
3. **`modules/packages/`** - Manifests JSON servidos remotamente, não lidos localmente.
4. **`config/providers.json`** - Existe mas nunca é lido (ProviderCatalog é hardcoded).

### Cobertura de testes faltante:
- `MemoryStore`, `MemoryEntry` (testado indiretamente)
- `SimulationRuntime` (indireto)
- `Runner` (indireto)
- `PluginWatcher` (sem testes)
- `AuraBootstrap`, `ConfigLoader`, `AuraConfiguration`, `ModulesConfiguration` (sem testes)
- `EventBus` (sem testes)
- `AiAssistant`, `AiAssistantService` (sem testes)
- `OpenRouterClient` (indireto)
- `AgentSession` (indireto)
- `LojaUninstaller` (indireto)
- `RuntimeManager` + pipeline completa (sem testes)
- `ProcessExecutorBase` (indireto)
- `PythonStdlibModules` (sem testes)

### Código redundante:
1. **`PythonStdlibModules` (Installer)** e **`DependencyAnalyzer.PythonStdlib` (Modules)** - Mesma lista de módulos stdlib (~90 entries), em projetos diferentes.
2. **`BinaryPath.FindOnPath` (Modules.Runtime)** e **`ProcessExecutorBase.ResolveBinary` (Modules.Executors)** - Mesma função de busca PATH.
3. **`AgentManager.ResolveExecutable` (Agents)** e **`PythonLauncher.FindOnPath` (Core)** - Mesma função.