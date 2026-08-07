# AURA — Roadmap: reativar funcionalidades não usadas (4 itens)

Objetivo: ligar/corrigir as 4 funcionalidades implementadas mas não usadas que
mais agregam valor, sem erros ou conflitos. Ordem anti-conflito: nenhum arquivo
é tocado em fases paralelas; Fases que mexem na mesma classe rodam em sequência.

## Fase 1 — Item 2: Células funcionais no mobile (correção de bug)

**Problema:** `Cell.RootDirectory` (src/AURA.Core/Runtime/Cell.cs) e `CellStore`
(src/AURA.Core/Runtime/CellStore.cs) fixam `~/AURA/cells*` e ignoram o
`cellsRoot` configurado. Afeta `DirectoryCellBackend` (cria/deleta na pasta
errada) e `AppendLog`/`ReadCellLog` (SimulationRuntime.cs). No mobile o root é
`AppDataDirectory/cells`, mas tudo grava em `~/AURA/cells` → fora da sandbox.

**Solução:** adicionar `[JsonIgnore] public string CellRoot` em `Cell`, setado
pelo runtime em `CreateCell` e `LoadFromStoreAsync` (`Path.Combine(_cellsRoot, id)`);
`RootDirectory => CellRoot ?? <default atual>`. `CellStore` passa a usar
`Path.Combine(_cellsRoot, "cells.json")`, preservando `~/AURA/cells.json` quando
o root for o default (sem regressão no Termux).

**Arquivos:** Cell.cs, CellStore.cs, SimulationRuntime.cs
**Validação:** build `AURA.sln` + smoke-test; mobile `-t:Compile`.

## Fase 2 — Item 1: Executores Git/Python/Node executáveis

**Problema:** a aba Executores (mobile) só mostra `IsAvailable()`; `ExecuteAsync`
só roda nos testes. Os 4 executores já implementam `ExecuteAsync` via
`ProcessExecutorBase`.

**Solução:** `ExecutorsPage` ganha seletor de executor + campos de comando/
argumentos + botão Executar → `ExecuteAsync(ExecutionRequest)` e exibe
stdout/stderr/exit code. Sem binário no Android → tratar "Não disponível"
graciosamente.

**Arquivos:** ExecutorsPage.xaml, ExecutorsPage.xaml.cs
**Validação:** reusar padrão de tests/AURA.Tests/ExecutorsTests.cs; mobile `-t:Compile`.

## Fase 3 — Item 3: EventBus conectado

**Solução:**
1. try/catch defensivo em `EventBus.Publish` (handler com exceção não derruba o publisher).
2. Eventos concretos em AURA.Core.Events: `CellStateChanged`, `AssistantResponded`,
   `ExecutorCompleted` (todos `IEvent`).
3. `SimulationRuntime` ganha `EventBus` opcional e publica em
   create/start/stop/pause/resume/delete; `AgentManager.AskAsync` publica;
   CLI assina e loga (via `bootstrap.Events`); `MemoryStore` assina para gravar
   `MemoryKind.CellEvent` (reativa código morto `MemoryEntry.CellStateChange`).

**Arquivos:** EventBus.cs, SimulationRuntime.cs (após Fase 1), AgentManager.cs,
MemoryStore.cs
**Validação:** build + smoke.

## Fase 4 — Item 4: Configuração com efeito

**Solução (sem regressão, defaults preservados):**
- `FirstRunCompleted`: CLI mostra banner de boas-vindas na 1ª execução e grava.
- `Theme`: mobile aplica `Application.Current.UserAppTheme` de `Settings.Theme`.
- `ModuleFlags`: só expor (comando `config` no CLI + seção no mobile), sem gate
  de features (default = tudo false; gate desabilitaria tudo por padrão).

**Arquivos:** Program.cs (CLI), App.xaml.cs/AppShell (mobile)
**Validação:** build + smoke.

## Fase 5 — Integração

Commits pequenos por fase no `main`, push via `scripts/git-push.sh`; quando o
Actions voltar, disparar `build-test` + `build-android-apk`.

## Matriz anti-conflito

| Fase | Arquivos tocados |
|---|---|
| 1 | Cell.cs, CellStore.cs, SimulationRuntime.cs |
| 2 | ExecutorsPage.xaml, ExecutorsPage.xaml.cs |
| 3 | EventBus.cs, SimulationRuntime.cs, AgentManager.cs, MemoryStore.cs |
| 4 | Program.cs, App.xaml.cs / AppShell |
