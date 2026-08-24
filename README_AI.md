# AURA_assistente — Guia para Agentes de IA

Este arquivo é a entrada técnica recomendada para agentes de IA que precisam analisar, modificar, testar ou revisar o código-fonte do AURA_assistente.

## Objetivo

AURA_assistente é um assistente pessoal multiplataforma construído em .NET MAUI, com arquitetura modular. O código existente é a fonte de verdade; esta documentação não deve ser usada para justificar uma arquitetura paralela.

## Estrutura principal

```text
src/
├── AURA.Abstractions/   Contratos e abstrações compartilhadas
├── AURA.Core/           Núcleo, logging e componentes fundamentais
├── AURA.Memory/         Memória e armazenamento de soluções/regras
├── AURA.Agents/         Orquestração, agentes, intenção, políticas e programas
├── AURA.AI/             Integração com provedores/modelos de IA
├── AURA.Modules/        Metadados e ciclo de vida de módulos
├── AURA.Mobile/         Aplicação .NET MAUI e integração Android
└── AURA.Windows/        Componentes da plataforma Windows

tests/                   Testes automatizados
AURA.sln                Solution principal
.github/workflows/      CI/CD e validações
```

Confirme os nomes e responsabilidades no código antes de fazer alterações estruturais.

## Regras de dependência

- `AURA.Abstractions` deve permanecer independente de Android, MAUI e implementações concretas.
- `AURA.Core` contém fundamentos reutilizáveis e não deve depender da UI móvel.
- `AURA.Memory` fornece memória sem depender da UI.
- `AURA.Agents` concentra agentes, intenção, políticas, ferramentas e orquestração.
- `AURA.AI` integra modelos/provedores sem tornar um provedor específico obrigatório para o Kernel.
- `AURA.Mobile` contém UI e adapters específicos de Android/MAUI.
- Serviços Android não devem ser expostos diretamente por contratos em `AURA.Abstractions`.

Antes de adicionar uma referência entre projetos, verifique os `.csproj` reais.

## Fluxo do Kernel

O Kernel é distribuído pelos módulos existentes; não crie um projeto artificial `AURA.Kernel` sem necessidade arquitetural comprovada.

```text
Entrada do usuário
      ↓
AuraOrchestrator
      ↓
Memória / intenção
      ↓
PolicyGuard
      ↓
ToolResolver / programa / ferramenta
      ↓
Runner / execução
      ↓
Resultado
      ↓
UI ou resposta do agente
```

LLM/provedores são componentes de inteligência/inferência e não devem ser requisito obrigatório para operações determinísticas.

## Segurança

Toda execução que use capacidades deve passar pela política antes da execução:

```text
Resolver → identificar capacidades → PolicyGuard → executar
```

Uma capacidade desconhecida deve ser bloqueada por padrão. A UI não deve contornar o `PolicyGuard`.

## UI unificada (navegação Mobile)

A UI Mobile está organizada em seções no `MainPage` (TabbedPage + `SectionPage`), sem remover páginas existentes:

```text
Sistema
  Início (HomePage) · Ecossistema · Diagnóstico (hub Sistema) · Correções
Assistente
  Chat · Agente · Memória · Navegador
Ferramentas
  Terminal · Executores · Módulos · Logs
Apps
  Programas · Células · Rodar programa
```

Arquivos-chave:

- `src/AURA.Mobile/MainPage.cs` — registro de seções e `NavigateToProcessAsync`
- `src/AURA.Mobile/Pages/SectionPage.cs` — grade 2×N por seção
- `src/AURA.Mobile/Pages/HomePage.xaml(.cs)` — status + atalhos + entrada de comando
- `src/AURA.Mobile/Pages/DiagnosticoPage.xaml(.cs)` — hub Sistema + Device Diagnostic via Cell Program
- `src/AURA.Mobile/Pages/ProgramsPage.xaml` + `ViewModels/ProgramsPageViewModel.cs` — catálogo de programas
- `src/AURA.Mobile/Pages/ChatPage.xaml.cs` — intent → `IOrchestrator` antes do fluxo IA

