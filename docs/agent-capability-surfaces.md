# Agent Capability Surfaces

## Objetivo

As capacidades da AURA continuam sendo os motores existentes. A interface do Agente apresenta uma superfície transitória quando uma capacidade precisa ser usada, em vez de duplicar o terminal, executor ou runtime.

## Princípios

- Um único motor por capacidade.
- Terminal do Agente e Terminal nativo usam `ShellExecutor`.
- A superfície é somente apresentação; não cria uma segunda implementação de execução.
- Saída pode ser acumulada durante a execução.
- A superfície pode ser fechada sem destruir o motor ou o estado.
- Novas capacidades reutilizam a mesma superfície antes de ganhar uma aba própria.

## Fluxo

`Agente -> capacidade existente -> superfície transitória -> resultado -> Agente`

## Fase 1

Introduzir o componente reutilizável `AgentCapabilitySurface`. A integração de execução deve ser feita sobre os pontos atuais do `AgentPage`, preservando `ShellExecutor`, `CellProgramRunner`, `MemoryStore`, executores e `IAndroidCapabilityService`.

## Fase 2

Conectar a saída realtime do `ShellExecutor` à superfície, respeitando o `WorkingDirectory` compartilhado do workspace. Depois reutilizar o componente para programas, memória, Python, Node, Git e Android.

## Fase 3

Com as superfícies validadas no aparelho, reduzir a navegação principal. As capacidades continuam disponíveis ao Agente e não são removidas.
