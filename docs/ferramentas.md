# Orquestração das ferramentas de desenvolvimento da AURA

Este documento define **como distribuir, gerenciar, delegar e executar**
trabalho usando as 4 camadas de assistência disponíveis:

| Camada | Ferramenta | Papel |
|---|---|---|
| 1 | **opencode** (eu) | Núcleo: código C#, arquitetura, testes, decisões |
| 2 | **aichat** | Assistente LLM poderoso: revisão, geração, análise, tools |
| 3 | **termux-ai** | Assistente leve no celular: consultas rápidas, fallback |
| 4 | **GitHub** | Fonte de verdade: histórico, CI, issues, releases |

## Princípio de delegação

> **Regra de ouro**: o `opencode` decide *o quê* e *por quê*; os assistentes
> LLM executam *como* (revisões, rascunhos, análises); o GitHub garante *onde*
> (histórico, CI, entrega).

```
Quero X ──► opencode (planeja) ──► aichat/termux-ai (executa apoio) ──► git push (valida/CI)
                                        ▲                                      │
                                        └────────── revisão ◄─────────────────┘
```

## Matriz de delegação por tarefa

| Tarefa | opencode | aichat | termux-ai | GitHub |
|---|---|---|---|---|
| Implementar feature F3 | **lidera** | revisa código | — | branch + PR |
| Review de código | decisão final | `-r aura-review` | — | CodeQL + PR review |
| Gerar testes unitários | valida/adapta | rascunha | — | CI roda |
| Consulta rápida (conceito, comando) | — | `aichat "..."` | `termux-ai "..."` | — |
| Rodar comandos shell | — | `aichat -e "..."` | — | Actions workflow |
| Documentar | revisa | rascunha | — | wiki/docs no repo |
| Loja de módulos (F4) | desenha formato | — | — | **Releases** servem `.dll` |
| Reportar bug | — | — | `termux-ai` relata | **Issue** com label |
| CI/build/test | — | — | — | **Actions** |

## Como eu (opencode) uso cada uma

### aichat — o "braço de análise"
- **Revisão**: `aichat -r aura-review -f <arquivo.cs> "Revise."` (já configurado;
  achou bug real no CellStore).
- **Consulta com contexto**: `aichat -f docs/README.md "explique o roadmap"`.
- **Execução shell**: `aichat -e "liste células em ~/AURA"`.
- **Sessões**: `aichat -s aura-f3` para manter contexto entre chamadas.
- **Tools (function calling)**: ao configurar `use_tools`, o aichat pode
  executar comandos reais (data, fs, web) em vez de "inventar" (como fez a data
  errada de 2024).

### termux-ai — o "assistente do celular"
- Usar quando o aichat não estiver disponível (Termux puro, sem proot).
- Consultas rápidas e discretas; custo baixo; sem tools.
- Futuro: virar **célula do AURA** (F3) — `aura run termux-ai --cell chat`.

### GitHub — o "arquivo e o CI"
- **Repositório**: `denilsonluiz3-sys/AURA_assistente` (privado desejado;
  hoje público).
- **Commit**: toda mudança validada local (build 0 erros + smoke-test) antes
  do push.
- **Actions**: build + testes em Linux real (resolve o bloqueio do VSTest no
  proot).
- **Issues**: roadmap F3–F7 com labels; bugs com reprodução.
- **Releases**: futuras — distribuição de plugins e binário (alimenta `aura update`).

## Como adicionar ferramentas úteis ao aichat (llm-functions)

O aichat suporta **tools e agents** via
[llm-functions](https://github.com/sigoden/llm-functions) (bash/js/python).
Pré-requisitos: `argc` + `jq` (`jq` já presente; instalar `argc`).

```
git clone https://github.com/sigoden/llm-functions ~/.config/aichat/functions
cd ~/.config/aichat/functions
# tools.txt com os tools desejados (ex.: execute_command.sh, fs_*.sh)
argc build          # gera functions.json
argc link-to-aichat # symlink para o functions_dir do aichat
```

Exemplo de tool própria (bash), que dá ao aichat acesso ao runtime da AURA:

```bash
#!/usr/bin/env bash
set -e

# @describe Executa um comando no CLI da AURA.
# @option --command! O comando AURA (ex.: cells, launchers, run app.py)

main() {
    printf '%s\n' "$argc_command" |
        dotnet ~/AURA/src/AURA.CLI/bin/Debug/net10.0/AURA.CLI.dll
}
eval "$(argc --argc-eval "$0" "$@")"
```

## Ferramentas desejáveis a adicionar

1. **`aura-cli` tool** — aichat conversa com o runtime da AURA (células, logs).
2. **`git-status` / `git-log`** — aichat sabe o estado do repo antes de propor.
3. **`web-search`** — via Perplexity/Tavily (escolher e linkar `web_search.sh`).
4. **Agent `aura-dev`** — combina tools + role de review + RAG do `docs/`.
5. **RAG do projeto** — indexar `docs/` e `src/` para o aichat responder sobre o
   código com base nos arquivos reais.

## Fluxo de trabalho recomendado

1. **Iniciar feature**: eu abro Issue no GitHub (ex.: `F3 célula-assistente`).
2. **Desenvolver**: eu implemento; aichat revisa a cada passo; commit por passo.
3. **Validar**: `smoke-test.sh` + `dotnet build` locais; CI no GitHub confirma.
4. **Entregar**: merge; Release quando for F4.
5. **Iterar**: bugs viram issues; termux-ai relata do celular; eu corrijo.

## Estabilidade do ambiente (proot/Termux)

**Problema real observado**: o proot desmonta `/data/data/com.termux` no meio
da sessão. Ferramentas que moram lá (`aichat`, `jq`) somem; `/root` sobrevive.

**Regras para manter o ambiente estável:**

1. **Tudo crítico vive em `/root`** (estável por construção):
   - `~/bin/argc`, `~/.config/aichat/functions/`, `~/.config/aichat/config.yaml`,
     `~/.local/share/termux-ai/config.json`.
2. **Após qualquer remontagem, rode**: `bash scripts/check-env.sh`
   — reporta exatamente o que sumiu e o comando para restaurar.
3. **O repo git local pode corromper** (inodes `-?????????`). Sempre que isso
   ocorrer, use o clone íntegro: `git clone git@github.com:denilsonluiz3-sys/AURA_assistente.git`.
4. **Builds locais são instáveis** (deps.json incompleto, VSTest ARM64
   bloqueado). Não dependa deles para decisão — **confie no GitHub Actions**
   (runner x64 limpo, ~30s) para validar build+tests+smoke.
5. **Padrão fire-and-forget**: push → CI roda em ~30s → trabalhe noutra coisa →
   volte e leia o resultado na API.

**Se o mount voltar** (proot reiniciado): copie `aichat` e `jq` para `/root/bin`
para blindar contra a próxima remontagem.