Home e Assistente não reimplementam lógica de programa: encaminham para o orquestrador / navegação existente.

## Cell Programs

Cell Programs são programas internos controlados. A V1 não representa isolamento real de processos ou sandbox de segurança.

Componentes principais:

- `IAuraCellProgram`
- `IAuraCellContext`
- `IAuraCellContextFactory`
- `CellProgramRegistry`
- `CellProgramRunner`
- `DeviceDiagnosticProgram`

Cada programa declara `RequiredCapabilities`.

O contexto de `AURA.Abstractions` não deve expor `IAndroidCapabilityService`. A integração Android ocorre por adapter/contexto em `AURA.Mobile`.

Fluxo:

```text
Apps → Programas → ProgramsPage
                  ↓
            ProgramCardViewModel
                  ↓
             PolicyGuard
                  ↓
          CellProgramRunner
                  ↓
           IAuraCellContext
                  ↓
            Adapter Android
                  ↓
       IAndroidCapabilityService
                  ↓
          resultado no card
```

O hub Sistema (`DiagnosticoPage`) também pode disparar `device-diagnostic` pelo mesmo caminho PolicyGuard → Runner.

## UI de Programas

Arquivos principais:

- `src/AURA.Mobile/Pages/ProgramsPage.xaml`
- `src/AURA.Mobile/Pages/ProgramsPage.xaml.cs`
- `src/AURA.Mobile/ViewModels/ProgramsPageViewModel.cs`

Estados do card: Disponível · Executando · Concluído · Bloqueado · Requer confirmação · Indisponível · Erro.

A UI não deve chamar diretamente serviços Android quando existe um programa/runner/policy para a operação.

## IntentResolver

O resolvedor transforma comandos em intenções estruturadas. Exemplos V1:

```text
"faça um diagnóstico" / "diagnostique meu celular"
        ↓
intent = android, action = device-diagnostic
        ↓
CellProgramRegistry → PolicyGuard → Runner

"abra o terminal"
        ↓
intent = navigate, page = Terminal
        ↓
MainPage.NavigateToProcessAsync
```

Consumidores: `AuraOrchestrator`, `ChatPage`, `VoiceAssistantService` (após STT).

Localize primeiro o resolver usado pelo orquestrador; não duplique regras em camadas diferentes.

## Voz (TTS + STT)

- FAB nativo: `Platforms/Android/VoiceFloatingButton.cs` (topo direito)
- TTS: `ISpeechService` → `HybridSpeechService` / `AndroidTtsSpeechService`
- STT: `ISpeechRecognitionService` → `AndroidSpeechRecognitionService` (`SpeechRecognizer`, pt-BR)
- Orquestração de voz: `VoiceAssistantService` (escuta → intent/orquestrador → fala)
- Permissão: `RECORD_AUDIO` + `Permissions.Microphone`

Toque no FAB inicia escuta; segundo toque cancela. JSON longo é resumido na fala.

## Memória

`AURA.Memory` contém os mecanismos existentes, incluindo `MemoryStore`, `MemoryEntry` e regras/soluções. Antes de criar outro sistema de memória, procure e reutilize o existente quando compatível.

## IA e provedores

`AURA.AI` contém componentes de sessão/agente, resultados de ferramentas e runtime/catalogação de provedores. Falha de um provedor não deve ser confundida com falha estrutural do Kernel.

## DI

`src/AURA.Mobile/MauiProgram.cs` é um ponto central de composição MAUI. Ao adicionar serviços, confirme contrato, implementação, projeto correto, consumidores e construtores antes de registrar no DI.

Não injete uma implementação Android em um projeto que deve permanecer multiplataforma.

## Testes

Priorize testes para resolução de intenção, autorização de capacidades, bloqueio de capacidades desconhecidas, registry, runner e regras arquiteturais. Testes existentes de Cell Programs cobrem `device-diagnostic`, capacidades permitidas/bloqueadas e resolução case-insensitive do registry.

