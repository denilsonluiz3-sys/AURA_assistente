# AURA — Roadmap completo (atualizado)

**Data:** 2026-08-10  
**Fonte de verdade:** código em `main` + docs existentes + PRs recentes  
**Método:** evidence-first (código > docs antigos > hipótese)

---

## 1. Estado atual verificado

### Feito e consolidado (F0–F3 + infra)

| Área | Status | Evidência |
|------|--------|-----------|
| Runtime de células (isolamento, recycle, prlimit) | **Feito** | `SimulationRuntime`, F0–F2 no README |
| Persistência `cells.json` + orfãos | **Feito** | F1 |
| `CellRoot` / root customizado (mobile) | **Feito** | `Cell.cs` já tem `CellRoot` + `JsonIgnore` |
| Executores Shell/Git/Python/Node | **Feito** | `ProcessExecutorBase` + testes |
| UI Executores com ExecuteAsync | **Feito** | `ExecutorsPage` executa e publica `ExecutorCompletedEvent` |
| EventBus defensivo + eventos | **Feito** | `EventBus.Publish` try/catch; `AuraEvents.cs` |
| F3 AgentManager + ask/run assistentes | **Feito** | `AgentManager`, workspace, prompts |
| Tool consolidation Step 1 | **Implemented** | `ShellAgentTool` → `IToolExecutor`; `docs/architecture/tool-consolidation-plan.md` |
| Reasoning Gemini / OpenRouter | **Feito** | PR #21 merged |
| TTS híbrido + FAB voz | **Feito** | PR #22 merged |
| CI build+test+smoke | **Ativo** | `build-test.yml` on push main |
| CI APK Android | **Ativo** | `build-android-apk.yml` |

### Em andamento

| Item | Status | Notas |
|------|--------|-------|
| **PR #23 — ToolRegistry (Fase A)** | Aberto | Registro/lookup central de `AgentTool`; sem mudar `ExecuteAsync → string` |

### Gaps reais restantes

| Gap | Severidade | Notas |
|-----|------------|-------|
| `docs/roadmap-4-itens.md` desatualizado | Baixa | Fases 1–3 já no código; tratar como histórico |
| `IAgent` ainda stub | Média | Existe em Core; não é usado pelo AgentManager |
| Loja de módulos (F4) | Alta (produto) | Não implementada — especificação v2 feita e validada contra o código (docs/f4-loja-local-spec-1.md) |
| Daemon + API HTTP (F5) | Média | Não implementada |
| ToolResult interno / MemoryKind expandido | Média | Fase B cognitiva (após ToolRegistry) |

### Docs legados a não seguir cegamente

- `roadmap-4-itens.md` — itens 1–3 **já no código**; não reimplementar.
- `planejamento.md` — útil para histórico; F3 concluído; próximo é F4.
- Análises baseadas em zips antigos — sempre conferir `main` no GitHub.

---

## 2. Princípios de escolha (melhores opções)

1. **Reuse before create** — consolidar o que existe (`IToolExecutor`, `SimulationRuntime`, `EventBus`, `ModuleManager`).
2. **Small reversible changes** — commits pequenos, CI verde antes da próxima fase.
3. **Mobile + CLI compartilham Core** — não criar runtime paralelo no MAUI.
4. **IA é capacidade, não dependência** — tools e células funcionam sem LLM.
5. **Validar no CI** — build local no Termux/proot é instável; Actions é fonte de verdade.

---

## 3. Roadmap prioritizado (ordem de execução)

### P0 — Fechar o que está em voo (agora)

| # | Ação | Critério de pronto |
|---|------|--------------------|
| P0.1 | CI + merge do **PR #23** (ToolRegistry) | `build-and-test` verde; merged em `main` |
| P0.2 | Docs de consolidação alinhados | ✅ `tool-consolidation-plan.md` = Implemented |
| P0.3 | Smoke device: `run_shell` via adapter | Comando real → `exit=0` |
| P0.4 | Tratar `roadmap-4-itens.md` como histórico | Evita retrabalho |

### P1 — Camada cognitiva (após ToolRegistry)

| # | Ação | Por quê |
|---|------|--------|
| P1.1 | ToolResult interno (Fase B) | Classificar sucesso/erro sem quebrar string para o LLM |
| P1.2 | `search_files` (via executor / grep) | RAG local sem filesystem paralelo |
| P1.3 | Expandir `MemoryKind` (ToolCall, ErrorEvent, ProceduralExperience) | Memória episódica + procedural |
| P1.4 | Autocrítica leve antes de write/edit | Reviewer antes de mutação |

### P2 — F4 Loja de módulos (maior próximo marco de produto)

| # | Ação | Reuso |
|---|------|-------|
| P2.1 | Loja local `~/AURA/loja` + manifesto simples | `ModuleManager`, `PluginWatcher` |
| P2.2 | `aura update` / apply / remove | Já esboçado no ModuleManager mobile |
| P2.3 | Releases GitHub como fonte de `.dll` | Docs já preveem |
| P2.4 | Só depois: HTTPS remoto | Não misturar com local |

**Decisão detalhada (F4 — Loja local, resumo validado contra código):**

- Fonte de verdade da spec: `docs/f4-loja-local-spec-1.md` (implementação v2 validada contra `PluginWatcher`, `ModuleManager`, `ModuleCatalog`).
- Reuso obrigatório: ModuleManager (`AURA.Modules`) + PluginWatcher (`AURA.Core`). Não criar framework paralelo nem alterar `SimulationRuntime`.

