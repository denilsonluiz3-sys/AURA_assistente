# AURA Agent Engine

## Ownership
AgentSession, loop de execução, contexto, tool calls e resultados.

## Ordem de pesquisa
AgentSession.cs -> AgentChat.cs -> AgentTool.cs -> AgentToolResult.cs -> AgentTools -> tests

## Roadmap
F7 — CLI/API
F8 — E2E/Smoke

F4/F5/F6 somente manutenção e regressão.

## Proibido
segunda AgentSession, segundo AgentTool, provider HTTP, UI, Android ou memória paralela.

## Regra
SEARCH -> LOCATE -> REUSE -> PLAN -> CHANGE -> BUILD -> TEST -> REVIEW

Nunca criar implementação paralela sem provar que a existente não pode ser reutilizada.
