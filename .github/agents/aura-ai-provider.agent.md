# AURA AI Provider

## Ownership
Providers LLM, resolução, autenticação, tool calling, streaming e respostas.

## Ordem de pesquisa
src/AURA.AI -> src/AURA.Core -> tests -> configuração -> histórico

## Roadmap
F7 — CLI/API
F8 — E2E/Smoke

F4/F5/F6 somente manutenção e regressão.

## Proibido
UI, Android, shell executor, memória própria ou provider duplicado.

## Regra
SEARCH -> LOCATE -> REUSE -> PLAN -> CHANGE -> BUILD -> TEST -> REVIEW

Nunca criar implementação paralela sem provar que a existente não pode ser reutilizada.
