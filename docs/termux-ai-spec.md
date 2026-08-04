# Especificação para o termux-ai — scripts de instalação e entrypoint da AURA

Você é o agente `termux-ai`, responsável pelos scripts Bash de operação da
AURA no **Termux real**. O código .NET (runtime de células, persistência,
plugins) já está pronto e buildando com 0 erros. Seu trabalho é:

1. `scripts/setup.sh` — instalação do ambiente.
2. `scripts/aura.sh` — entrypoint que abre o CLI da AURA (substituindo o chat
   OpenRouter atual).
3. `scripts/smoke-test.sh` — verificação automatizada de que a AURA funciona.

## Contexto de arquitetura

- AURA é um orquestrador de aplicativos: o usuário escolhe um programa
  (arquivo `.py`, `.jar`, `.dll`, `.txt`, ...) e a AURA decide como rodá-lo
  dentro de uma célula isolada (processo OS separado).
- Frontend atual: CLI interativa em `src/AURA.CLI` (namespace `AURA.CLI`),
  assembly `AURA.CLI.dll`, framework `net10.0`.
- Build: `dotnet build AURA.sln` dentro de `$AURA_ROOT` (pasta do projeto).
- Dados em runtime:
  - células: `~/AURA/cells/<id>/` (cada célula tem `cell.log`)
  - índice persistido: `~/AURA/cells.json`
  - plugins: `~/AURA/plugins/*.dll` (hot-reload automático)
- Comandos do CLI (interativo ou `aura.sh "comando arg"`):
  - `run <arquivo> [args] [--cell <id>]`
  - `cells`
  - `cell start|stop|pause|resume|delete|log <id>`
  - `persist` / `save`
  - `diagnostico`, `internet`, `modulos`, `launchers`, `plugins`
  - `ajuda`, `exit`

## Restrições de plataforma (CRÍTICO)

- O target é **Termux real em arm64** (`linux-bionic`). Cross-publish
  single-file para `linux-bionic-arm64` está **quebrado** no .NET 9/10 —
  a compilação DEVE acontecer no próprio Termux.
- Workaround OOM do GC em ARM64 (documentado, obrigatório):
  ```bash
  export DOTNET_GCHeapHardLimit=1C0000000 DOTNET_GCHeapCount=2
  ```
- `pkg` (Termux) só roda como usuário normal; **nunca** como root.
- A AURA usa sinais (`SIGSTOP`/`SIGCONT` = pause/resume de célula) e
  `kill(pid, SIGKILL)` para encerrar células — nada disso precisa de root.
- Não usar `sudo`. Não assumir `systemd`.

## 1. `scripts/setup.sh`

Função: deixar o Termux pronto para rodar a AURA. Deve:

- Detectar se está no Termux (`$PREFIX` definido, ex. `/data/data/com.termux/files/usr`).
  Se não estiver, abortar com mensagem clara.
- Abortar se `id -u` = 0 (pkg não roda como root).
- Instalar dependências de sistema via `pkg update && pkg install -y`:
  - `dotnet10.0` (SDK/runtime .NET 10)
  - `python3`, `openjdk-17` (launchers .py e .jar), `curl`
  - `termux-tools` (já presente; garantir)
- Após instalar, configurar o ambiente de build:
  - setar os exports OOM acima de forma permanente em `~/.bashrc`
    (idempotente: só adiciona se ainda não estiverem lá).
- Rodar `dotnet build` do projeto e reportar sucesso/erro.
- Se o CLI for chamado de qualquer diretório, criar um symlink ou wrapper
  `~/bin/aura` apontando para `scripts/aura.sh` (criar `~/bin` se preciso).
- Ser idempotente: rodar duas vezes não deve duplicar exports nem quebrar.

## 2. `scripts/aura.sh`

Função: entrypoint único da AURA. **Substituir o conteúdo atual** (que era um
chat OpenRouter) pelo novo comportamento:

