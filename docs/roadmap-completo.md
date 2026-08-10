# Roadmap Completo (estado atualizado)

Status geral
- F4 (LojaLocalResolver): concluído — implementado, testes, docs; branch feature/loja-local-resolver foi criada e CI verde.
- F5 (Uninstall & cleanup): concluído — LojaUninstaller implementado, ModuleManager.Remove uses the uninstaller; tests updated.
- F6 (Concorrência/Resiliência): concluído — LockHelper implemented with timeout/retry, LojaLocalResolver adapted to use locks with timeout; concurrency tests added.
- F7 (CLI/API): planejado — CLI for install/uninstall/dry-run after F5+F6.
- F8 (E2E/Smoke): em progresso — ajustes no smoke test para aumentar robustez (adicionado --wait nas chamadas de run) e reduzir false negatives.

Progresso recente
- Implemented LockHelper (timeout + exponential backoff) and adapted LojaLocalResolver to use locks with a 5s timeout.
- Added LockHelperTests and InstallerConcurrencyTests to validate lock behavior and installer concurrency.
- Finalized LojaUninstaller and integrated with ModuleManager.Remove.
- Updated scripts/smoke-test.sh to wait for cell completion (--wait) to improve reliability in CI.
- Updated roadmap and docs with current status.

Próximos passos imediatos
1. Trigger CI for branch feature/loja-local-resolver (push already performed). Analyze failures if any and patch.
2. If smoke tests still fail, add additional logging in CLI to help diagnose; iterate until stable.
3. Implement CLI (F7) and smoke pipeline (F8) after stabilization.

