# Agent Capability Surfaces

## Objetivo

As capacidades da AURA continuam sendo os motores existentes. A interface do Agente apresenta uma superfície transitória quando uma capacidade precisa ser usada, em vez de duplicar o terminal, executor ou runtime.

## Princípios

- Um único motor por capacidade.
- Terminal do Agente e Terminal nativo usam `ShellExecutor`.
- A superfície é somente apresentação; não cria uma segunda implementação de execução.
- A saída pode ser acumulada durante a execução.
- A superfície pode ser fechada sem destruir o motor ou o estado.
- Novas capacidades reutilizam a mesma superfície antes de ganhar uma aba própria.
- A superfície não deve decidir permissões: segurança e políticas permanecem nos motores existentes.
- O Agente não deve inventar caminhos, resultados ou capacidades; deve consultar o runtime real.
- Falha de uma capacidade deve voltar como estado/resultado para o Agente, sem matar a sessão.

## Fluxo-alvo

`Agente -> capacidade existente -> superfície transitória -> execução realtime -> resultado -> Agente`

## Fases

### Fase 1 — superfície reutilizável

Componente `AgentCapabilitySurface` criado e incorporado à área de conversa. Ele apresenta título, estado, progresso de execução, saída incremental e fechamento manual.

### Fase 2 — Terminal como prova de integração

Conectar o `ShellExecutor` existente à superfície, sem criar outro shell. O Terminal do Agente deve usar o mesmo workspace/cwd que o contexto operacional da AURA. A saída stdout/stderr deve poder aparecer incrementalmente.

Critérios:

- `pwd` do Terminal nativo e do Agente apontam para o mesmo contexto.
- `ls` mostra a mesma árvore.
- `cat` acessa os mesmos arquivos.
- execução longa mantém a superfície atualizada.
- encerramento não deixa processo órfão.
- o resultado final volta para a conversa.

### Fase 3 — Programas e executores

Reutilizar a superfície para `run_program`, Python e Node. Os motores existentes continuam responsáveis por execução, contexto, cancelamento e segurança.

### Fase 4 — Memória, Git, Android e Web

Apresentar busca/salvamento de memória, operações Git, capacidades Android e consultas Web como superfícies temporárias quando houver saída relevante para o usuário.

### Fase 5 — Células e processos

Usar a mesma superfície para acompanhar processos e células em execução, aproveitando o `ProcessRegistry` e o runtime existentes. A UI deve observar o estado, não criar um segundo ciclo de execução.

### Fase 6 — Simplificação da navegação

Somente depois das fases anteriores validadas no aparelho, reduzir abas e atalhos permanentes. Nenhuma capacidade deve ser removida apenas para simplificar a UI.

## Não fazer

- Não criar DSL de comandos.
- Não duplicar `ShellExecutor`.
- Não criar um segundo runtime.
- Não mover a lógica de segurança para a UI.
- Não esconder ferramentas antes de comprovar que o Agente consegue invocá-las.
- Não substituir componentes funcionais por mocks/NoOp.

## Estado da implementação

- Superfície reutilizável: implementada.
- Host visual no AgentPage: implementado.
- Execução realtime pelo ShellExecutor: próximo ponto de integração.
- Workspace/cwd compartilhado: precisa de validação e ajuste mínimo.
- Demais capacidades: reutilizar após o Terminal.