## CI/CD

GitHub Actions executa build, testes e análises remotamente. Ao diagnosticar CI, identifique workflow/job, leia a etapa que falhou, classifique a causa como código/dependência/SDK/workload/configuração, corrija a causa e valide novamente.

## Procedimento recomendado para uma IA

Antes de modificar código:

1. Leia `AURA.sln`.
2. Liste os projetos `src/*` e `tests/*`.
3. Leia os `.csproj` relevantes.
4. Localize interfaces e implementações existentes.
5. Siga os consumidores do componente alterado.
6. Verifique `MauiProgram.cs`.
7. Verifique `AuraOrchestrator`.
8. Verifique `PolicyGuard` quando houver execução de capacidades.
9. Procure testes existentes.
10. Faça a menor alteração necessária.

## Regras para alterações automatizadas

- Não duplicar interfaces existentes.
- Não criar dependências Android em abstrações.
- Não mover código entre camadas sem necessidade comprovada.
- Não criar implementações especulativas quando já existe uma implementação funcional.
- Não contornar `PolicyGuard`.
- Não adicionar scripts temporários de teste ao produto sem necessidade.
- Não colocar segredos ou API keys no código.
- Preferir mudanças pequenas e verificáveis.
- Atualizar testes quando o comportamento mudar.
- Usar CI como validação final.

## Fonte de verdade

Este documento é um mapa para agentes de IA, não uma cópia do código. O código-fonte atual sempre tem precedência. Se houver divergência, a IA deve verificar o código e atualizar esta documentação quando a mudança arquitetural for intencional.

## Ordem recomendada de leitura

```text
AURA.sln
  ↓
*.csproj relevantes
  ↓
AURA.Abstractions
  ↓
AURA.Core
  ↓
AURA.Memory
  ↓
AURA.Agents
  ↓
AURA.AI
  ↓
AURA.Mobile/MauiProgram.cs
  ↓
AURA.Mobile/MainPage.cs
  ↓
testes relacionados
  ↓
.github/workflows/*
```

<!-- AI-DOCS:START -->
## Snapshot automático do código

> Esta seção é regenerada pelo GitHub Actions após mudanças no código. O código-fonte continua sendo a fonte de verdade.

- Commit: `9592388`
- Data UTC: `2026-08-24 13:27:31 UTC`
- Branch: `main`

### Projetos

- `src/AURA.AI/AURA.AI.csproj`
- `src/AURA.Abstractions/AURA.Abstractions.csproj`
- `src/AURA.Agents/AURA.Agents.csproj`
- `src/AURA.CLI/AURA.CLI.csproj`
- `src/AURA.Core/AURA.Core.csproj`
- `src/AURA.Installer/AURA.Installer.csproj`
- `src/AURA.Memory/AURA.Memory.csproj`
- `src/AURA.Mobile/AURA.Mobile.csproj`
- `src/AURA.Modules/AURA.Modules.csproj`
- `src/AURA.Network/AURA.Network.csproj`
- `src/AURA.SystemInfo/AURA.SystemInfo.csproj`
- `src/AURA.Windows/AURA.Windows.csproj`
- `tests/AURA.Tests/AURA.Tests.csproj`

### Estrutura de código

- `src/AURA.AI`: 29 arquivos C#, 0 arquivos XAML
- `src/AURA.Abstractions`: 16 arquivos C#, 0 arquivos XAML
- `src/AURA.Agents`: 16 arquivos C#, 0 arquivos XAML
- `src/AURA.CLI`: 1 arquivos C#, 0 arquivos XAML
- `src/AURA.Core`: 39 arquivos C#, 0 arquivos XAML
- `src/AURA.Installer`: 17 arquivos C#, 0 arquivos XAML
- `src/AURA.Memory`: 5 arquivos C#, 0 arquivos XAML
- `src/AURA.Mobile`: 60 arquivos C#, 34 arquivos XAML
- `src/AURA.Modules`: 23 arquivos C#, 0 arquivos XAML
- `src/AURA.Network`: 2 arquivos C#, 0 arquivos XAML
- `src/AURA.SystemInfo`: 2 arquivos C#, 0 arquivos XAML
- `src/AURA.Windows`: 0 arquivos C#, 0 arquivos XAML

