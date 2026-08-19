#!/system/bin/sh
# AURA runtime smoke test for Android/Termux.
# Execute from AURA Terminal with: sh /path/to/aura_runtime_smoke_test.sh

PASS=0
WARN=0
FAIL=0
ROOT="${AURA_TEST_ROOT:-${TMPDIR:-/data/user/0/com.aura.genesis/files}/aura-smoke}"
REPORT="$ROOT/report.txt"

mkdir -p "$ROOT" 2>/dev/null || {
    echo "FAIL: não foi possível criar $ROOT"
    exit 1
}

log() {
    printf '%s\n' "$1" | tee -a "$REPORT"
}

ok() {
    PASS=$((PASS + 1))
    log "PASS: $1"
}

warn() {
    WARN=$((WARN + 1))
    log "WARN: $1"
}

fail() {
    FAIL=$((FAIL + 1))
    log "FAIL: $1"
}

: > "$REPORT"
log "============================================================"
log " AURA RUNTIME SMOKE TEST"
log "============================================================"
log "ROOT: $ROOT"
log ""

# 1. Shell
if [ -x /system/bin/sh ]; then
    ok "/system/bin/sh disponível"
elif [ -x /bin/sh ]; then
    ok "/bin/sh disponível"
else
    fail "nenhum /system/bin/sh ou /bin/sh disponível"
fi

# 2. Basic command execution
OUTPUT=$(printf 'AURA-SHELL-OK' 2>/dev/null)
if [ "$OUTPUT" = "AURA-SHELL-OK" ]; then
    ok "execução básica e captura de stdout"
else
    fail "execução básica do shell"
fi

# 3. stderr and exit code
ERR=$(sh -c 'printf AURA-STDERR 1>&2; exit 7' 2>&1)
CODE=$?
if [ "$CODE" -eq 7 ] && [ "$ERR" = "AURA-STDERR" ]; then
    ok "stderr e exit code"
else
    fail "stderr/exit code (code=$CODE output=$ERR)"
fi

# 4. Environment propagation
AURA_SMOKE="valor-ok"
export AURA_SMOKE
if [ "$(sh -c 'printf %s "$AURA_SMOKE"')" = "valor-ok" ]; then
    ok "variáveis de ambiente"
else
    fail "propagação de variável de ambiente"
fi

# 5. Working directory
mkdir -p "$ROOT/workdir" 2>/dev/null
if [ -d "$ROOT/workdir" ]; then
    PWD_OUT=$(cd "$ROOT/workdir" && pwd)
    case "$PWD_OUT" in
        "$ROOT/workdir"|"$ROOT/workdir"/*) ok "working directory" ;;
        *) fail "working directory incorreto: $PWD_OUT" ;;
    esac
else
    fail "criação do working directory"
fi

# 6. File write/read
printf 'AURA-FILE-OK\n' > "$ROOT/test.txt" 2>/dev/null
if [ "$(cat "$ROOT/test.txt" 2>/dev/null)" = "AURA-FILE-OK" ]; then
    ok "criação, escrita e leitura de arquivo"
else
    fail "I/O de arquivo"
fi

# 7. Temporary directory cleanup
mkdir -p "$ROOT/temporary" 2>/dev/null
printf 'temporary' > "$ROOT/temporary/item.txt" 2>/dev/null
if [ -f "$ROOT/temporary/item.txt" ]; then
    rm -rf "$ROOT/temporary"
    if [ ! -e "$ROOT/temporary" ]; then
        ok "limpeza de temporários"
    else
        fail "diretório temporário não foi removido"
    fi
else
    fail "criação do arquivo temporário"
fi

# 8. Process lifecycle
(sh -c 'sleep 1') &
PID=$!
if [ "$PID" -gt 0 ] 2>/dev/null; then
    wait "$PID" 2>/dev/null
    ok "criação e encerramento de processo"
else
    fail "criação de processo"
fi

# 9. Long input safety: the script itself must remain a file, not a FileName.
LONG_TEST="$ROOT/long-input.txt"
printf '%s\n' "$(printf 'AURA-%0400d' 0)" > "$LONG_TEST" 2>/dev/null
if [ -s "$LONG_TEST" ]; then
    ok "entrada longa armazenada como arquivo"
else
    fail "teste de entrada longa"
fi

# 10. Optional tools: report availability without failing Android smoke test.
for tool in git python python3 node dotnet; do
    if command -v "$tool" >/dev/null 2>&1; then
        ok "$tool disponível"
    else
        warn "$tool não disponível neste ambiente"
    fi
done

log ""
log "============================================================"
log " RESULTADO"
log "============================================================"
log "PASS : $PASS"
log "WARN : $WARN"
log "FAIL : $FAIL"
log "RELATÓRIO: $REPORT"

if [ "$FAIL" -eq 0 ]; then
    if [ "$WARN" -eq 0 ]; then
        log "STATUS: READY_FOR_RUNTIME_TEST"
    else
        log "STATUS: READY_WITH_WARNINGS"
    fi
else
    log "STATUS: NOT_READY"
fi

log "============================================================"
exit "$FAIL"
