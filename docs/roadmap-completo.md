# Roadmap Completo (estado atualizado)

Status geral
- F4 (LojaLocalResolver): concluído — implementado, testes, docs; branch feature/loja-local-resolver foi criada e CI verde.
- F5 (Uninstall & cleanup): concluído — LojaUninstaller implementado, ModuleManager.Remove uses the uninstaller; tests updated.
- F6 (Concorrência/Resiliência): concluído — LockHelper implemented with timeout/retry, LojaLocalResolver adapted to use locks with timeout; concurrency tests added.
- F7 (CLI/API): planejado — CLI for install/uninstall/dry-run after F5+F6.
- F8 (E2E/Smoke): em progresso — ajustes no smoke test para aumentar robustez (adicionado --wait nas chamadas de run) e reduzir false negatives.

## UI Holográfica (novo eixo — 2026-08-11)

Conceito de referência: vídeos Gemini (lua/sol + anéis + aurora + menu inferior Network / Sensor / Ethereum / System / Device).

| Fase | Descrição | Status |
|------|-----------|--------|
| **UI-Holo F0** | Análise + escolha de abordagem (MAUI puro sem SkiaSharp nesta fase) | **FEITO** 2026-08-11 |
| **UI-Holo F1** | Dashboard HomePage: orb central + bottom bar mapeada a capacidades reais | **EM ANDAMENTO** (branch `feature/ui-holographic-dashboard`) |
| **UI-Holo F2** | Animações leves (pulse/glow) + navegação dos 5 botões | planejado |
| **UI-Holo F3** | SkiaSharp/SKGLView ou WebView para efeitos holográficos avançados | planejado (só após F1/F2 estáveis no CI) |
| **UI-Holo F4** | Shell + TabBar customizado substituindo TabbedPage (opcional) | futuro |

### Mapeamento visual → AURA (F1)

| Botão (vídeo) | Capacidade AURA | Ação na UI |
|---------------|-----------------|------------|
| Network | `NetworkManager` | Refresh status de rede |
| Sensor | `SystemAnalyzer` | Refresh diagnóstico |
| Ethereum | (reservado) | Placeholder / futuro módulo |
| System | Sistema / Home | Já na própria página |
| Device | Células / device | Navega para seção Apps/Células quando disponível |

### Decisão de arquitetura (F0)

- **Escolhido para F1:** MAUI XAML + C# puro (Border, Grid, Labels) — zero nova dependência, build-android-apk inalterado.
- **Rejeitado por agora:** SkiaSharp / DrawnUI / WebView Three.js — aumenta superfície de falha e tempo de CI; entra só em F3 se F1/F2 validarem no dispositivo.
- Local de implementação: `src/AURA.Mobile/Pages/HomePage.*` (reuso da página Início existente).

Progresso recente
- Implemented LockHelper (timeout + exponential backoff) and adapted LojaLocalResolver to use locks with a 5s timeout.
- Added LockHelperTests and InstallerConcurrencyTests to validate lock behavior and installer concurrency.
- Finalized LojaUninstaller and integrated with ModuleManager.Remove.
- Updated scripts/smoke-test.sh to wait for cell completion (--wait) to improve reliability in CI.
- Updated roadmap and docs with current status.
- **2026-08-11:** iniciada UI-Holo F1 (dashboard holográfico) na branch feature/ui-holographic-dashboard.

CI re-run
- Action: triggered automated CI re-run from Copilot to validate smoke test fix.
- Trigger time (UTC): 2026-08-10T23:51:00Z
- Purpose: ensure smoke-test.sh change resolves intermittent failures; if failures persist Copilot will inspect logs and apply fixes.

Próximos passos imediatos
1. Merge UI-Holo F1 após CI mobile verde; validar visual no APK de debug.
2. UI-Holo F2: pulse no orb + handlers dos botões com feedback.
3. Analyze CI runs; if failures occur, diagnose and patch small issues automatically.
4. Implement CLI (F7) and smoke pipeline (F8) after stabilization.
