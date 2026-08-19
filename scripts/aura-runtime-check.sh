#!/system/bin/sh
# AURA Runtime Check — teste manual do ambiente real do app.
# Não altera o repositório nem exige ferramentas de desenvolvimento.

PASS=0
WARN=0
FAIL=0

ok()   { PASS=$((PASS+1)); echo "PASS: $1"; }
warn() { WARN=$((WARN+1)); echo "WARN: $1"; }
bad()  { FAIL=$((FAIL+1)); echo "FAIL: $1"; }

run() {
    name="$1"
    shift
    if "$@" >/dev/null 2>&1; then ok "$name"; else bad "$name"; fi
}

echo "=== AURA RUNTIME CHECK ==="
echo "PWD: $(pwd)"
echo

# Shell real usado pelo Android/Termux.
if [ -x /system/bin/sh ]; then ok "/system/bin/sh disponível"; else bad "/system/bin/sh"; fi
if [ -x /bin/sh ]; then ok "/bin/sh disponível"; else warn "/bin/sh ausente"; fi

# Execução, stdout, stderr e exit code.
out="$(printf 'AURA_STDOUT_OK')"
[ "$out" = "AURA_STDOUT_OK" ] && ok "stdout" || bad "stdout"
err="$(printf 'AURA_STDERR_OK' >&2 2>&1 >/dev/null)"
[ "$?" -eq 0 ] && ok "stderr" || bad "stderr"

sh -c 'exit 7' >/dev/null 2>&1
[ "$?" -eq 7 ] && ok "exit code" || bad "exit code"

# Ambiente e diretório de trabalho.
AURA_CHECK_ENV="OK"; export AURA_CHECK_ENV
[ "$AURA_CHECK_ENV" = "OK" ] && ok "variável de ambiente" || bad "variável de ambiente"
run "working directory atual" pwd

# I/O temporário dentro do sandbox do app.
tmp="${TMPDIR:-${HOME:-/data/local/tmp}}/aura_runtime_check_$$"
if mkdir -p "$tmp" && printf 'AURA_FILE_OK' > "$tmp/check.txt" && [ "$(cat "$tmp/check.txt")" = "AURA_FILE_OK" ]; then
    ok "criar/escrever/ler arquivo"
else
    bad "criar/escrever/ler arquivo"
fi
rm -rf "$tmp" 2>/dev/null
[ ! -e "$tmp" ] && ok "limpeza temporária" || bad "limpeza temporária"

# Processos básicos.
if sh -c 'sleep 1' >/dev/null 2>&1; then ok "iniciar processo"; else bad "iniciar processo"; fi

# Ferramentas opcionais: ausência não significa falha do AURA.
command -v git >/dev/null 2>&1 && ok "git disponível" || warn "git não disponível"
command -v python >/dev/null 2>&1 && ok "python disponível" || warn "python não disponível"
command -v python3 >/dev/null 2>&1 && ok "python3 disponível" || warn "python3 não disponível"
command -v node >/dev/null 2>&1 && ok "node disponível" || warn "node não disponível"
command -v dotnet >/dev/null 2>&1 && ok ".NET disponível" || warn ".NET não disponível"

# Resultado.
echo
echo "=== RESULTADO ==="
echo "PASS=$PASS WARN=$WARN FAIL=$FAIL"
if [ "$FAIL" -eq 0 ]; then
    [ "$WARN" -eq 0 ] && echo "STATUS=READY" || echo "STATUS=READY_WITH_WARNINGS"
    exit 0
fi
echo "STATUS=NOT_READY"
exit 1
