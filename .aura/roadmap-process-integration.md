# AURA — Integração incremental de processos

## Núcleo como processo jurídico (LegalProcessEngine)
- [x] Conectar estado de processos ao `EventBus`/Cells existente.
- [x] Registrar processos da Assistente sem alterar o fluxo atual de IA/orquestração.
- [x] Exibir processos como mini-cards na aba Assistente.
- [x] Manter processos fora da página para sobreviver à navegação.
- [x] Navegar do mini-card para a aba correspondente.
- [x] Preservar a identidade da Cell no processo exibido.
- [x] Conectar a execução da solicitação da Assistente ao ciclo vivo do `ProcessRegistry`.
- [x] Publicar etapas do `AuraOrchestrator` no `EventBus` e refletir nos mini-cards.
- [x] Conectar solicitações explicitamente orquestradas da Assistente ao `AuraOrchestrator`.
- [x] Criar `IProcessOrchestrator` como porta única que une chat e agentes (fases jurídicas).
- [x] Criar `LegalProcessEngine` percorrendo fases pré-processual → conhecimento → decisão → recursal → execução → arquivamento.
- [x] Associar decisões if/else de cada fase aos blocos existentes (MemoryAgent, AutomationAgent, AIAgent, orquestrador, LLM opcional).
- [x] `KnowledgeManager` como agente IAgent de conhecimento offline/online (cache + DuckDuckGo + aprendizado local).
- [x] `ChatPage` roteia toda solicitação pelo `IProcessOrchestrator` (unifica chat + agentes).
- [x] Retry isolado da etapa que falhar (fase recursal).
- [x] Revisão dos resultados e composição final (arquivamento).

## Próximo núcleo (pendente)
- [ ] Dividir uma solicitação em múltiplas tarefas/Cells nomeadas.
- [ ] Expor sentenças jurídicas (`Verdict`) no card de processo da UI.
- [ ] Motor de conhecimento ampliado (perguntas frequentes embutidas e tópicos do sistema).

Estado atual: o `LegalProcessEngine` conduz cada solicitação como um processo jurídico — conciliação via memória/conhecimento local, instrução com pesquisa, sentença (com LLM opcional), recurso com retry isolado, execução e arquivamento — publicando cada fase no `EventBus` para os mini-cards da Assistente. Validação contínua pelo GitHub Actions.