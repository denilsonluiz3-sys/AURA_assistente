#!/usr/bin/env bash
# AURA — espaço de auto-melhoria.
# Clona o repositório AURA num workspace isolado e abre uma célula opencode
# apontando para esse clone, para que a própria AURA possa ler/editar os
# arquivos dela sem tocar no repositório principal.
#
# Uso:
#   aura-workspace.sh clone   # clona (ou atualiza) ~/AURA/workspace/AURA_assistente
#   aura-workspace.sh open    # abre célula opencode no workspace (via CLI da AURA)
#   aura-workspace.sh status  # mostra o estado do workspace
set -euo pipefail

log() { printf '[workspace] %s\n' "$*" >&2; }
die() { log "ERRO: $*"; exit 1; }
have() { command -v "$1" >/dev/null 2>&1; }

# --- Localização do projeto --------------------------------------------------

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
AURA_ROOT="${AURA_ROOT:-$(dirname "$SCRIPT_DIR")}"

if [[ ! -f "$AURA_ROOT/AURA.sln" ]]; then
  die "AURA.sln não encontrado em $AURA_ROOT. Ajuste AURA_ROOT."
fi

REPO_URL_HTTPS="https://github.com/denilsonluiz3-sys/AURA_assistente.git"
REPO_URL_SSH="git@github.com:denilsonluiz3-sys/AURA_assistente.git"
WORKSPACE_ROOT="${AURA_WORKSPACE:-$HOME/AURA/workspace}"
WORKSPACE_REPO="$WORKSPACE_ROOT/AURA_assistente"

# --- Ambiente (GC OOM p/ ARM64/Termux) --------------------------------------

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
export DOTNET_GCHeapHardLimit="${DOTNET_GCHeapHardLimit:-1C0000000}"
export DOTNET_GCHeapCount="${DOTNET_GCHeapCount:-2}"

# True if WORKSPACE_REPO holds a valid clone (a commit reachable from HEAD).
is_valid_clone() {
  [[ -d "$WORKSPACE_REPO/.git" ]] && \
    ( cd "$WORKSPACE_REPO" && git rev-parse --verify -q HEAD >/dev/null 2>&1 )
}

clone() {
  mkdir -p "$WORKSPACE_ROOT"

  # Repo corrompido/incompleto (init vazio sem HEAD): refaz do zero.
  if [[ -d "$WORKSPACE_REPO/.git" ]] && ! is_valid_clone; then
    log "Workspace sem commit válido; removendo e clonando novamente..."
    rm -rf "$WORKSPACE_REPO"
  fi

  if is_valid_clone; then
    log "Workspace já existe; atualizando (git pull)..."
    ( cd "$WORKSPACE_REPO" && git pull --ff-only )
    return 0
  fi

  log "Clonando AURA em $WORKSPACE_REPO (HTTPS)..."
  if ! git clone "$REPO_URL_HTTPS" "$WORKSPACE_REPO"; then
    log "HTTPS falhou; tentando SSH..."
    git clone "$REPO_URL_SSH" "$WORKSPACE_REPO"
  fi
  log "Clone concluído."
}

open_cell() {
  clone
  AURA_CLI="$AURA_ROOT/src/AURA.CLI/bin/Debug/net10.0/AURA.CLI.dll"
  [[ -f "$AURA_CLI" ]] || AURA_CLI="$AURA_ROOT/src/AURA.CLI/bin/Release/net10.0/AURA.CLI.dll"
  if [[ ! -f "$AURA_CLI" ]]; then
    die "CLI não compilado. Rode scripts/setup.sh primeiro."
  fi

  log "Criando célula opencode no workspace (cd $WORKSPACE_REPO)..."
  ( cd "$WORKSPACE_REPO" && dotnet "$AURA_CLI" run opencode --cell dev )
}

status() {
  if ! is_valid_clone; then
    log "Workspace vazio ou inválido. Rode 'aura-workspace.sh clone'."
    return 0
  fi

  log "Workspace: $WORKSPACE_REPO"
  ( cd "$WORKSPACE_REPO" && git rev-parse --short HEAD && git status -sb | head -20 )
  printf 'Para auto-melhoria, dentro da AURA use:\n'
  printf '  run opencode --cell dev   (no dir do workspace)\n'
  printf '  ask "melhore o AgentManager" --assistente opencode\n'
}

case "${1:-clone}" in
  clone)  clone ;;
  open)   open_cell ;;
  status) status ;;
  *) die "Subcomando desconhecido: $1 (use clone|open|status)." ;;
esac
