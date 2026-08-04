#!/usr/bin/env bash
# AURA — smoke test não-interativo.
# Valida que o CLI abre, roda uma célula Python e persiste/limpa tudo.
# Saída final: "SMOKE TEST OK" (exit 0) ou "SMOKE TEST FALHOU" (exit 1).
set -uo pipefail

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
export DOTNET_GCHeapHardLimit="${DOTNET_GCHeapHardLimit:-1C0000000}"
export DOTNET_GCHeapCount="${DOTNET_GCHeapCount:-2}"

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
FAIL=0

log()  { printf '[smoke] %s\n' "$*" >&2; }
pass() { log "OK: $*"; }
fail() { log "FALHOU: $*"; FAIL=1; }

# --- Localização (mesma lógica do aura.sh) ----------------------------------

find_root() {
  if [[ -n "${AURA_ROOT:-}" ]] && [[ -f "$AURA_ROOT/AURA.sln" ]]; then
    printf '%s\n' "$AURA_ROOT"; return 0
  fi
  local dir="$SCRIPT_DIR"
  while :; do
    if [[ -f "$dir/AURA.sln" ]]; then printf '%s\n' "$dir"; return 0; fi
    [[ "$dir" == "/" ]] && break
    dir="$(dirname "$dir")"
  done
  return 1
}

AURA_ROOT="$(find_root)" || { echo "SMOKE TEST FALHOU: AURA.sln não encontrado"; exit 1; }

find_dll() {
  local dll="$AURA_ROOT/src/AURA.CLI/bin/Debug/net10.0/AURA.CLI.dll"
  [[ -f "$dll" ]] && { printf '%s\n' "$dll"; return 0; }
  dll="$AURA_ROOT/src/AURA.CLI/bin/Release/net10.0/AURA.CLI.dll"
  [[ -f "$dll" ]] && { printf '%s\n' "$dll"; return 0; }
  return 1
}

AURA_CLI_DLL="$(find_dll)" || { echo "SMOKE TEST FALHOU: CLI não compilado"; exit 1; }

cli() { printf '%s\n' "$1" | timeout 90 dotnet "$AURA_CLI_DLL"; }

# --- Arquivo de teste -------------------------------------------------------

TMP_FILE="$(mktemp /tmp/aura_smoke_XXXXXX.py)"
trap 'rm -f "$TMP_FILE"' EXIT

cat > "$TMP_FILE" <<'PY'
import time
print("AURA_SMOKE_OK", flush=True)
time.sleep(2)
PY

# --- Passos -----------------------------------------------------------------

OUT="$(cli 'diagnostico')"
if grep -q "Sistema operacional" <<< "$OUT"; then
  pass "diagnostico"
else
  fail "diagnostico não reportou o sistema"
fi

OUT="$(cli 'launchers')"
if grep -q "PythonLauncher" <<< "$OUT"; then
  pass "launchers"
else
  fail "launchers não mostra PythonLauncher"
fi

CELL_ID="smoke_$$"
RUN_OUT="$(cli "run $TMP_FILE --cell $CELL_ID")"
if grep -q "Célula criada e iniciada" <<< "$RUN_OUT"; then
  pass "run $CELL_ID"
else
  fail "run não criou a célula: $RUN_OUT"
fi

LOG_OUT="$(cli "cell log $CELL_ID")"
if grep -q "AURA_SMOKE_OK" <<< "$LOG_OUT"; then
  pass "log contém AURA_SMOKE_OK"
else
  fail "log sem AURA_SMOKE_OK: $LOG_OUT"
fi

cli "cell stop $CELL_ID" >/dev/null
cli "cell delete $CELL_ID" >/dev/null

CELLS_OUT="$(cli 'cells')"
if grep -q "$CELL_ID" <<< "$CELLS_OUT"; then
  fail "célula $CELL_ID ainda existe após delete"
else
  pass "delete removeu $CELL_ID"
fi

PERSIST_OUT="$(cli 'persist')"
if grep -q "Células persistidas" <<< "$PERSIST_OUT"; then
  pass "persist"
else
  fail "persist não confirmou"
fi

PLUGINS_OUT="$(cli 'plugins')"
if grep -q "Plugins" <<< "$PLUGINS_OUT"; then
  pass "plugins (com ou sem plugins carregados)"
else
  fail "plugins não respondeu"
fi

# --- Verdict ----------------------------------------------------------------

if [[ "$FAIL" == "1" ]]; then
  echo "SMOKE TEST FALHOU" >&2
  exit 1
fi

echo "SMOKE TEST OK"
exit 0