### Referências entre projetos

**src/AURA.AI/AURA.AI.csproj**
- ../AURA.Abstractions/AURA.Abstractions.csproj
- ../AURA.Core/AURA.Core.csproj
- ../AURA.Memory/AURA.Memory.csproj

**src/AURA.Agents/AURA.Agents.csproj**
- ../AURA.Abstractions/AURA.Abstractions.csproj
- ../AURA.Core/AURA.Core.csproj
- ../AURA.Memory/AURA.Memory.csproj
- ../AURA.AI/AURA.AI.csproj

**src/AURA.CLI/AURA.CLI.csproj**
- ../AURA.Core/AURA.Core.csproj
- ../AURA.SystemInfo/AURA.SystemInfo.csproj
- ../AURA.Network/AURA.Network.csproj
- ../AURA.Modules/AURA.Modules.csproj
- ../AURA.Agents/AURA.Agents.csproj
- ../AURA.AI/AURA.AI.csproj
- ../AURA.Windows/AURA.Windows.csproj

**src/AURA.Installer/AURA.Installer.csproj**
- ../AURA.Modules/AURA.Modules.csproj
- ../AURA.SystemInfo/AURA.SystemInfo.csproj

**src/AURA.Memory/AURA.Memory.csproj**
- ../AURA.Core/AURA.Core.csproj

**src/AURA.Mobile/AURA.Mobile.csproj**
- ../AURA.Abstractions/AURA.Abstractions.csproj
- ../AURA.Core/AURA.Core.csproj
- ../AURA.Modules/AURA.Modules.csproj
- ../AURA.Agents/AURA.Agents.csproj
- ../AURA.Memory/AURA.Memory.csproj
- ../AURA.AI/AURA.AI.csproj
- ../AURA.Network/AURA.Network.csproj
- ../AURA.SystemInfo/AURA.SystemInfo.csproj

**src/AURA.Modules/AURA.Modules.csproj**
- ../AURA.Abstractions/AURA.Abstractions.csproj
- ../AURA.Core/AURA.Core.csproj

**src/AURA.Windows/AURA.Windows.csproj**
- ../AURA.Core/AURA.Core.csproj

**tests/AURA.Tests/AURA.Tests.csproj**
- ../../src/AURA.Abstractions/AURA.Abstractions.csproj
- ../../src/AURA.Agents/AURA.Agents.csproj
- ../../src/AURA.Core/AURA.Core.csproj
- ../../src/AURA.SystemInfo/AURA.SystemInfo.csproj
- ../../src/AURA.Network/AURA.Network.csproj
- ../../src/AURA.Modules/AURA.Modules.csproj
- ../../src/AURA.AI/AURA.AI.csproj
- ../../src/AURA.Installer/AURA.Installer.csproj
- ../../src/AURA.Memory/AURA.Memory.csproj

### Workflows

- `.github/workflows/ai-failure-diagnostics.yml`
- `.github/workflows/build-android-apk.yml`
- `.github/workflows/cleanup-artifacts.yml`
- `.github/workflows/codeql.yml`
- `.github/workflows/sync-main.yml`
- `.github/workflows/update-ai-docs.yml`

### Arquivos de código relevantes

