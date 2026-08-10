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
| Tool consolidation Step 1 | **Feito** | `ShellAgentTool` → `IToolExecutor`; docs em `architecture/` |
| Reasoning Gemini / OpenRouter | **Feito** | PR #21 merged |
| TTS híbrido + FAB voz | **Feito** | PR #22 merged |
| CI build+test+smoke | **Ativo** | `build-test.yml` on push main |
| CI APK Android | **Ativo** | `build-android-apk.yml` |

### Parcialmente feito / gaps reais

| Gap | Severidade | Notas |
|-----|------------|-------|
| `docs/roadmap-4-itens.md` desatualizado | Baixa | Fases 1–3 já implementadas no código |
| `docs/ferramentas.md` não menciona adapter | Baixa | Atualizar após consolidação |
| `IAgent` ainda stub | Média | Existe em Core; não é usado pelo AgentManager |
| Loja de módulos (F4) | Alta (produto) | Não implementada |
| Daemon + API HTTP (F5) | Média | Não implementada |
| Registry leve de AgentTools | Baixa | Lista manual em `AgentPage` basta por enquanto |
| CI ainda não validou commits de consolidação neste ambiente | Média | Depende do Actions no push |

### Docs legados a não seguir cegamente

- `roadmap-4-itens.md` — itens 1–3 **já no código**; não reimplementar.
- `planejamento.md` — útil para histórico; F3 marcado concluído; próximo é F4.

---

## 2. Princípios de escolha (melhores opções)

1. **Reuse before create** — consolidar o que existe (`IToolExecutor`, `SimulationRuntime`, `EventBus`, `ModuleManager`).
2. **Small reversible changes** — commits pequenos, CI verde antes da próxima fase.
3. **Mobile + CLI compartilham Core** — não criar runtime paralelo no MAUI.
4. **IA é capacidade, não dependência** — tools e células funcionam sem LLM.
5. **Validar no CI** — build local no Termux/proot é instável; Actions é fonte de verdade.

---

## 3. Roadmap prioritizado (ordem de execução)

### P0 — Estabilizar o que acabou de entrar (agora)

| # | Ação | Critério de pronto |
|---|------|--------------------|
| P0.1 | CI `build-and-test` verde no commit do adapter | `dotnet test` + smoke OK |
| P0.2 | CI APK se paths Mobile mudaram | artifact APK gerado |
| P0.3 | Atualizar `docs/ferramentas.md` (process tools → IToolExecutor) | Doc alinhado ao código |
| P0.4 | Marcar `roadmap-4-itens.md` como histórico / feito | Evita retrabalho |

### P1 — Fechar valor mobile/agente já exposto

| # | Ação | Por quê |
|---|------|--------|
| P1.1 | Smoke manual: AgentPage `run_shell` via `ShellExecutor` | Confirma consolidação no dispositivo |
| P1.2 | Opcional: `GitAgentTool` / wrappers só se o LLM precisar de schema dedicado | Evitar tools extras sem demanda |
| P1.3 | Config com efeito (Theme / FirstRun) se ainda incompleto | `roadmap-4-itens` Fase 4 |

### P2 — F4 Loja de módulos (maior próximo marco de produto)

| # | Ação | Reuso |
|---|------|-------|
| P2.1 | Loja local `~/AURA/loja` + manifesto simples | `ModuleManager`, `PluginWatcher` |
| P2.2 | `aura update` / apply / remove | Já esboçado no ModuleManager mobile |
| P2.3 | Releases GitHub como fonte de `.dll` | Docs já preveem |
| P2.4 | Só depois: HTTPS remoto | Não misturar com local |

**Não fazer em F4:** novo framework de plugins; segundo ModuleManager.

### P3 — F5 Daemon + API

| # | Ação | Notas |
|---|------|-------|
| P3.1 | Célula dedicada expondo HTTP mínimo | Usa `SimulationRuntime`, não processo especial |
| P3.2 | Termux: `termux-services`; Linux: `systemd --user` | Conforme README |
| P3.3 | Auth simples / localhost-first | Segurança antes de expor rede |

### P4 — Opcional / estudo

| Item | Quando |
|------|--------|
| F6 proot/firejail sob demanda | Só células suspeitas |
| F7 qcow2/KVM | Linux real com `/dev/kvm` |
| Registry central de AgentTools | Se a lista em AgentPage ficar difícil de manter |
| `IAgent` de verdade | Se unificar AgentManager + AgentSession |

---

## 4. Matriz anti-conflito (arquivos)

| Fase | Arquivos típicos | Conflita com |
|------|------------------|--------------|
| P0 docs | `docs/*` | Nada de runtime |
| P1 mobile UX | `AgentPage`, config mobile | Evitar mexer em Core |
| P2 F4 | `ModuleManager`, CLI `update`, loja | Não tocar SimulationRuntime em paralelo |
| P3 F5 | novo projeto API + scripts serviço | Depois de F4 estável |
| Tool wrappers | `AURA.AI/AgentTools` | Depois de CI verde do adapter |

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

**Regra:** não iniciar F4 enquanto P0.1 (CI do adapter) não estiver verde.

---

## 6. Decisão resumida — melhores opções agora

1. **Não reabrir** CellRoot / ExecutorsPage / EventBus — já feitos.
2. **Validar CI** dos commits de consolidação de tools.
3. **Alinhar docs** (ferramentas + roadmap-4-itens histórico).
4. **Próximo marco de produto = F4 loja local**, reusando ModuleManager + PluginWatcher.
5. **Manter** AgentTool como adapter; não mudar `ExecuteAsync` para `ExecutionResult` ainda.
6. **Skills especializadas** (tool-calling / diagnostics / recovery) só depois do P0 estável.

---

## 7. Critério de sucesso deste roadmap

- CI verde no `main` após consolidação.
- Docs não contradizem o código.
- Próxima feature (F4) tem desenho de reuso claro, sem arquitetura paralela.
- Mobile e CLI continuam no mesmo Core/Abstractions.
