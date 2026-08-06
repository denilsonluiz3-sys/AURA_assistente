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
TMP_NODE="$(mktemp /tmp/aura_smoke_XXXXXX.js)"
TMP_GO="$(mktemp /tmp/aura_smoke_XXXXXX.go)"
trap 'rm -f "$TMP_FILE" "$TMP_NODE" "$TMP_GO"' EXIT

cat > "$TMP_FILE" <<'PY'
import time
print("AURA_SMOKE_OK", flush=True)
time.sleep(2)
PY

cat > "$TMP_NODE" <<'JS'
console.log("AURA_SMOKE_NODE_OK");
JS

cat > "$TMP_GO" <<'GO'
package main

import "fmt"

func main() { fmt.Println("AURA_SMOKE_GO_OK") }
GO

# --- Passos -----------------------------------------------------------------

OUT="$(cli 'diagnostico')"
if grep -q "Sistema operacional" <<< "$OUT"; then
  pass "diagnostico"
else
  fail "diagnostico não reportou o sistema"
fi

OUT="$(cli 'launchers')"
if grep -q "PythonLauncher" <<< "$OUT" && grep -q "JavaLauncher" <<< "$OUT" && grep -q "NodeLauncher" <<< "$OUT" && grep -q "GoLauncher" <<< "$OUT"; then
  pass "launchers"
else
  fail "launchers não mostra todos os launchers"
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

# --- Célula Node.js (se node instalado) -------------------------------------

if command -v node >/dev/null 2>&1; then
  NODE_CELL_ID="smoke_node_$$"
  NODE_RUN_OUT="$(cli "run $TMP_NODE --cell $NODE_CELL_ID")"
  if grep -q "Célula criada e iniciada" <<< "$NODE_RUN_OUT"; then
    pass "run node $NODE_CELL_ID"
  else
    fail "run node não criou a célula: $NODE_RUN_OUT"
  fi

  NODE_LOG_OUT="$(cli "cell log $NODE_CELL_ID")"
  if grep -q "AURA_SMOKE_NODE_OK" <<< "$NODE_LOG_OUT"; then
    pass "log node contém AURA_SMOKE_NODE_OK"
  else
    fail "log node sem AURA_SMOKE_NODE_OK: $NODE_LOG_OUT"
  fi

  cli "cell stop $NODE_CELL_ID" >/dev/null
  cli "cell delete $NODE_CELL_ID" >/dev/null
else
  pass "node não instalado; teste de célula node pulado"
fi

# --- Célula Go (se go instalado) --------------------------------------------

if command -v go >/dev/null 2>&1; then
  GO_CELL_ID="smoke_go_$$"
  GO_RUN_OUT="$(cli "run $TMP_GO --cell $GO_CELL_ID")"
  if grep -q "Célula criada e iniciada" <<< "$GO_RUN_OUT"; then
    pass "run go $GO_CELL_ID"
  else
    fail "run go não criou a célula: $GO_RUN_OUT"
  fi

  GO_LOG_OUT="$(cli "cell log $GO_CELL_ID")"
  if grep -q "AURA_SMOKE_GO_OK" <<< "$GO_LOG_OUT"; then
    pass "log go contém AURA_SMOKE_GO_OK"
  else
    fail "log go sem AURA_SMOKE_GO_OK: $GO_LOG_OUT"
  fi

  cli "cell stop $GO_CELL_ID" >/dev/null
  cli "cell delete $GO_CELL_ID" >/dev/null
else
  pass "go não instalado; teste de célula go pulado"
fi

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