- `src/AURA.AI/AgentChat.cs`
- `src/AURA.AI/AgentSession.cs`
- `src/AURA.AI/AgentTool.cs`
- `src/AURA.AI/AgentToolResult.cs`
- `src/AURA.AI/AgentTools/AndroidCapabilityTool.cs`
- `src/AURA.AI/AgentTools/CodeExecutorTool.cs`
- `src/AURA.AI/AgentTools/CodeExtractorTool.cs`
- `src/AURA.AI/AgentTools/FileTools.cs`
- `src/AURA.AI/AgentTools/InterpretCommandTool.cs`
- `src/AURA.AI/AgentTools/SearchFilesTool.cs`
- `src/AURA.AI/AgentTools/SearchMemoryTool.cs`
- `src/AURA.AI/AgentTools/ShellAgentTool.cs`
- `src/AURA.AI/AgentTools/WebFetchTool.cs`
- `src/AURA.AI/AgentTools/WebSearchTool.cs`
- `src/AURA.AI/AgentTools/WorkspaceAgentTool.cs`
- `src/AURA.AI/AiAssistant.cs`
- `src/AURA.AI/AiAssistantService.cs`
- `src/AURA.AI/OpenRouterAiClientAdapter.cs`
- `src/AURA.AI/OpenRouterClient.cs`
- `src/AURA.AI/ProviderCatalog.cs`
- `src/AURA.AI/ProviderRuntime.cs`
- `src/AURA.AI/Providers/AiApiFormat.cs`
- `src/AURA.AI/Providers/ApiKeyProviderResolver.cs`
- `src/AURA.AI/Providers/IAiProvider.cs`
- `src/AURA.AI/Providers/IApiKeyProviderResolver.cs`
- `src/AURA.AI/Providers/ProviderCredential.cs`
- `src/AURA.AI/Providers/ProviderDetectionResult.cs`
- `src/AURA.AI/Providers/ProviderHealthResult.cs`
- `src/AURA.AI/ToolRegistry.cs`
- `src/AURA.Abstractions/CellProgramResult.cs`
- `src/AURA.Abstractions/Execution/ExecutionRequest.cs`
- `src/AURA.Abstractions/Execution/ExecutionResult.cs`
- `src/AURA.Abstractions/Execution/IToolExecutor.cs`
- `src/AURA.Abstractions/IAiClient.cs`
- `src/AURA.Abstractions/IAndroidCapabilityService.cs`
- `src/AURA.Abstractions/IAuraCellContext.cs`
- `src/AURA.Abstractions/IAuraCellContextFactory.cs`
- `src/AURA.Abstractions/IAuraCellProgram.cs`
- `src/AURA.Abstractions/IDeviceDiagnosticCapability.cs`
- `src/AURA.Abstractions/Orchestration/IOrchestrator.cs`
- `src/AURA.Abstractions/Process/IProcessOrchestrator.cs`
- `src/AURA.Abstractions/Process/ProcessState.cs`
- `src/AURA.Abstractions/Process/Verdict.cs`
- `src/AURA.Abstractions/Runtime/RuntimeInterfaces.cs`
- `src/AURA.Abstractions/Runtime/RuntimeModels.cs`
- `src/AURA.Agents/AIAgent.cs`
- `src/AURA.Agents/AgentManager.cs`
- `src/AURA.Agents/AuraOrchestrator.cs`
- `src/AURA.Agents/AutomationAgent.cs`
- `src/AURA.Agents/IntentResolver.cs`
- `src/AURA.Agents/LegalProcessEngine.cs`
- `src/AURA.Agents/MemoryAgent.cs`
- `src/AURA.Agents/PolicyGuard.cs`
- `src/AURA.Agents/Programs/CellProgramRegistry.cs`
- `src/AURA.Agents/Programs/CellProgramRunner.cs`
- `src/AURA.Agents/Programs/DeviceDiagnosticProgram.cs`
- `src/AURA.Agents/ToolResolver.cs`
- `src/AURA.Agents/Tools/AndroidTool.cs`
- `src/AURA.Agents/Tools/FileTool.cs`
- `src/AURA.Agents/Tools/RunTool.cs`
- `src/AURA.Agents/Tools/SearchTool.cs`
- `src/AURA.CLI/Program.cs`
- `src/AURA.Core/Abstractions/IAgent.cs`
- `src/AURA.Core/Abstractions/ICommand.cs`
- `src/AURA.Core/Abstractions/IModule.cs`
- `src/AURA.Core/Abstractions/IPlugin.cs`
- `src/AURA.Core/Abstractions/IService.cs`
- `src/AURA.Core/Abstractions/IWebSearch.cs`
- `src/AURA.Core/Bootstrap/AuraBootstrap.cs`
- `src/AURA.Core/Configuration/AuraConfiguration.cs`
- `src/AURA.Core/Configuration/ConfigLoader.cs`
- `src/AURA.Core/Configuration/ModulesConfiguration.cs`
- `src/AURA.Core/DependencyInjection/ServiceContainer.cs`
- `src/AURA.Core/Events/AuraEvents.cs`
- `src/AURA.Core/Events/EventBus.cs`
- `src/AURA.Core/Events/IEvent.cs`
- `src/AURA.Core/Events/OrchestrationStepEvent.cs`
- `src/AURA.Core/Knowledge/KnowledgeManager.cs`
- `src/AURA.Core/Launchers/CellCommand.cs`
- `src/AURA.Core/Launchers/DllLauncher.cs`
- `src/AURA.Core/Launchers/GoLauncher.cs`
- `src/AURA.Core/Launchers/ILauncher.cs`
- `src/AURA.Core/Launchers/JarLauncher.cs`
- `src/AURA.Core/Launchers/NodeLauncher.cs`
- `src/AURA.Core/Launchers/PythonLauncher.cs`
- `src/AURA.Core/Launchers/Runner.cs`
- `src/AURA.Core/Launchers/ShellLauncher.cs`
- `src/AURA.Core/Logging/ConsoleLogger.cs`
- `src/AURA.Core/Logging/FileLogger.cs`
- `src/AURA.Core/Logging/ILogger.cs`
- `src/AURA.Core/Runtime/Cell.cs`
- `src/AURA.Core/Runtime/CellNetworkPolicy.cs`
- `src/AURA.Core/Runtime/CellState.cs`
- `src/AURA.Core/Runtime/CellStore.cs`
- `src/AURA.Core/Runtime/DirectoryCellBackend.cs`
- `src/AURA.Core/Runtime/ICellBackend.cs`
- `src/AURA.Core/Runtime/PluginWatcher.cs`
- `src/AURA.Core/Runtime/ResourceLimits.cs`
- `src/AURA.Core/Runtime/SimulationRuntime.cs`
- `src/AURA.Core/VersionInfo.cs`
- `src/AURA.Core/WebSearchService.cs`
- `src/AURA.Installer/ArtifactAnalysisService.cs`
- `src/AURA.Installer/ArtifactIdentification.cs`
- `src/AURA.Installer/ArtifactType.cs`
- `src/AURA.Installer/DependencyReport.cs`
- `src/AURA.Installer/EnvironmentSelectionResult.cs`
- `src/AURA.Installer/EnvironmentSelectionService.cs`
- `src/AURA.Installer/FileIdentifier.cs`
- `src/AURA.Installer/IDependencyAnalyzer.cs`
- `src/AURA.Installer/IEnvironmentSelector.cs`
- `src/AURA.Installer/IFileIdentifier.cs`
- `src/AURA.Installer/IInstaller.cs`
- `src/AURA.Installer/InstallationResult.cs`
- `src/AURA.Installer/InstallationService.cs`
- `src/AURA.Installer/PythonDependencyAnalyzer.cs`
- `src/AURA.Installer/PythonEnvironmentSelector.cs`
- `src/AURA.Installer/PythonInstaller.cs`
- `src/AURA.Installer/PythonStdlibModules.cs`
- `src/AURA.Memory/MemoryEntry.cs`
- `src/AURA.Memory/MemoryStore.cs`
- `src/AURA.Memory/RequestContext.cs`
- `src/AURA.Memory/SolutionRule.cs`
- `src/AURA.Memory/SolutionStore.cs`
- `src/AURA.Mobile/App.xaml`
- `src/AURA.Mobile/App.xaml.cs`
- `src/AURA.Mobile/Controls/AiConfig.cs`
- `src/AURA.Mobile/Controls/AiConfigView.cs`
- `src/AURA.Mobile/DesignSystem.cs`
- `src/AURA.Mobile/Diagnostics/AgentWorkspace.cs`
- `src/AURA.Mobile/Diagnostics/AiDiagnosticsService.cs`
- `src/AURA.Mobile/Diagnostics/FixProposal.cs`
- `src/AURA.Mobile/Diagnostics/ProjectAccessService.cs`
- `src/AURA.Mobile/Diagnostics/RuntimeConfig.cs`
- `src/AURA.Mobile/Diagnostics/SearchCatalog.cs`
- `src/AURA.Mobile/Diagnostics/WebSearchAnswer.cs`
- `src/AURA.Mobile/MainPage.cs`
- `src/AURA.Mobile/MauiProgram.cs`
- `src/AURA.Mobile/Pages/AgentPage.xaml`
- `src/AURA.Mobile/Pages/AgentPage.xaml.cs`
- `src/AURA.Mobile/Pages/BrowserPage.xaml`
- `src/AURA.Mobile/Pages/BrowserPage.xaml.cs`
- `src/AURA.Mobile/Pages/BrowserSettingsPage.cs`
- `src/AURA.Mobile/Pages/CellsPage.xaml`
- `src/AURA.Mobile/Pages/CellsPage.xaml.cs`
- `src/AURA.Mobile/Pages/ChatPage.xaml`
- `src/AURA.Mobile/Pages/ChatPage.xaml.cs`
- `src/AURA.Mobile/Pages/DiagnosticoPage.xaml`
- `src/AURA.Mobile/Pages/DiagnosticoPage.xaml.cs`
- `src/AURA.Mobile/Pages/EcosystemPage.xaml`
- `src/AURA.Mobile/Pages/EcosystemPage.xaml.cs`
- `src/AURA.Mobile/Pages/ExecutorsPage.xaml`
- `src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs`
- `src/AURA.Mobile/Pages/FixesPage.xaml`
- `src/AURA.Mobile/Pages/FixesPage.xaml.cs`
- `src/AURA.Mobile/Pages/HomePage.xaml`
- `src/AURA.Mobile/Pages/HomePage.xaml.cs`
- `src/AURA.Mobile/Pages/ImageSearchPage.xaml`
- `src/AURA.Mobile/Pages/ImageSearchPage.xaml.cs`
- `src/AURA.Mobile/Pages/LogsPage.xaml`
- `src/AURA.Mobile/Pages/LogsPage.xaml.cs`
- `src/AURA.Mobile/Pages/MemoryPage.xaml`
- `src/AURA.Mobile/Pages/MemoryPage.xaml.cs`
- `src/AURA.Mobile/Pages/ModulesPage.xaml`
- `src/AURA.Mobile/Pages/ModulesPage.xaml.cs`
- `src/AURA.Mobile/Pages/ProgramsPage.xaml`
- `src/AURA.Mobile/Pages/ProgramsPage.xaml.cs`
- `src/AURA.Mobile/Pages/RunPage.xaml`
- `src/AURA.Mobile/Pages/RunPage.xaml.cs`
- `src/AURA.Mobile/Pages/SectionPage.cs`
- `src/AURA.Mobile/Pages/TerminalPage.xaml`
- `src/AURA.Mobile/Pages/TerminalPage.xaml.cs`
- `src/AURA.Mobile/Platforms/Android/AuraAndroidBridgeTest.cs`
- `src/AURA.Mobile/Platforms/Android/AuraLog.cs`
- `src/AURA.Mobile/Platforms/Android/MainActivity.cs`
- `src/AURA.Mobile/Platforms/Android/MainApplication.cs`
- `src/AURA.Mobile/Platforms/Android/StoragePermissionHelper.cs`
- `src/AURA.Mobile/Platforms/Android/VoiceFloatingButton.cs`
- `src/AURA.Mobile/Platforms/Android/VpnHelper.cs`
- `src/AURA.Mobile/Platforms/Android/WebView/AuraDownloadListener.cs`
- `src/AURA.Mobile/Platforms/Android/WebView/AuraLongClickListener.cs`
- `src/AURA.Mobile/Platforms/Android/WebView/AuraTouchListener.cs`
- `src/AURA.Mobile/Platforms/Android/WebView/AuraWebViewHandler.cs`
- `src/AURA.Mobile/ProcessInfo.cs`
- `src/AURA.Mobile/ProcessRegistry.cs`
- `src/AURA.Mobile/RoleToColorConverter.cs`
- `src/AURA.Mobile/Services/AgentPromptStore.cs`
- `src/AURA.Mobile/Services/AndroidCapabilityService.cs`
- `src/AURA.Mobile/Services/AuraCellContext.cs`
- `src/AURA.Mobile/Services/AuraCellContextFactory.cs`
- `src/AURA.Mobile/Services/LocalCommandRecipes.cs`
- `src/AURA.Mobile/Services/LocalPlaybook.cs`
- `src/AURA.Mobile/Services/WebSearchService.cs`
- `src/AURA.Mobile/Speech/AndroidSpeechRecognitionService.cs`
- `src/AURA.Mobile/Speech/AndroidTtsSpeechService.cs`
- `src/AURA.Mobile/Speech/HybridSpeechService.cs`
- `src/AURA.Mobile/Speech/ISpeechRecognitionService.cs`
- `src/AURA.Mobile/Speech/ISpeechService.cs`
- `src/AURA.Mobile/Speech/VoiceAssistantService.cs`
- `src/AURA.Mobile/ViewModels/ModuleRow.cs`
- `src/AURA.Mobile/ViewModels/ProgramsPageViewModel.cs`
- `src/AURA.Modules/Executors/GitExecutor.cs`
- `src/AURA.Modules/Executors/NodeExecutor.cs`
- `src/AURA.Modules/Executors/ProcessExecutorBase.cs`
- `src/AURA.Modules/Executors/PythonExecutor.cs`
- `src/AURA.Modules/Executors/ShellExecutor.cs`
- `src/AURA.Modules/Loja/LockHelper.cs`
- `src/AURA.Modules/Loja/LojaLocalResolver.cs`
- `src/AURA.Modules/Loja/LojaUninstaller.cs`
- `src/AURA.Modules/ModuleCatalog.cs`
- `src/AURA.Modules/ModuleDifficulty.cs`
- `src/AURA.Modules/ModuleInfo.cs`
- `src/AURA.Modules/ModuleManager.cs`
- `src/AURA.Modules/ModuleStatus.cs`
- `src/AURA.Modules/Runtime/BinaryPath.cs`
- `src/AURA.Modules/Runtime/CompatibilityChecker.cs`
- `src/AURA.Modules/Runtime/DependencyAnalyzer.cs`
- `src/AURA.Modules/Runtime/Installer.cs`
- `src/AURA.Modules/Runtime/LanguageDetector.cs`
- `src/AURA.Modules/Runtime/RuntimeCatalog.cs`
- `src/AURA.Modules/Runtime/RuntimeManager.cs`
- `src/AURA.Modules/Runtime/RuntimeProcessExecutor.cs`
- `src/AURA.Modules/Runtime/RuntimeResolver.cs`
- `src/AURA.Modules/Runtime/SyntaxValidator.cs`
- `src/AURA.Network/NetworkManager.cs`
- `src/AURA.Network/NetworkStatus.cs`
- `src/AURA.SystemInfo/SystemAnalyzer.cs`
- `src/AURA.SystemInfo/SystemDiagnosticsResult.cs`

### Últimos commits

- `9592388` feat(local): receitas sem API key — objetivo → aura-sh → execução (2026-08-24)
- `ef071cd` docs(ai): record CI failure context (2026-08-24)
- `b37b3e5` docs(ai): sync README_AI with source tree (2026-08-24)
- `3943170` fix(agent): não reinicia sessão a cada mensagem; prompt Android realista (2026-08-24)
- `01de0d3` docs(ai): record CI failure context (2026-08-23)
<!-- AI-DOCS:END -->
