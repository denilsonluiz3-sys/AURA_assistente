# AURA — Roadmap: reativar funcionalidades não usadas (4 itens)

> **HISTÓRICO** — 2026-08-10  
> Fases 1–3 deste documento **já estão implementadas no `main`**.  
> Não reimplementar. Fonte de verdade atual: [`docs/roadmap-completo.md`](roadmap-completo.md).

Objetivo original: ligar/corrigir as 4 funcionalidades implementadas mas não usadas que
mais agregam valor, sem erros ou conflitos. Ordem anti-conflito: nenhum arquivo
é tocado em fases paralelas; Fases que mexem na mesma classe rodam em sequência.

## Fase 1 — Item 2: Células funcionais no mobile (correção de bug) — **FEITO**

**Problema:** `Cell.RootDirectory` e `CellStore` fixavam `~/AURA/cells*` e ignoravam o
`cellsRoot` configurado.

**Solução aplicada:** `CellRoot` + `JsonIgnore` em `Cell`; `CellStore` usa o root configurado.

**Arquivos:** Cell.cs, CellStore.cs, SimulationRuntime.cs

## Fase 2 — Item 1: Executores Git/Python/Node executáveis — **FEITO**

**Problema:** a aba Executores só mostrava `IsAvailable()`.

**Solução aplicada:** `ExecutorsPage` executa via `ExecuteAsync` e publica evento.

**Arquivos:** ExecutorsPage.xaml, ExecutorsPage.xaml.cs

## Fase 3 — Item 3: EventBus conectado — **FEITO**

**Solução aplicada:** try/catch defensivo; eventos concretos; publicação no runtime e AgentManager.

**Arquivos:** EventBus.cs, SimulationRuntime.cs, AgentManager.cs, MemoryStore.cs

## Fase 4 — Item 4: Configuração com efeito — **parcial / ainda relevante**

**Solução prevista:**
- `FirstRunCompleted`: CLI mostra banner de boas-vindas na 1ª execução e grava.
- `Theme`: mobile aplica `Application.Current.UserAppTheme` de `Settings.Theme`.
- `ModuleFlags`: só expor (comando `config` no CLI + seção no mobile), sem gate
  de features (default = tudo false).

**Arquivos:** Program.cs (CLI), App.xaml.cs/AppShell (mobile)

> Se ainda incompleto, tratar como item residual no `roadmap-completo.md` (P1.3).

## Fase 5 — Integração

Commits pequenos por fase no `main`, push via `scripts/git-push.sh`; CI `build-test` + `build-android-apk`.

## Matriz anti-conflito (histórico)

| Fase | Arquivos tocados |
|---|---|
| 1 | Cell.cs, CellStore.cs, SimulationRuntime.cs |
| 2 | ExecutorsPage.xaml, ExecutorsPage.xaml.cs |
| 3 | EventBus.cs, SimulationRuntime.cs, AgentManager.cs, MemoryStore.cs |
| 4 | Program.cs, App.xaml.cs / AppShell |
