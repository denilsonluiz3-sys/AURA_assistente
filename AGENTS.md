# AGENTS.md — Instruções para Agentes de IA

## Identificação

- **Projeto:** AURA_assistente — assistente pessoal multiplataforma (.NET MAUI)
- **SDK:** .NET 10.0.110 (definido em `global.json`)
- **Linguagem:** C# + XAML
- **Testes:** xunit (`tests/AURA.Tests/`)
- **CI:** GitHub Actions (build Android APK, CodeQL, diagnóstico automático)

## Comandos essenciais

```bash
# Restaurar dependências
dotnet restore src/AURA.Mobile/AURA.Mobile.csproj

# Build Android (Release)
dotnet build src/AURA.Mobile/AURA.Mobile.csproj -c Release --no-restore

# Build Agents (verificação rápida de compilação)
dotnet build src/AURA.Agents/AURA.Agents.csproj --no-restore

# Testes
dotnet test tests/AURA.Tests/AURA.Tests.csproj

# Workload MAUI Android
dotnet workload install maui-android

# Smoke test (verifica artefatos de build)
bash scripts/smoke-test.sh

# Diagnóstico local (PRoot)
bash scripts/ai/proot/aura-repair.sh --diagnose
```

## Arquitetura — Camadas

```text
src/
├── AURA.Abstractions/   Contratos (sem dependência de Android/MAUI)
├── AURA.Core/           Fundamentos, logging, launchers, runtime
├── AURA.Memory/         Memória persistente (MemoryStore, SolutionStore)
├── AURA.Agents/         Orquestração, agentes, PolicyGuard, Cell Programs
├── AURA.AI/             Integração com provedores LLM (OpenRouter etc.)
├── AURA.Modules/        Catálogo de módulos, executores, runtime
├── AURA.Mobile/         App MAUI, UI, adapters Android
├── AURA.Windows/        Componentes Windows
├── AURA.CLI/            Interface de linha de comando
├── AURA.Installer/      Instalador de runtimes (Python etc.)
├── AURA.Network/        Gerenciamento de rede
└── AURA.SystemInfo/     Análise do sistema
```

## Regras de dependência (OBRIGATÓRIO)

1. `AURA.Abstractions` **NÃO** pode depender de Android, MAUI ou implementações concretas
2. `AURA.Core` **NÃO** pode depender da UI móvel
3. `AURA.Memory` **NÃO** pode depender da UI
4. Serviços Android **NÃO** devem ser expostos por contratos em Abstractions
5. A integração Android ocorre por adapter/contexto em `AURA.Mobile`
6. Antes de adicionar referência entre projetos, verifique os `.csproj` reais

## Fluxo crítico

```text
Usuário → AuraOrchestrator → IntentResolver → PolicyGuard → ToolResolver → Runner → Resultado
```

- **PolicyGuard:** toda execução com capacidades deve passar pela política antes de executar
- Capacidade desconhecida = **bloqueada por padrão**
- UI **NÃO** pode contornar o PolicyGuard
- LLM é componente de inferência; **NÃO** é requisito obrigatório para operações determinísticas

## Cell Programs

- Cada programa declara `RequiredCapabilities`
- Fluxo: `ProgramsPage → PolicyGuard → CellProgramRunner → IAuraCellContext → Adapter Android`
- `IAuraCellContext` em Abstractions **NÃO** expõe `IAndroidCapabilityService`

## UI Mobile (seções)

```text
Sistema:    Início · Ecossistema · Diagnóstico · Correções
Assistente: Chat · Agente · Memória · Navegador
Ferramentas: Terminal · Executores · Módulos · Logs
Apps:       Programas · Células · Rodar programa
```

Arquivo-chave: `src/AURA.Mobile/MainPage.cs`

## Antes de alterar código (checklist)

1. Ler `README_AI.md` se existir
2. Listar projetos em `src/*` e `tests/*`
3. Ler `.csproj` relevantes
4. Localizar interfaces e implementações existentes
5. Verificar `MauiProgram.cs` (DI) e `AuraOrchestrator`

## Proibições

- **NÃO** duplicar interfaces existentes
- **NÃO** criar dependências Android em abstrações
- **NÃO** mover código entre camadas sem necessidade comprovada
- **NÃO** contornar PolicyGuard
- **NÃO** adicionar scripts temporários ao produto
- **NÃO** colocar segredos ou API keys no código
- **NÃO** criar projeto artificial `AURA.Kernel`

## CI/CD

| Workflow | Função |
|----------|--------|
| `build-android-apk.yml` | Build do APK Android (PR e push em main) |
| `ai-failure-diagnostics.yml` | Diagnóstico automático de falhas CI |
| `codeql.yml` | Análise de segurança |
| `sync-main.yml` | Sincronização |
| `update-ai-docs.yml` | Atualiza snapshot em README_AI.md |

Falhas CI são classificadas como: C#/compilação, XAML, MAUI workload, Android SDK, NuGet, GitHub Actions, CodeQL.

Relatório mais recente: `docs/ai/CI_FAILURE_LATEST.md`

## Contexto: isolamento do Agente

O Agente atualmente está **isolado** do resto do app. O app já tem Terminal, Memória, Navegador, Executores, Programas e Células, mas o Agente não os controla como tools.

Direção correta: cada capacidade do app vira uma **tool invocável** pelo agente:

| Tool | Serviço real |
|------|-------------|
| `run_terminal` | Terminal |
| `memory_search` / `memory_save` | Memória |
| `open_browser` | Navegador |
| `run_executor` | Executores |
| `run_program` | Programas/Células |

Prioridade: Terminal → Memória → Logs/Diagnóstico → Navegador → Programas

## Fonte de verdade

O código-fonte **sempre** tem precedência sobre esta documentação. Se houver divergência, verificar o código.

Leitura recomendada: `AURA.sln` → `.csproj` → `AURA.Abstractions` → `AURA.Core` → `AURA.Memory` → `AURA.Agents` → `AURA.AI` → `MauiProgram.cs` → `MainPage.cs` → testes → `.github/workflows/*`
