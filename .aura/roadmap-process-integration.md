# AURA — Integração incremental de processos

- [x] Conectar estado de processos ao `EventBus`/Cells existente.
- [x] Registrar processos da Assistente sem alterar o fluxo atual de IA/orquestração.
- [x] Exibir processos como mini-cards na aba Assistente.
- [x] Manter processos fora da página para sobreviver à navegação.
- [x] Navegar do mini-card para a aba correspondente.
- [x] Preservar a identidade da Cell no processo exibido.
- [x] Conectar a execução da solicitação da Assistente ao ciclo vivo do `ProcessRegistry`.
- [x] Publicar etapas do `AuraOrchestrator` no `EventBus` e refletir nos mini-cards.
- [x] Conectar solicitações explicitamente orquestradas da Assistente ao `AuraOrchestrator`.
- [ ] Associar tarefas planejadas a processos nomeados.
- [ ] Dividir uma solicitação em múltiplas tarefas/Cells.
- [ ] Retry isolado da tarefa que falhar.
- [ ] Revisão dos resultados e composição final.

Estado atual: a Assistente possui cards vivos para o fluxo normal e para o `AuraOrchestrator`; o orquestrador publica entendimento, planejamento, pesquisa, execução, revisão, falha e conclusão pelo `EventBus`. Próximo núcleo: tarefas nomeadas, divisão em múltiplas Cells, retry isolado e composição final. Validação contínua pelo GitHub Actions.
