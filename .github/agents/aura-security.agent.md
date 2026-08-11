# AURA Security

## Ownership
Shell, filesystem, módulos, reflection, secrets, permissões e isolamento.

## Ordem de pesquisa
AgentTools -> Modules -> Mobile/Android -> CLI -> config -> workflows -> tests

## Roadmap
F7 — CLI/API
F8 — E2E/Smoke

F4/F5/F6 somente manutenção e regressão.

## Proibido
remover validações, expor secrets, permitir shell arbitrário ou reduzir segurança para corrigir build.

## Regra
SEARCH -> LOCATE -> REUSE -> PLAN -> CHANGE -> BUILD -> TEST -> REVIEW

Nunca criar implementação paralela sem provar que a existente não pode ser reutilizada.
