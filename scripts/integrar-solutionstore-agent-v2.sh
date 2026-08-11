#!/data/data/com.termux/files/usr/bin/bash

set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT" || exit 1

STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_DIR="$ROOT/.aura/backup-agent-solution-v2-$STAMP"
LOG="$ROOT/.aura/agent-solution-v2-$STAMP.log"

mkdir -p "$BACKUP_DIR"

exec > >(tee -a "$LOG") 2>&1

echo "=============================================="
echo " AURA - SolutionStore -> AgentSession v2"
echo "=============================================="

fail() {
    echo
    echo "[ERRO] $1"
    echo "Backup: $BACKUP_DIR"
    echo "Log:    $LOG"
    exit 1
}

AGENT="src/AURA.AI/AgentSession.cs"
PROJECT="src/AURA.AI/AURA.AI.csproj"

[ -f "$AGENT" ] || fail "AgentSession.cs não encontrado."
[ -f "$PROJECT" ] || fail "AURA.AI.csproj não encontrado."

echo
echo "[1/10] Verificando Git..."

git rev-parse --is-inside-work-tree >/dev/null 2>&1 \
    || fail "Repositório Git não encontrado."

BRANCH="$(git branch --show-current)"
echo "Branch: $BRANCH"

echo
echo "[2/10] Verificando estado..."

git status --short

echo
echo "[3/10] Backup..."

mkdir -p "$BACKUP_DIR/src/AURA.AI"

cp "$AGENT" "$BACKUP_DIR/$AGENT" \
    || fail "Falha no backup do AgentSession.cs"

cp "$PROJECT" "$BACKUP_DIR/$PROJECT" \
    || fail "Falha no backup do AURA.AI.csproj"

git diff > "$BACKUP_DIR/working-tree.patch" || true

echo "Backup criado em:"
echo "$BACKUP_DIR"

echo
echo "[4/10] Verificando build BASE..."

if ! dotnet build src/AURA.CLI/AURA.CLI.csproj --nologo; then
    fail "O projeto já falha antes da alteração."
fi

echo
echo "[5/10] Verificando referência AURA.Memory..."

if ! grep -q 'AURA.Memory/AURA.Memory.csproj' "$PROJECT"; then
    dotnet add "$PROJECT" reference \
        src/AURA.Memory/AURA.Memory.csproj \
        || fail "Não foi possível adicionar referência AURA.Memory."
else
    echo "[OK] Referência AURA.Memory já existe."
fi

echo
echo "[6/10] Aplicando alteração estrutural..."

python3 - "$AGENT" <<'PY'
from pathlib import Path
import sys

path = Path(sys.argv[1])
text = path.read_text()

original = text

def require_once(text, marker, description):
    count = text.count(marker)
    if count != 1:
        raise RuntimeError(
            f"{description}: esperado 1 ocorrência, encontrado {count}"
        )

# ---------------------------------------------------------
# 1. using AURA.Memory
# ---------------------------------------------------------

if "using AURA.Memory;" not in text:
    marker = "using AURA.Core.Logging;"
    require_once(
        text,
        marker,
        "marcador do using"
    )

    text = text.replace(
        marker,
        marker + "\nusing AURA.Memory;",
        1
    )
else:
    print("[OK] using AURA.Memory já existe.")

# ---------------------------------------------------------
# 2. Campo _solutionStore
# ---------------------------------------------------------

field = "        private readonly SolutionStore _solutionStore;\n"

if "_solutionStore" not in text:
    marker = "        private readonly string? _systemPrompt;\n"

    require_once(
        text,
        marker,
        "campo _systemPrompt"
    )

    text = text.replace(
        marker,
        marker + field,
        1
    )
else:
    print("[OK] campo _solutionStore já existe.")

# ---------------------------------------------------------
# 3. Inicialização no construtor
# ---------------------------------------------------------

init = "            _solutionStore = new SolutionStore();\n"

if "_solutionStore = new SolutionStore();" not in text:
    marker = (
        "            _logger = logger ?? new ConsoleLogger();\n"
    )

    require_once(
        text,
        marker,
        "final do construtor"
    )

    text = text.replace(
        marker,
        marker + init,
        1
    )
