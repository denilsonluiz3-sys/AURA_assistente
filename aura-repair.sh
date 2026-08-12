#!/data/data/com.termux/files/usr/bin/bash

ROOT="${AURA_ROOT:-$HOME/AURA}"
LOG="$ROOT/.aura/repair.log"
BACKUP="$ROOT/.aura/backup-$(date +%Y%m%d-%H%M%S)"

mkdir -p "$BACKUP" "$(dirname "$LOG")"

exec > >(tee -a "$LOG") 2>&1

echo "======================================"
echo " AURA SELF-REPAIR"
echo "======================================"

cd "$ROOT" || exit 1

echo
echo "[1] MEMORIA"
free -h

echo
echo "[2] OLLAMA"
if command -v ollama >/dev/null 2>&1; then
    ollama ps || true
else
    echo "Ollama não encontrado"
fi

echo
echo "[3] BACKUP"

for f in \
src/AURA.AI/AgentSession.cs \
src/AURA.AI/OpenRouterClient.cs \
src/AURA.CLI/Program.cs
do
    if [ -f "$f" ]; then
        mkdir -p "$BACKUP/$(dirname "$f")"
        cp "$f" "$BACKUP/$f"
        echo "Backup: $f"
    fi
done

echo
echo "[4] ANALISE DO AGENTE"

grep -RniE \
'MaxRounds|_messages|ChatToolsAsync|MaxTokens|num_ctx|context|tool_calls' \
src \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
> "$BACKUP/context-audit.txt" 2>&1 || true

cat "$BACKUP/context-audit.txt"

echo
echo "[5] LIMPEZA"

find . -type d \( -name bin -o -name obj \) \
-prune -exec rm -rf {} + 2>/dev/null || true

echo
echo "[6] BUILD"

dotnet build -v:minimal

RESULT=$?

echo
echo "[7] RESULTADO"

if [ "$RESULT" -eq 0 ]; then
    echo "BUILD: OK"
else
    echo "BUILD: FALHOU"
fi

echo
echo "Backup: $BACKUP"
echo "Log: $LOG"

exit "$RESULT"
