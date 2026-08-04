#!/usr/bin/env bash
# AURA — entrypoint único do orquestrador.
# O usuário escolhe um programa; a AURA decide como rodá-lo em uma célula.
# Uso:
#   aura                          # modo interativo
#   aura run app.py [--cell x]    # comando direto
#   aura cells | cell stop x | ...
set -euo pipefail

# --- Ambiente ---------------------------------------------------------------

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
export DOTNET_GCHeapHardLimit="${DOTNET_GCHeapHardLimit:-1C0000000}"
export DOTNET_GCHeapCount="${DOTNET_GCHeapCount:-2}"

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"

# AURA_ROOT: variável explícita > pasta do script > subir até achar AURA.sln
find_root() {
  if [[ -n "${AURA_ROOT:-}" ]] && [[ -f "$AURA_ROOT/AURA.sln" ]]; then
    printf '%s\n' "$AURA_ROOT"
    return 0
  fi
  local dir="$SCRIPT_DIR"
  while :; do
    if [[ -f "$dir/AURA.sln" ]]; then
      printf '%s\n' "$dir"
      return 0
    fi
    [[ "$dir" == "/" ]] && break
    dir="$(dirname "$dir")"
  done
  return 1
}

AURA_ROOT="$(find_root)" || {
  echo "[aura] AURA.sln não encontrado a partir de $SCRIPT_DIR. Rode scripts/setup.sh." >&2
  exit 1
}

# --- Log --------------------------------------------------------------------

LOG_DIR="$HOME/AURA/logs"
mkdir -p "$LOG_DIR"
LOG_FILE="$LOG_DIR/aura_launcher.log"
printf '[%s] exec: %s\n' "$(date +%Y-%m-%dT%H:%M:%S)" "$*" >> "$LOG_FILE"

# --- Localização do CLI -----------------------------------------------------

find_dll() {
  local dll="$AURA_ROOT/src/AURA.CLI/bin/Debug/net10.0/AURA.CLI.dll"
  if [[ -f "$dll" ]]; then
    printf '%s\n' "$dll"
    return 0
  fi
  dll="$AURA_ROOT/src/AURA.CLI/bin/Release/net10.0/AURA.CLI.dll"
  if [[ -f "$dll" ]]; then
    printf '%s\n' "$dll"
    return 0
  fi
  return 1
}

AURA_CLI_DLL="$(find_dll)" || {
  echo "[aura] CLI não compilado. Rode scripts/setup.sh." >&2
  exit 1
}

# --- Execução ---------------------------------------------------------------

if (( $# == 0 )); then
  exec dotnet "$AURA_CLI_DLL"
fi

exec dotnet "$AURA_CLI_DLL" "$@"
