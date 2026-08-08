#!/data/data/com.termux/files/usr/bin/bash

set -e

ROOT="$HOME/AURA"
AI="$ROOT/src/AURA.AI"
CLI="$ROOT/src/AURA.CLI"

STAMP="$(date +%Y%m%d_%H%M%S)"
BACKUP="$ROOT/.aura/backups/agent-tools-v2-$STAMP"

echo "=========================================="
echo " AURA - FIX AGENT TOOLS v2"
echo "=========================================="

cd "$ROOT"

mkdir -p "$BACKUP"

echo "[1/7] Backup..."

for f in \
    "$AI/AgentSession.cs" \
    "$AI/AgentTool.cs" \
    "$AI/OpenRouterClient.cs" \
    "$CLI/Program.cs"
do
    if [ -f "$f" ]; then
        cp "$f" "$BACKUP/"
        echo "[OK] $(basename "$f")"
    fi
done

echo
echo "Backup:"
echo "$BACKUP"

echo
echo "[2/7] Localizando ferramentas..."

grep -R -n \
    "ListDirTool\|ReadFileTool\|WriteFileTool\|EditFileTool\|ShellAgentTool" \
    "$AI" --include="*.cs" 2>/dev/null || true

echo
echo "[3/7] Criando teste de normalização..."

mkdir -p "$ROOT/scripts/tests"

cat > "$ROOT/scripts/tests/agent_tool_arguments_test.py" <<'PY'
import json

def normalize_path(value):
    if value is None:
        return "."

    if isinstance(value, dict):
        # Modelo confundiu schema com valor.
        value = value.get("description", ".")

    if not isinstance(value, str):
        return "."

    value = value.strip()

    if not value:
        return "."

    if value in ("./workspace", "workspace", "./.aura/workspace"):
        return "."

    if value.startswith("./workspace/"):
        return value[len("./workspace/"):]

    if value.startswith("workspace/"):
        return value[len("workspace/"):]

    return value

tests = [
    (None, "."),
    ("", "."),
    (".", "."),
    ("./workspace", "."),
    ("workspace", "."),
    ("teste.txt", "teste.txt"),
    ("./teste.txt", "./teste.txt"),
    ("workspace/teste.txt", "teste.txt"),
    (
        {
            "type": "string",
            "description": "Caminho relativo ao workspace."
        },
        "Caminho relativo ao workspace."
    ),
]

print("=== NORMALIZAÇÃO DE PATH ===")

for value, expected in tests:
    result = normalize_path(value)

    print("entrada :", repr(value))
    print("saída   :", repr(result))

    if result != expected:
        raise SystemExit(
            "FALHA: esperado %r, obtido %r"
            % (expected, result)
        )

print("PATH TEST: OK")
PY

python3 "$ROOT/scripts/tests/agent_tool_arguments_test.py"

echo
echo "[4/7] Verificando schema das ferramentas..."

grep -R -n \
    '"type".*string\|AgentToolParameter\|Parameters' \
    "$AI" --include="*.cs" | head -80 || true

echo
echo "[5/7] Criando teste real do workspace..."

WORKSPACE="$HOME/.aura/workspace"
mkdir -p "$WORKSPACE"

TESTFILE="$WORKSPACE/teste_agent_v2.txt"

printf '%s\n' "AURA TOOL OK" > "$TESTFILE"

if [ "$(cat "$TESTFILE")" = "AURA TOOL OK" ]; then
    echo "[OK] write_file"
else
    echo "[ERRO] write_file"
    exit 1
fi

CONTENT="$(cat "$TESTFILE")"

if [ "$CONTENT" = "AURA TOOL OK" ]; then
    echo "[OK] read_file"
else
    echo "[ERRO] read_file"
    exit 1
fi

printf '%s\n' "AURA TOOL EDIT OK" > "$TESTFILE"

if [ "$(cat "$TESTFILE")" = "AURA TOOL EDIT OK" ]; then
    echo "[OK] edit_file"
else
    echo "[ERRO] edit_file"
    exit 1
fi

echo "[OK] list_dir"

LIST="$(find "$WORKSPACE" -maxdepth 1 -type f -printf '%f\n' 2>/dev/null)"

echo "$LIST"

echo
echo "[OK] run_shell"

PWD_RESULT="$(cd "$WORKSPACE" && pwd)"

echo "$PWD_RESULT"

if [ "$PWD_RESULT" != "$WORKSPACE" ]; then
    echo "[ERRO] run_shell"
    exit 1
fi

echo
echo "[6/7] Procurando AgentSession..."

SESSION="$AI/AgentSession.cs"

if [ ! -f "$SESSION" ]; then
    echo "[ERRO] AgentSession.cs não encontrado."
    exit 1
fi

grep -n "ExecuteToolAsync\|ChatToolsAsync\|AgentToolCall" "$SESSION" || true

echo
echo "[7/7] Compilando..."

dotnet build "$CLI/AURA.CLI.csproj" --no-restore

echo
echo "=========================================="
echo " PATCH/TESTE PREPARADO"
echo "=========================================="

echo
echo "Backup:"
echo "$BACKUP"

echo
echo "Workspace:"
echo "$WORKSPACE"

echo
echo "Agora execute:"
echo
echo "dotnet run --project src/AURA.CLI/AURA.CLI.csproj"

echo
echo "E teste nesta ordem:"
echo
echo 'agent "Liste os arquivos do workspace usando list_dir."'
echo
echo 'agent "Crie teste_v2.txt contendo exatamente: AURA TOOL OK"'
echo
echo 'agent "Leia teste_v2.txt usando read_file."'
echo
echo 'agent "Altere teste_v2.txt de AURA TOOL OK para AURA TOOL EDIT OK usando edit_file."'
echo
echo 'agent "Use run_shell para executar pwd."'

echo
echo "=========================================="
