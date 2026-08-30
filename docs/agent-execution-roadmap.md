# AURA — Pipeline único de execução do Agente

## Objetivo

Fazer Agente, Terminal e as capacidades existentes usarem uma única identidade de execução e uma única superfície temporária, preservando os motores atuais.

## Ordem de implementação

1. **Identidade da execução** — `ProcessRegistry.Begin()` gera o `ProcessId`; `ExecutionRequest.CorrelationId` passa a carregar essa identidade.
2. **Contexto** — `WorkingDirectory` é definido antes do executor e deve ser `AgentWorkspace.ActiveRoot` no fluxo do Agente.
3. **Execução Shell** — `ShellExecutor` continua sendo o motor real; não criar DSL nem shell paralelo.
4. **Saída incremental** — `stdout` e `stderr` são encaminhados com `CorrelationId`.
5. **Superfície** — `AgentCapabilitySurface` vincula-se a um processo específico; exibe executando, saída incremental e estado terminal.
6. **Entrega** — resultado final é devolvido ao fluxo do Agente; a superfície é apresentação, não um segundo executor.
7. **Programas** — `run_program` deve passar pelo mesmo coordenador/superfície e continuar usando `IAuraCellContextFactory` + `CellProgramRunner` reais.
8. **Executores** — Python, Node e Git devem reutilizar o mesmo pipeline sem duplicar execução.
9. **Capacidades** — Memória, Android e Células devem reutilizar a mesma identidade/superfície quando houver execução observável; operações puramente locais continuam sem processo artificial.
10. **Deduplicação** — uma intenção do Agente não pode criar duas execuções nem duas respostas finais; o `ProcessId` deve ser a chave de correlação.
11. **Limpeza do AgentPage** — retirar somente caminhos que se tornarem redundantes após a migração e preservar o comportamento atual durante a transição.
12. **Redução da UI** — depois de validar o pipeline, manter Agente/Terminal como portas principais; esconder superfícies secundárias sem apagar seus motores.
13. **Validação** — build em cada lote, testes de execução real no aparelho e publicação do APK somente com `0 Error(s)`.

## Contratos de pronto

- Cada execução possui exatamente um `ProcessId`/`CorrelationId`.
- A superfície nunca mostra saída de outra execução.
- O cwd usado pelo Agente é o workspace ativo.
- `stdout`/`stderr` aparecem progressivamente quando o executor fornece eventos.
- O resultado final aparece uma única vez na conversa.
- Cancelamento e falha fecham corretamente o estado do processo.
- `run_program`, Python, Node, Git, Android e Células não criam motores paralelos.
- Nenhuma capacidade existente é removida durante a migração.

## Estado atual

A infraestrutura de correlação já existe em `ExecutionRequest`, `ProcessExecutorBase`, `ProcessRegistry` e `AgentCapabilitySurface`. `AgentExecutionCoordinator` agora expõe eventos de início, saída e conclusão para fechar a integração sem depender de heurísticas de cwd.

A migração do `AgentPage.xaml.cs` deve ser feita de forma localizada: o arquivo é grande e deve receber apenas a troca do ponto de execução, mantendo o restante intacto. O build atual verde é o checkpoint antes dessa migração.
