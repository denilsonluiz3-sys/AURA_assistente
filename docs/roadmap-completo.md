# Roadmap Completo (estado atualizado)

Status geral
- F4 (LojaLocalResolver): concluído — implementado, testes, docs; branch feature/loja-local-resolver foi criada e CI verde.
- F5 (Uninstall & cleanup): em progresso — uninstaller implementado e testes base adicionados na branch feature/loja-local-resolver.
- F6 (Concorrência/Resiliência): planejado — melhorias no lock e testes de concorrência pendentes.
- F7 (CLI/API): planejado — CLI para install/uninstall/dry-run após F5+F6.
- F8 (E2E/Smoke): planejado — pipeline smoke após F5+F6+F7.

Próximos passos imediatos
1. Completar F5: revisar, adicionar testes adicionais (casos parciais, falta de installedFiles.json), e rodar CI em main.
2. Iniciar F6: implementar timeout/retry em TryAcquireLock, criar testes de concorrência multi-processo.
3. Implementar CLI (F7) e integrar com uninstaller e installer, bem como flags --dry-run e --force.
4. Criar pipeline de smoke (F8) que valide o ciclo Install → Apply → PluginWatcher.

Notas
- Todas as mudanças relacionadas a arquivos e instalações usam installedFiles.json como fonte da verdade para uninstall.
- Plugins root path agora é passado ao ModuleManager (constructor estendido). Ajuste consumidores se necessário.
