# Tool Consolidation Plan — AURA

**Status:** Implemented (Step 1)  
**Data de implementação:** 2026-08  
**Scope:** Eliminate process-execution duplication between the cognitive layer (`AgentTool` / `ShellAgentTool`) and the operational layer (`IToolExecutor` / `ShellExecutor` / `ProcessExecutorBase`) without breaking existing behaviour.  
**Primary question answered:**

> Which component owns the real responsibility of executing a process, and how should `AgentTool` reuse that implementation without losing cognitive-layer characteristics?

---

## 1. Current State (verified from code on `main`)

### Cognitive path (LLM-facing) — **já consolidado**

```
AgentSession
  ↓ (holds List<AgentTool>)
AgentTool (abstract)
  ├── Definition : AgentToolDefinition
  └── ExecuteAsync(string argumentsJson) → string
        ↓
ShellAgentTool          ← adapter (não cria Process)
  ├── IToolExecutor (injetado)
  ├── ExecutionRequest
  └── FormatForLlm(ExecutionResult) → string
ListDirTool / ReadFileTool / WriteFileTool / EditFileTool
  └── WorkspaceAgentTool (path sandbox)
```

### Operational path (runtime/modules/CLI)

```
IToolExecutor
  ├── Name
  ├── IsAvailable()
  └── ExecuteAsync(ExecutionRequest) → ExecutionResult
        ↓
ShellExecutor / GitExecutor / PythonExecutor / NodeExecutor
  └── ProcessExecutorBase.RunAsync(...)   ← único dono da criação de processo
```

### Side-by-side comparison (estado **antes** da consolidação — histórico)

| # | Aspect | ShellAgentTool (antes) | ShellExecutor + ProcessExecutorBase | Verdict (antes) |
|---|--------|------------------------|-------------------------------------|-----------------|
| 1 | Process creation | `new Process` direto | `ProcessExecutorBase.RunAsync` | **Duplicated** |
| 2 | Arguments / command | string `command` → `sh -c` | `request.Command` + Arguments | Similar |
| 3 | Working directory | Fixo (`_workspaceRoot`) | Per-request | Executor mais flexível |
| 4 | Environment variables | Não suportado | `request.EnvironmentVariables` | Só executor |
| 5 | Timeout | Hard-coded 30 s | `request.Timeout` | Executor mais flexível |
| 6 | Cancellation token | Honourado | Honourado | Equivalente |
| 7–9 | stdout / stderr / exit | Texto formatado | Estruturado em `ExecutionResult` | Executor tipado |
| 10 | Exception handling | String `ERRO:` | `ExecutionResult.Failed` | Ambos seguros |
| 11 | Output limits | Truncate 30k | Sem truncamento | Específico cognitivo |
| 12–15 | Security / path / log / memory | Camada cognitiva | Camada operacional | Separação correta |
| 16 | Testes | Fracos no path de processo | `ExecutorsTests` cobrem processo | — |

**Estado atual (após Step 1):**

- `ShellAgentTool` **não** cria `Process`. Delega a `IToolExecutor`.
- `ProcessExecutorBase` é o **único** lugar que cria processo.
- Testes de adapter existem: `ShellAgentTool_RunsCommand_*`, empty command, unavailable executor, `FormatForLlm`.
- `AgentPage` injeta `ShellExecutor` no construtor de `ShellAgentTool`.
- `AURA.AI` referencia `AURA.Abstractions` (não `AURA.Modules`).

---

## 2. Canonical Contract — Responsibility Split

| Responsibility | Owner | Rationale |
|----------------|-------|-----------|
| Agent decision (which tool, when) | `AgentSession` + LLM | Cognitive loop |
| Tool schema / discovery for the model | `AgentTool.Definition` | Already correct |
| Argument parsing & validation (JSON → typed) | Concrete `AgentTool` | Cognitive; knows the schema it advertised |
| Authorization / path sandbox | `WorkspaceAgentTool` (file tools) or future policy on process tools | Must stay above the process layer |
| **Real process execution** | **`IToolExecutor` / `ProcessExecutorBase`** | Single owner of process lifecycle, env, timeout, structured result |
| Structured result | `ExecutionResult` | Success, ExitCode, stdout, stderr, Duration |
| Presentation to the LLM (string shaping, truncation, exit-code prefix) | `AgentTool` (adapter) | Cognitive concern; LLM needs text, not a typed object |
| Error classification for UI / recovery | Prefer existing `AgentErrorKind` where applicable; tool-level errors remain strings or a thin wrapper for now | Do not invent a second error system yet |
| Logging of tool steps | `AgentSession` (already emits `AgentStep`) | Keep |
| Memory of turns | `AgentSession` + `MemoryStore` | Keep |

**Answer to the core question**

> The component that owns the real responsibility of executing a process is **`IToolExecutor` (via `ProcessExecutorBase`)**.  
> `AgentTool` (specifically process-oriented tools such as `ShellAgentTool`) is a **thin cognitive adapter**: parse/validate arguments, optionally enforce policy, call `IToolExecutor.ExecuteAsync`, then adapt `ExecutionResult` into the `string` that `AgentSession` already expects.

File tools (`ListDirTool`, `ReadFileTool`, …) do **not** go through `IToolExecutor`; they stay pure workspace operations. Only tools that launch external processes should reuse the executor layer.

---

## 3. Target Flow (já em produção no `main`)