else:
    print("[OK] _solutionStore já é inicializado.")

# ---------------------------------------------------------
# 4. Método de consulta
# ---------------------------------------------------------

method_marker = "        private SolutionRule? TryGetKnownSolution("

if method_marker not in text:

    # Inserir imediatamente antes de ExecuteToolAsync.
    marker = (
        "        private async Task<string> ExecuteToolAsync(\n"
    )

    require_once(
        text,
        marker,
        "início de ExecuteToolAsync"
    )

    method = r'''        /// <summary>
        /// Consulta somente soluções que já foram validadas.
        /// A consulta não executa a solução e não substitui a IA.
        /// </summary>
        private SolutionRule? TryGetKnownSolution(
            RequestContext request)
        {
            if (request == null)
            {
                return null;
            }

            return _solutionStore.Find(
                request.Intent,
                request.Target,
                request.Goal);
        }

'''

    text = text.replace(
        marker,
        method + marker,
        1
    )
else:
    print("[OK] TryGetKnownSolution já existe.")

# ---------------------------------------------------------
# Segurança: cada elemento deve existir exatamente uma vez
# ---------------------------------------------------------

if text.count("using AURA.Memory;") != 1:
    raise RuntimeError("using AURA.Memory duplicado.")

if text.count("_solutionStore") < 2:
    raise RuntimeError(
        "_solutionStore não foi integrado corretamente."
    )

if text.count("TryGetKnownSolution") != 1:
    raise RuntimeError(
        "TryGetKnownSolution não foi integrado corretamente."
    )

if text == original:
    print("[INFO] Nenhuma alteração necessária.")
else:
    path.write_text(text)
    print("[OK] AgentSession.cs atualizado.")

PY

STATUS=$?

if [ "$STATUS" -ne 0 ]; then
    echo
    echo "[ERRO] A edição estrutural foi abortada."
    echo "[ROLLBACK] Restaurando arquivos..."

    cp "$BACKUP_DIR/$AGENT" "$AGENT"
    cp "$BACKUP_DIR/$PROJECT" "$PROJECT"

    exit 1
fi

echo
echo "[7/10] Inspeção da alteração..."

grep -nE \
'using AURA.Memory|_solutionStore|TryGetKnownSolution|ExecuteToolAsync' \
"$AGENT"

echo
echo "[8/10] BUILD FINAL..."

if ! dotnet build src/AURA.CLI/AURA.CLI.csproj --nologo; then
    echo
    echo "[ERRO] BUILD FINAL FALHOU."
    echo "[ROLLBACK] Restaurando..."

    cp "$BACKUP_DIR/$AGENT" "$AGENT"
    cp "$BACKUP_DIR/$PROJECT" "$PROJECT"

    echo "[ROLLBACK] Concluído."
    exit 1
fi

echo
echo "[OK] BUILD FINAL PASSOU."

echo
echo "[9/10] Validação Git..."

git diff --check \
    || {
        echo "[ERRO] git diff --check falhou."
        cp "$BACKUP_DIR/$AGENT" "$AGENT"
        cp "$BACKUP_DIR/$PROJECT" "$PROJECT"
        exit 1
    }

git status --short

echo
echo "[10/10] Commit e push..."

git add "$AGENT" "$PROJECT"

if git diff --cached --quiet; then
    echo "[INFO] Nenhuma alteração nova."
else
    git commit \
        -m "feat: conecta AgentSession ao conhecimento validado" \
        || fail "Commit falhou."
fi

git push origin HEAD \
    || fail "Push falhou."

echo
echo "=============================================="
echo " SUCESSO"
echo "=============================================="
echo
echo "SolutionStore conectado estruturalmente ao AgentSession."
echo
echo "Build: OK"
echo "Push: OK"
echo
echo "Backup:"
echo "$BACKUP_DIR"
echo
echo "Log:"
echo "$LOG"
echo
echo "Próxima etapa:"
echo "usar RequestContext para procurar soluções"
echo "ANTES do fallback para a IA."
echo "=============================================="
