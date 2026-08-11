#!/data/data/com.termux/files/usr/bin/bash
set -e

cd "$(git rev-parse --show-toplevel)"

echo "=========================================="
echo " AURA — MEMÓRIA PERSISTENTE AUTOMÁTICA"
echo "=========================================="

mkdir -p .aura/diagnostico

AGENT="src/AURA.AI/AgentSession.cs"
STORE="src/AURA.Memory/SolutionStore.cs"

test -f "$AGENT"
test -f "$STORE"

echo
echo "[1/7] Estado atual..."
git status --short

echo
echo "[2/7] Verificando SolutionStore..."
grep -nE \
'Find\(|SaveValidated|solutions\.json|SolutionRule' \
"$STORE" || true

echo
echo "[3/7] Verificando integração AgentSession..."
grep -nE \
'TryGetKnownSolution|SolutionStore|ExecuteToolAsync|ChatToolsAsync|MaxRounds' \
"$AGENT" || true

echo
echo "[4/7] Verificando persistência..."
grep -nE \
'~/.aura/solutions\.json|memory\.json|File\.ReadAllText|File\.WriteAllText' \
src/AURA.Memory/*.cs || true

echo
echo "[5/7] Compilando..."
dotnet build AURA.sln --no-restore

echo
echo "[6/7] Procurando testes existentes..."
find . \
  -path './.git' -prune -o \
  -path './bin' -prune -o \
  -path './obj' -prune -o \
  -type f \
  \( -iname '*test*.cs' -o -iname '*agent*.py' \) \
  -print | head -100

echo
echo "[7/7] Gerando diagnóstico..."
OUT=".aura/diagnostico/memoria-automatica-$(date +%Y%m%d-%H%M%S).txt"

{
    echo "AURA — MEMÓRIA AUTOMÁTICA"
    echo "Data: $(date)"
    echo

    echo "===== GIT ====="
    git status --short
    echo

    echo "===== COMMITS RECENTES ====="
    git log -12 --oneline
    echo

    echo "===== AGENTSESSION ====="
    grep -nE \
    'MaxRounds|SolutionStore|TryGetKnownSolution|ChatToolsAsync|ToolCalls|ExecuteToolAsync|NormalizeToolArguments' \
    "$AGENT" || true
    echo

    echo "===== SOLUTION STORE ====="
    grep -nE \
    'class SolutionStore|Find\(|SaveValidated|solutions\.json|LoadLocked|PersistLocked' \
    "$STORE" || true
    echo

    echo "===== MEMORY STORE ====="
    grep -nE \
    'class MemoryStore|Append|Read|Clear|memory\.json|PersistLocked' \
    src/AURA.Memory/MemoryStore.cs || true
    echo

    echo "===== FERRAMENTAS ====="
    grep -RniE \
    'Name = "(list_dir|read_file|write_file|edit_file|run_shell)"' \
    src/AURA.AI \
    --include='*.cs' || true
    echo

    echo "===== BUILD ====="
    dotnet build AURA.sln --no-restore
} > "$OUT" 2>&1

echo
echo "DIAGNÓSTICO:"
echo "$OUT"

echo
echo "=========================================="
echo " MEMÓRIA AUTOMÁTICA VERIFICADA"
echo "=========================================="