- Shebang termux: `#!/data/data/com.termux/files/usr/bin/bash` com fallback
  seguro para `#!/usr/bin/env bash` se o primeiro não existir (não dá para
  dois shebangs — escolha: use `#!/usr/bin/env bash` e um teste no corpo para
  detectar Termux, já que o proot/CI usa `/usr/bin/bash`).
- `set -euo pipefail`.
- Localizar a raiz do projeto: preferir `$AURA_ROOT`, senão `~` / pasta atual
  contendo `AURA.sln` (procurar subindo os diretórios).
- Aplicar os exports OOM do GC se ainda não estiverem no ambiente.
- Locate CLI: `$AURA_ROOT/src/AURA.CLI/bin/Debug/net10.0/AURA.CLI.dll`
  (Debug) com fallback para `bin/Release/net10.0/AURA.CLI.dll`. Se não
  existir, sugerir rodar `scripts/setup.sh` e abortar.
- Invocar o CLI via `dotnet "$AURA_CLI_DLL" "$@"`.
  - Se passar argumentos (ex. `aura.sh run app.py`), repassa todos para o CLI.
  - Se não passar nenhum, abre o modo interativo do CLI.
- Preservar o exit code do CLI.
- Logar timestamps das execuções em `~/AURA/logs/aura_launcher.log` (append).
- Manter, opcionalmente, um modo `chat` legado? **Não.** O chat OpenRouter
  será movido para um plugin/célula futura. O `aura.sh` é só o entrypoint do
  orquestrador.

## 3. `scripts/smoke-test.sh`

Função: prova de fumaça não-interativa da AURA. Deve:

- Rodar o setup de ambiente (exports OOM) e localizar o DLL (mesma lógica do
  aura.sh — extrair para uma função compartilhada se quiser).
- Executar o CLI com comandos piped e conferir saída com `grep`:
  1. `diagnostico` → deve conter `Sistema operacional`.
  2. `launchers` → deve conter `PythonLauncher`.
  3. `run` de um arquivo `.py` de teste que imprime `AURA_SMOKE_OK`, com
     `--cell smoke` → depois `cell log smoke` → deve conter
     `AURA_SMOKE_OK`; depois `cell stop smoke` e `cell delete smoke`.
  4. `cells` depois do delete → NÃO deve conter `smoke`.
  5. `persist` → deve imprimir `Células persistidas`.
  6. `plugins` → pode imprimir `nenhum plugin` (ok).
- Criar o arquivo `.py` temporário em `$TMPDIR` e apagar ao final (trap).
- Se qualquer passo falhar: imprimir `SMOKE TEST FALHOU` e exit 1. Senão
  `SMOKE TEST OK`.
- Tempo limite por passo para não pendurar (ex. `timeout 60`).

## Critérios de aceite

- `scripts/setup.sh` roda no Termux real e deixa `aura` disponível.
- `scripts/aura.sh` (com ou sem args) abre a AURA e o exit code propaga.
- `scripts/smoke-test.sh` termina com `SMOKE TEST OK`.
- Nenhum comando exige root. Nenhuma variável de ambiente hardcoded de
  caminho do celular que não seja derivada de `$PREFIX` ou `$HOME`.
- Scripts seguem `shellcheck` sem erros fatais (`#!/usr/bin/env bash` é
  aceito; evite `errexit`+pipefail pegadinhas com o CLI — o CLI devolve 0).

## Como validar (agente)

- Você NÃO tem Termux real neste ambiente; o `pkg` não roda como root aqui.
  Teste a lógica no proot Ubuntu disponível (`/usr/bin/dotnet`, `bash`):
  - `bash -n scripts/*.sh` para sintaxe.
  - Rodar `smoke-test.sh` no proot: ele deve funcionar (dotnet existe).
  - `setup.sh` deve detectar "não é Termux" ou "roda como root" e abortar
    GRACIOSAMENTE aqui (não pode falhar feio).
- Reporte quais partes só podem ser validadas no celular.
