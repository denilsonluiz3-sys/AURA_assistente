# Prompts da AURA

Prompts de sistema (system prompts) usados para desenvolver e operar a AURA.

## Como usar com o LobeHub

1. Abra [LobeHub](https://lobehub.com) → crie um novo assistente.
2. Use o conteúdo de `systemRole` de um arquivo desta pasta como o prompt do
   assistente (ex.: `aura-dev.md`).
3. Itere: peça ao LobeHub para otimizar o prompt (agentes de "prompt
   engineering"), depois atualize o arquivo aqui.
4. O resultado vira o comportamento do `opencode` quando ele roda no
   workspace (`scripts/aura-workspace.sh open` → `ask "..." --assistente opencode`).

## Arquivos

| Arquivo | Agente | Uso |
|---|---|---|
| `aura-dev.md` | AURA Dev | Auto-melhoria do código da AURA (opencode) |

## Fluxo proposto (LobeHub → AURA)

```
LobeHub (itera prompt) ──► prompts/*.md (fonte de verdade) ──► opencode no
workspace executa com o prompt ──► build + smoke-test + CI ──► push
```

O `systemRole` de `aura-dev.md` consolida a compatibilidade: **termux-ai** e
**opencode** (e aichat) são todos assistentes da AURA; o opencode é o único que
edita o repositório — é por ele que a AURA se melhora.
