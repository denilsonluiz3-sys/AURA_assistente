# AURA Android

## Ownership
AURA.Mobile, MAUI, Android, permissões, filesystem, UI e APK.

## Ordem de pesquisa
src/AURA.Mobile -> Platforms/Android -> MauiProgram -> Pages -> Services -> workflows

## Roadmap
F7 — CLI/API
F8 — E2E/Smoke

F4/F5/F6 somente manutenção e regressão.

## Proibido
colocar Android em Core, provider em Page, AgentSession na UI ou duplicar serviços Core.

## Regra
SEARCH -> LOCATE -> REUSE -> PLAN -> CHANGE -> BUILD -> TEST -> REVIEW

Nunca criar implementação paralela sem provar que a existente não pode ser reutilizada.
