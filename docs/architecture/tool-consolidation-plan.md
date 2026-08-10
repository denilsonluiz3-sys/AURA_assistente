# Tool Consolidation Plan — AURA

**Status:** Decision document (audit approved)  
**Scope:** Eliminate process-execution duplication between the cognitive layer (`AgentTool` / `ShellAgentTool`) and the operational layer (`IToolExecutor` / `ShellExecutor` / `ProcessExecutorBase`) without breaking existing behaviour.  
**Primary question answered:**

> Which component owns the real responsibility of executing a process, and how should `AgentTool` reuse that implementation without losing cognitive-layer characteristics?

---

## 1. Current State (verified from code)

### Cognitive path (LLM-facing)

```
AgentSession
  ↓ (holds List<AgentTool>)
AgentTool (abstract)
  ├── Definition : AgentToolDefinition
  └── ExecuteAsync(string argumentsJson) → string
        ↓
ShellAgentTool          (process created here)
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
  └── ProcessExecutorBase.RunAsync(...)   (process created here)
```

### Side-by-side comparison (16 points)

| # | Aspect | ShellAgentTool (cognitive) | ShellExecutor + ProcessExecutorBase (operational) | Verdict |
|---|--------|----------------------------|---------------------------------------------------|---------|
| 1 | Process creation | `new Process { StartInfo = psi }` directly | `ProcessExecutorBase.RunAsync` → same | **Duplicated** |
| 2 | Arguments / command | Single string `command` passed to `sh -c "..."` with manual quote escaping | `request.Command` + `request.Arguments`; for shell joins into one string then `sh -c` | Similar intent; executor is cleaner |
| 3 | Working directory | Fixed at construction (`_workspaceRoot`) | Per-request (`request.WorkingDirectory`) | Executor more flexible |
| 4 | Environment variables | Not supported | `request.EnvironmentVariables` applied to `psi.Environment` | **Only executor supports it** |
| 5 | Timeout | Hard-coded 30 s via linked CTS | `request.Timeout` (nullable); tested | Executor more flexible |
| 6 | Cancellation token | Honoured (linked with timeout) | Honoured (linked with timeout) | Equivalent |
| 7 | stdout | Captured via `OutputDataReceived` → StringBuilder | Same pattern | Equivalent |
| 8 | stderr | Captured; prefixed with `"stderr: "` in final string | Captured separately in `ExecutionResult.StandardError` | Executor preserves structure |
| 9 | Exit code | Embedded as text prefix `"exit=N\n..."` | `ExecutionResult.ExitCode` + `Success` | Executor structured |
| 10 | Exception handling | Catch → return `"ERRO: ..."` string; kill on cancel | Catch → `ExecutionResult.Failed(...)` or timeout message; kill on cancel | Both safe; executor typed |
| 11 | Output limits | Truncates at 30 000 chars | No truncation | Cognitive-specific (LLM context) |
| 12 | Security (path) | N/A for shell (command is free-form) | N/A for shell | Path sandbox lives only in `WorkspaceAgentTool` |
| 13 | Path / workspace | `_workspaceRoot` fixed; shell runs inside it | Caller chooses WorkingDirectory | Different models |
| 14 | Logging | None inside tool; `AgentSession` logs tool name | None inside executor | Logging stays at session / caller |
| 15 | Persistence | None; `AgentSession` may append to `MemoryStore` | None | Memory stays at session |
| 16 | Existing tests | `AgentToolsTests` (file tools + path traversal + definitions). **No direct test of ShellAgentTool process behaviour.** | `ExecutorsTests` (availability, stdout, stderr, env, timeout, non-zero exit, working dir) | Operational path better tested for process semantics |

**Key verified facts**

- Both implementations create a `Process`, redirect stdout/stderr, support cancellation and kill on timeout.
- `ShellAgentTool` hard-codes timeout, workspace root, output truncation and returns a single string suitable for the LLM.
- `ProcessExecutorBase` is the richer, more testable process engine and already serves four executors.
- There is **no** shared adapter today. The two paths are parallel.

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
> `AgentTool` (specifically process-oriented tools such as `ShellAgentTool`) must become a **thin cognitive adapter**: parse/validate arguments, optionally enforce policy, call `IToolExecutor.ExecuteAsync`, then adapt `ExecutionResult` into the `string` that `AgentSession` already expects.

File tools (`ListDirTool`, `ReadFileTool`, …) do **not** go through `IToolExecutor`; they stay pure workspace operations. Only tools that launch external processes should reuse the executor layer.

---

## 3. Target Flow

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

### Important constraint (incremental migration)

**Do not** change the signature of `AgentTool.ExecuteAsync` from `Task<string>` to `Task<ExecutionResult>` in the first step.

Reasons (verified):

- `AgentSession.ExecuteToolAsync` and the message protocol expect a string that becomes `AgentMessage.Content` for the `tool` role.
- All existing call sites and the UI event `AgentStep.Result` are strings.
- Changing the abstract signature is a breaking change across the cognitive layer.

Preferred approach: keep `Task<string>` on `AgentTool` and perform the adaptation **inside** the concrete process tools.

---

## 4. Concrete Consolidation Design

### 4.1 ShellAgentTool becomes an adapter