```
LLM
 ↓
AgentSession          (loop, memory, AgentStep events)
 ↓
AgentTool             (schema, parse JSON, policy)
 ↓
validation / authorization   (WorkspaceAgentTool or tool-specific checks)
 ↓
IToolExecutor         (ShellExecutor, GitExecutor, …)
 ↓
ProcessExecutorBase   (single process engine)
 ↓
ExecutionResult
 ↓
Adapter inside AgentTool   (string for LLM: exit code, truncated stdout/stderr)
 ↓
AgentSession
 ↓
LLM
```

### Constraint mantida (migração incremental)

**Não** mudar a assinatura de `AgentTool.ExecuteAsync` de `Task<string>` para `Task<ExecutionResult>` no Step 1.

Razões (verificadas):

- `AgentSession.ExecuteToolAsync` e o protocolo de mensagens esperam string (`AgentMessage.Content` role `tool`).
- Call sites e UI (`AgentStep.Result`) são strings.
- Mudar a assinatura abstrata é breaking change na camada cognitiva.

A adaptação continua **dentro** das tools de processo concretas (`FormatForLlm`).

---

## 4. Concrete Design (implementado)

### 4.1 ShellAgentTool como adapter

Comportamento preservado:

- Name: `run_shell`
- Required: `command` (string)
- Working directory = workspace root no construtor
- Timeout ≈ 30 s
- Truncamento ~30 000 caracteres
- Formato: `exit=N\n<stdout>\nstderr: <stderr>`
- Erros → string começando com `ERRO`

Implementação atual:

```text
ShellAgentTool
  ├── holds IToolExecutor (ShellExecutor) injected from composition root
  ├── holds workspaceRoot (for WorkingDirectory)
  ├── Parse command from argumentsJson
  ├── Build ExecutionRequest {
  │     Command = command,
  │     WorkingDirectory = workspaceRoot,
  │     Timeout = 30s
  │   }
  ├── result = await executor.ExecuteAsync(request, ct)
  └── return FormatForLlm(result)
```

### 4.2 O que permanece inalterado

- `IToolExecutor`, `ExecutionRequest`, `ExecutionResult`, `ProcessExecutorBase`
- Executores existentes
- Loop de `AgentSession` e protocolo de mensagens
- File tools e `WorkspaceAgentTool`
- Superfície pública `AgentTool.ExecuteAsync` → `string`

### 4.3 Referências de projeto

- `AURA.AI` referencia `AURA.Abstractions` (para contratos).
- **Não** referencia `AURA.Modules` (evita acoplamento cognitivo → executores concretos).
- Composition root (`AgentPage`) injeta `ShellExecutor`.

### 4.4 Follow-ups opcionais (ainda não feitos)

| Item | Quando |
|------|--------|
| `GitAgentTool` / `PythonAgentTool` | Quando o LLM precisar de schema dedicado |
| `ProcessAgentToolBase` helper | Se surgir mais de uma process tool |
| `ToolResult` interno estruturado | Fase B (após string path estável) |
| `ToolRegistry` central | PR #23 (Fase A) — em andamento |

---

## 5. Migration Steps

### Step 1 — Adapter only — **DONE**

1. ✅ Referência `AURA.Abstractions` em `AURA.AI`
2. ✅ `ShellAgentTool` aceita `IToolExecutor` e delega
3. ✅ `FormatForLlm(ExecutionResult)`
4. ✅ `AgentPage` passa `ShellExecutor`
5. ✅ Testes de adapter em `AgentToolsTests`

### Step 2 — Align timeout / env (opcional, pendente)

- Timeout configurável no schema
- Forward de environment variables se a camada cognitiva precisar

### Step 3 — Documentation — **em andamento**

- ✅ `docs/ferramentas.md` já descreve o adapter
- ✅ Este arquivo atualizado para Status: Implemented

---

## 6. Test Strategy (estado atual)

| Layer | Coverage |
|-------|----------|
| Process engine | `ExecutorsTests` |
| File tools + path sandbox | `AgentToolsTests` |
| ShellAgentTool adapter | `RunsCommand`, empty, unavailable, `FormatForLlm` |
| AgentSession loop | Memory + reasoning tests |

---

## 7. Decision Summary

**Canonical process owner:** `IToolExecutor` + `ProcessExecutorBase`.

**Role of `AgentTool` (process tools):** cognitive adapter — schema, validation, policy, and presentation of `ExecutionResult` as the `string` that the existing agent loop consumes.

**Role of file tools:** unchanged; they do not need `IToolExecutor`.

**Migration style:** incremental. Step 1 complete; public contracts stable.

**Rejected alternatives** (ainda válidos)

| Alternative | Why rejected |
|-------------|--------------|
| `ExecuteAsync` → `ExecutionResult` imediatamente | Quebra `AgentSession`, protocolo, UI |
| Deletar `IToolExecutor` e manter só path cognitivo | Joga fora o engine multi-executor testado |
| Criar terceiro “ToolRuntime” | Arquitetura paralela; viola reuse |
| Big-bang rewrite | Risco alto, desnecessário |

---

## 8. Completion Criteria — Step 1

| Critério | Status |
|----------|--------|
| Responsibility split aceito e documentado | ✅ |
| Step 1 (adapter) no `main` | ✅ |
| `ExecutorsTests` + `AgentToolsTests` verdes | ✅ (código + testes presentes) |
| Nenhum `Process` criado fora de `ProcessExecutorBase` para tools de processo | ✅ |

Process-execution duplication eliminada. Características cognitivas de `AgentTool` preservadas.
