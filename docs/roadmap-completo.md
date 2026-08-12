# Roadmap Completo — estado atualizado 2026-08-12

## Status por fase (Core / CLI)

| Fase | Descrição | Status |
|------|-----------|--------|
| **F0** | Migração net48→net10.0; runtime de células (processo isolado + reciclagem em crash) | ✅ FEITO |
| **F1** | `cells.json` persistência + restauração, adoção de órfãos, hot-reload de plugins (ALC coletável + FSW) | ✅ FEITO |
| **F2** | `prlimit` por célula (`--mem`, `--cpu`, `--files`, `--procs`) — sem root, funciona no Termux | ✅ FEITO |
| **F3** | Célula "assistente": `AgentManager` (aichat / termux-ai / opencode), `aura ask`, `aura run <assistente> --cell <id>`, workspace auto-melhoria | ✅ FEITO |
| **F4 — Loja local** | `LojaLocalResolver`, `LojaUninstaller`, `LockHelper` (timeout + backoff), concurrency tests; `ModuleManager.Remove` integrado | ✅ FEITO |
| **F5 — Uninstall/cleanup** | `LojaUninstaller` finalizado, integrado ao `ModuleManager.Remove`, testes atualizados | ✅ FEITO |
| **F6 — Concorrência/Resiliência** | `LockHelper` com timeout/retry; `LojaLocalResolver` com lock 5s; `InstallerConcurrencyTests` | ✅ FEITO |
| **F7 — CLI loja** | CLI para install/uninstall/dry-run (`aura update`, `aura install`, `aura remove`) | 🔲 PLANEJADO |
| **F8 — Smoke E2E** | `smoke-test.sh` robusto com `--wait`; redução de falsos negativos no CI | 🔄 EM PROGRESSO |
| **F9 — Daemon + API HTTP** | Termux: `termux-services` (runit); Linux: `systemd --user`; API via célula dedicada | 🔲 PLANEJADO |
| **F10 — Isolamento forte** | `proot`/firejail sob demanda para células suspeitas (`.jar` da internet) | 🔲 OPCIONAL |
| **F11 — KVM/qcow2** | Backend em Linux real (requer root/`/dev/kvm`); impossível no celular | 🔲 ESTUDO |

## UI Holográfica (eixo mobile — MAUI)

Conceito de referência: vídeos Gemini (lua/sol + anéis + aurora + menu inferior Network / Sensor / Ethereum / System / Device).

| Fase | Descrição | Status |
|------|-----------|--------|
| **UI-Holo F0** | Análise + escolha de abordagem (MAUI puro sem SkiaSharp nesta fase) | ✅ FEITO 2026-08-11 |
| **UI-Holo F1** | Dashboard HomePage: orb central + bottom bar mapeada a capacidades reais | ✅ FEITO 2026-08-11 (main e20e726, build-android-apk #70 + build-and-test #141 verdes) |
| **UI-Holo F2** | Animações leves (pulse/glow no CoreOrb) + feedback nos 5 botões da bottom bar | ✅ FEITO 2026-08-11 (main 44c221d, PR #37, build-and-test #142 verde) |
| **UI-Holo F3** | SkiaSharp/SKGLView ou WebView para efeitos holográficos avançados | 🔲 PLANEJADO — só após F1/F2 validados no dispositivo real |
| **UI-Holo F4** | Shell + TabBar customizado substituindo TabbedPage | 🔲 FUTURO/OPCIONAL |

### Mapeamento visual → AURA (F1)

| Botão (vídeo) | Capacidade AURA | Ação na UI |
|---------------|-----------------|------------|
| Network | `NetworkManager` | Refresh status de rede |
| Sensor | `SystemAnalyzer` | Refresh diagnóstico |
| Ethereum | (reservado) | Placeholder / futuro módulo |
| System | Sistema / Home | Já na própria página |
| Device | Células / device | Navega para seção Apps/Células |

### Decisão de arquitetura UI (F0)

- **Escolhido F1/F2:** MAUI XAML + C# puro (`ScaleTo`/`FadeTo`) — zero nova dependência.
- **Rejeitado por agora:** SkiaSharp / DrawnUI / WebView Three.js — entra só em UI-Holo F3 se F2 validar no dispositivo.
- Implementação: `src/AURA.Mobile/Pages/HomePage.*`.

## Progresso recente (2026-08-11 → 2026-08-12)

- **2026-08-11:** UI-Holo F1 mergeada em main (PR #36, e20e726). CI mobile verde.
- **2026-08-11:** UI-Holo F2 mergeada em main (PR #37, 44c221d). build-and-test #142 verde.
- **2026-08-11:** `fix(mobile)` — falha de TTS nunca interrompe chat nem Agent Loop (a1270f1).
- **2026-08-11:** `fix(modules)` — usa pacote embarcado quando download remoto falha (8742e0d).
- **2026-08-11:** `HybridSpeechService` adicionado — TTS nativo Android + Kokoro ONNX como fallback.
- **2026-08-11:** `.well-known` (Brave Creators verification) adicionado em main (1d4e6a6).
- **2026-08-12:** PR #33 (`fix/run-wait-go-smoke`) fechado — conteúdo já integrado em main via rebase.
- **2026-08-12:** main sincronizado localmente. Build: 0 erros, 158 warnings (nullability + xUnit).

## PRs e Issues

| # | Tipo | Status | Descrição |
|---|------|--------|-----------|
| [#33](https://github.com/denilsonluiz3-sys/AURA_assistente/pull/33) | PR | ✅ Fechado | fix(go): run --wait + smoke test Go — conteúdo integrado em main |
| [#8](https://github.com/denilsonluiz3-sys/AURA_assistente/pull/8) | PR | 🔲 Aberto | Feat/project access — aguarda revisão/decisão |
| [#28](https://github.com/denilsonluiz3-sys/AURA_assistente/issues/28) | Issue | 🔲 Aberta | P4.1 — IAgent concretos (MemoryAgent, AutomationAgent, AIAgent wrapper) |
| [#27](https://github.com/denilsonluiz3-sys/AURA_assistente/issues/27) | Issue | 🔲 Aberta | P4.1 — IAgent concretos (duplicata de #28 — fechar) |
| [#32](https://github.com/denilsonluiz3-sys/AURA_assistente/issues/32) | Issue | 🔲 Aberta | (sem título — investigar e fechar ou detalhar) |

## Próximos passos (ordem de prioridade)

1. **[mobile]** Validar UI-Holo F2 (pulse + feedback) no APK de debug em dispositivo real.
2. **[issue #28]** Implementar `IAgent` concretos mínimos (`MemoryAgent`, `AutomationAgent`, `AIAgent` wrapper) — fecha F3 completamente.
3. **[F7]** CLI de loja: `aura install <módulo>`, `aura remove <módulo>`, `aura update` (dry-run).
4. **[F8]** Finalizar smoke test E2E — cobrir cenários Go e módulos no CI.
5. **[PR #8]** Revisar e decidir sobre `feat/project-access` (aceitar, descartar ou adaptar).
6. **[issues #27/#32]** Triagem: fechar #27 como duplicata; detalhar ou fechar #32.
7. **[UI-Holo F3]** Avaliar SkiaSharp só após F2 estável no dispositivo.
8. **[F9]** Daemon + API HTTP (pós F7/F8 estáveis).