Current behaviour that must be preserved (compatibility):

- Name: `run_shell`
- Required parameter: `command` (string)
- Working directory = workspace root supplied at construction
- Timeout ≈ 30 s (current hard-coded value)
- Output truncated at ~30 000 characters
- Result string shape roughly: `exit=N\n<stdout>\nstderr: <stderr>` (or equivalent readable form)
- On missing shell / empty command / cancel → error string starting with `ERRO`

New internal behaviour:

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
  └── return FormatForLlm(result)   // truncation + exit prefix + stderr label
```

`FormatForLlm` is the only place that keeps cognitive-specific presentation rules.

### 4.2 What stays unchanged

- `IToolExecutor`, `ExecutionRequest`, `ExecutionResult`, `ProcessExecutorBase`
- All existing executors (`ShellExecutor`, `GitExecutor`, `PythonExecutor`, `NodeExecutor`)
- `AgentSession` loop and message protocol
- File tools and `WorkspaceAgentTool`
- Public surface of `AgentTool.ExecuteAsync` → `string`

### 4.3 Project reference decision

`AURA.AI` currently references only `AURA.Core` and `AURA.Memory`.

For Step 1:

- Add project reference **`AURA.Abstractions`** to `AURA.AI` (for `IToolExecutor`, `ExecutionRequest`, `ExecutionResult`).
- Do **not** add `AURA.Modules` to `AURA.AI` (avoids coupling cognitive layer to concrete executors).
- Composition root (`AgentPage` / `MauiProgram`) already has `ShellExecutor` in DI and injects it into `ShellAgentTool`.

### 4.4 Optional follow-ups (not part of the first consolidation)

| Item | When |
|------|------|
| `GitAgentTool` / `PythonAgentTool` wrappers | When the agent needs first-class tools instead of going through `run_shell` |
| Shared `ProcessAgentToolBase` helper | If more than one process tool appears |
| Soft introduction of structured results | Only after string path is stable and tested |
| Central tool registry | Only if manual list construction becomes painful |

---

## 5. Migration Steps (incremental, reversible)

### Step 1 — Adapter only (recommended first PR)

1. Add `AURA.Abstractions` reference to `AURA.AI.csproj`.
2. Change `ShellAgentTool` to accept `IToolExecutor` and delegate process execution.
3. Implement `FormatForLlm(ExecutionResult)` preserving current string shape.
4. Update `AgentPage` to pass `ShellExecutor` (from DI or `new ShellExecutor()`).
5. Add unit tests for the adapter path.

**Risk:** Low. Behaviour is intentionally preserved; only the process engine moves.

### Step 2 — Align timeout / env (optional)

- Allow optional timeout override on `ShellAgentTool`.
- Forward environment variables if the cognitive layer needs them.

### Step 3 — Documentation

- Update `docs/ferramentas.md` to state that process tools go through `IToolExecutor`.
- Keep this file as the architectural decision record.

### Explicit non-goals for the first change

- No change to `AgentTool` abstract signature.
- No new registry.
- No new error type hierarchy for tools.
- No refactor of file tools.
- No large “tool framework” rewrite.

---

## 6. Test Strategy

| Layer | Existing coverage | Required for consolidation |
|-------|-------------------|----------------------------|
| Process engine | `ExecutorsTests` | Keep green |
| File tools + path sandbox | `AgentToolsTests` | Keep green |
| ShellAgentTool behaviour | Weak / absent for process outcomes | **Add** adapter-path tests |
| AgentSession loop | Memory + reasoning tests | Keep green; string contract unchanged |

Minimum new tests for Step 1:

1. `run_shell` with a trivial command returns success-shaped string.
2. Empty command → `ERRO`.
3. Unavailable executor → `ERRO`.
4. Truncation still applies when output is large (optional if limit injectable).

---

## 7. Decision Summary

**Canonical process owner:** `IToolExecutor` + `ProcessExecutorBase`.

**Role of `AgentTool` (process tools):** cognitive adapter — schema, validation, policy, and presentation of `ExecutionResult` as the `string` that the existing agent loop already consumes.

**Role of file tools:** unchanged; they do not need `IToolExecutor`.

**Migration style:** incremental. First change is internal to `ShellAgentTool` + composition root; public contracts stay stable.

**Rejected alternatives**

| Alternative | Why rejected |
|-------------|--------------|
| Make `AgentTool.ExecuteAsync` return `ExecutionResult` immediately | Breaks `AgentSession`, message protocol, UI events |
| Delete `IToolExecutor` and keep only `ShellAgentTool` process code | Throws away the better-tested multi-executor engine |
| Create a third “ToolRuntime” abstraction | Parallel architecture; violates reuse principles |
| Big-bang rewrite of both layers | High risk, unnecessary for the verified gap |

---

## 8. Completion Criteria

This plan is ready for implementation when:

1. The responsibility split above is accepted.
2. Step 1 (adapter) is the only mandatory first change.
3. Existing `ExecutorsTests` and `AgentToolsTests` remain the safety net.
4. No new process-creation code is introduced outside `ProcessExecutorBase`.

Once Step 1 is merged and green, process-execution duplication is eliminated while cognitive characteristics of `AgentTool` are preserved.
