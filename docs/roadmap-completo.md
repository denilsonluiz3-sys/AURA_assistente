# Roadmap Completo (estado atualizado)

Status geral
- F4 (LojaLocalResolver): concluído — implementado, testes, docs; branch feature/loja-local-resolver foi criada e CI verde.
- F5 (Uninstall & cleanup): concluído — LojaUninstaller implementado, ModuleManager.Remove usa o uninstaller; testes atualizados.
- F6 (Concorrência/Resiliência): em progresso — LockHelper implementado com timeout/retry e testes básicos de concorrência adicionados.
- F7 (CLI/API): planejado — CLI para install/uninstall/dry-run após F5+F6.
- F8 (E2E/Smoke): planejado — pipeline smoke após F5+F6+F7.

Progresso recente
- Implementado LockHelper (timeout + exponential backoff) e adaptação do LojaLocalResolver para usar locks com timeout de 5s.
- Adicionados testes de concorrência (LockHelperTests).
- Finalizado LojaUninstaller e integração com ModuleManager.Remove.
- Atualizado roadmap e docs com o estado atual.

Próximos passos imediatos
1. Rodar CI na branch feature/loja-local-resolver e analisar possíveis falhas.
2. Completar testes do Uninstaller (casos limites) e adicionar dry-run se desejado.
3. Implementar testes multi-processo para o instalador (Execuções paralelas do InstallFromLoja) e observar comportamento em Windows/Ubuntu.
4. Após estabilizar, abrir PR para revisão e merge (squash) para main.

Notas
- O fluxo de auto-merge está disponível via workflow .github/workflows/ci-and-auto-merge.yml; para uso do auto-merge configure o segredo COPILOT_PAT com um PAT de curta validade.
- Recomenda-se rodar os testes em runners Windows e Ubuntu para garantir lock semantics cross-platform.
