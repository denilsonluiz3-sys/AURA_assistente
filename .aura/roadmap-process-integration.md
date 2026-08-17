# AURA — Integração incremental de processos

- [x] Conectar estado de processos ao `EventBus`/Cells existente.
- [x] Registrar processos da Assistente sem alterar o fluxo atual de IA/orquestração.
- [x] Exibir processos como mini-cards na aba Assistente.
- [x] Manter processos fora da página para sobreviver à navegação.
- [x] Navegar do mini-card para a aba correspondente.
- [x] Preservar a identidade da Cell no processo exibido.
- [x] Conectar a execução da solicitação da Assistente ao ciclo vivo do `ProcessRegistry`.
- [ ] Associar tarefas planejadas a processos nomeados.
- [ ] Dividir uma solicitação em múltiplas tarefas/Cells.
- [ ] Retry isolado da tarefa que falhar.
- [ ] Revisão dos resultados e composição final.

Estado atual: solicitações da Assistente agora criam processos vivos, atualizam status/progresso durante a execução e concluem/falham no `ProcessRegistry`; Cells continuam compartilhando identidade no registro. Validação contínua pelo GitHub Actions.