Principais regras e responsabilidades:
- LojaLocalResolver (nova peça) apenas: validar id no `ModuleCatalog`, copiar `payload/*.dll` para `~/AURA/plugins/` (pasta flat), e escrever `packagesDir/<id>/module.json` gerado a partir do `ModuleCatalog` (schema real). Não decide habilitação nem discovery.
- `ModuleManager.Apply(id)` permanece responsável por habilitação (grava flag em `modules.json`) e continua sem tocar arquivos — instalar é duas fases: (1) instalar bytes no pluginsRoot via LojaLocalResolver, (2) ModuleManager.Apply para habilitar.
- `PluginWatcher` permanece responsável por escanear `~/AURA/plugins/` (flat) e hot-reload via ALC.

Manifesto mínimo (`manifest.json`) na loja local deve conter apenas:

```json
{
  "id": "executores-git",
  "payloadFiles": [
    "ExecutoresGit.dll",
    "ExecutoresGit.deps.json"
  ]
}
```

Regras de segurança e validação recomendadas (implementação):
- Validar `ModuleCatalog.GetById(id)` ANTES de copiar qualquer arquivo; falhar se não existir.
- Rejeitar `payloadFiles` que contenham path traversal ou separadores (aceitar somente nomes simples, ex: regex ^[A-Za-z0-9._-]+$).
- Política de colisão em `~/AURA/plugins/`: por padrão FAIL se arquivo destino já existir e pertencer a outro id; oferecer `--force`/overwrite explicitamente. Registrar propriedade (packagesDir/<id>/installedFiles.json) facilita uninstall/cleanup.
- Cópia atômica: copiar para tmp, validar, mover atomically; escrever `module.json` atomically.
- Locking por id durante instalação (packagesDir/<id>/.install.lock) para evitar concorrência.

Testes mínimos exigidos (unit + integração):
- `InstallFromLoja_IdNotInCatalog_ThrowsBeforeCopyingAnything`
- `InstallFromLoja_CopiesPayloadFilesToPluginsRootFlat`
- `InstallFromLoja_WritesModuleJsonFromCatalogSchema`
- `InstallFromLoja_ThenModuleManagerApply_Succeeds` (integração)
- `InstallFromLoja_ThenPluginWatcherReload_DiscoversDll` (integração)
- `InstallFromLoja_PayloadFileNameContainsPathTraversal_Throws`
- Concorrência/rollback tests: instalação paralela, falha no meio da cópia → rollback

**Nota:** Loja local só instala módulos pré-cadastrados no `ModuleCatalog` — não é loja aberta.

---

### P3 — Diagnóstico + recuperação

| # | Ação |
|---|------|
| P3.1 | Classificação de erro (`AgentErrorKind` quando existir) |
| P3.2 | Recuperação segura (retry com estratégia diferente) |
| P3.3 | Experiências procedurais verificadas → memória |

### P4 — F5 Daemon + API

| # | Ação | Notas |
|---|------|-------|
| P4.1 | Célula dedicada exposando HTTP mínimo | Usa `SimulationRuntime` |
| P4.2 | Termux: `termux-services`; Linux: `systemd --user` | Conforme README |
| P4.3 | Auth simples / localhost-first | Segurança antes de expor rede |

### P5 — Opcional / estudo

| Item | Quando |
|------|--------|
| F6 proot/firejail sob demanda | Só células suspeitas |
| F7 qcow2/KVM | Linux real com `/dev/kvm` |
| `GitAgentTool` / wrappers | Se o LLM precisar de schema dedicado |
| `IAgent` de verdade | Se unificar AgentManager + AgentSession |

---

## 4. Matriz anti-conflito (arquivos)

| Fase | Arquivos típicos | Conflita com |
|------|------------------|--------------|
| P0 ToolRegistry | `ToolRegistry.cs`, `AgentSession`, `AgentPage`, testes | Evitar F4 em paralelo |
| P1 cognitivo | `AURA.AI`, Memory | Depois do merge do PR #23 |
| P2 F4 | `ModuleManager`, CLI `update`, loja | Não tocar SimulationRuntime em paralelo |
| P3 recovery | AI + Memory | Depois de P1 estável |
| P4 F5 | novo projeto API + scripts | Depois de F4 estável |

---

## 5. Gestão e validação

```
Mudança pequena
  ↓
dotnet build AURA.sln (CI)
  ↓
dotnet test (CI)
  ↓
smoke-test.sh (CI)
  ↓
(se Mobile) build-android-apk
  ↓
merge / push main
  ↓
próximo item do roadmap
```

**Regra:** não iniciar F4 enquanto P0.1 (PR #23 merged + CI verde) não estiver feito.

---

## 6. Decisão resumida — melhores opções agora

1. **Não reabrir** CellRoot / ExecutorsPage / EventBus / tool consolidation Step 1 — já feitos.
2. **Fechar PR #23** (ToolRegistry) como próximo passo imediato.
3. **Manter** AgentTool como adapter; não mudar `ExecuteAsync` para `ExecutionResult` ainda.
4. **Próximo marco de produto = F4 loja local**, reusando ModuleManager + PluginWatcher.
5. **Camada cognitiva** (ToolResult, search_files, memória) em PRs pequenos após o registry.

---

## 7. Critério de sucesso deste roadmap

- CI verde no `main` após consolidação e ToolRegistry.
- Docs não contradizem o código.
- Próxima feature (F4) tem desenho de reuso claro, sem arquitetura paralela.
- Mobile e CLI continuam no mesmo Core/Abstractions.
