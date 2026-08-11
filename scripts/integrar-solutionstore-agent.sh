#!/data/data/com.termux/files/usr/bin/bash

set -u

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT" || exit 1

STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_DIR="$ROOT/.aura/backup-agent-solution-$STAMP"
LOG="$ROOT/.aura/agent-solution-$STAMP.log"

mkdir -p "$BACKUP_DIR"

exec > >(tee -a "$LOG") 2>&1

echo "=============================================="
echo " AURA - Integração SolutionStore → AgentSession"
echo "=============================================="

fail() {
    echo "[ERRO] $1"
    echo "Log: $LOG"
    exit 1
}

echo
echo "[1/9] Verificando Git..."

git rev-parse --is-inside-work-tree >/dev/null 2>&1 \
    || fail "Não estamos em um repositório Git."

BRANCH="$(git branch --show-current)"
echo "Branch: $BRANCH"

echo
echo "[2/9] Verificando estado..."

git status --short

echo
echo "[3/9] Fazendo backup..."

cp src/AURA.AI/AgentSession.cs \
   "$BACKUP_DIR/AgentSession.cs" \
   || fail "Não foi possível fazer backup do AgentSession.cs"

git diff > "$BACKUP_DIR/working-tree.patch" || true

echo "Backup: $BACKUP_DIR"

echo
echo "[4/9] Verificando dependência AURA.Memory..."

if ! grep -q 'AURA.Memory' src/AURA.AI/AURA.AI.csproj; then
    echo "[INFO] Adicionando referência AURA.Memory..."

    dotnet add src/AURA.AI/AURA.AI.csproj \
        reference src/AURA.Memory/AURA.Memory.csproj \
        || fail "Não foi possível adicionar referência AURA.Memory."
else
    echo "[OK] AURA.Memory já é referência do AURA.AI."
fi

echo
echo "[5/9] Criando integração..."

python3 - <<'PY'
from pathlib import Path

path = Path("src/AURA.AI/AgentSession.cs")
text = path.read_text()

if "using AURA.Memory;" not in text:
    marker = "using "
    lines = text.splitlines()

    insert_at = 0

    while insert_at < len(lines) and lines[insert_at].startswith("using "):
        insert_at += 1

    lines.insert(insert_at, "using AURA.Memory;")
    text = "\n".join(lines) + ("\n" if text.endswith("\n") else "")

# Campo do SolutionStore
if "_solutionStore" not in text:
    needle = "private readonly"
    pos = text.find(needle)

    if pos == -1:
        raise SystemExit(
            "Não foi possível localizar os campos privados do AgentSession."
        )

    # Insere antes do primeiro campo readonly.
    text = (
        text[:pos]
        + "private readonly SolutionStore _solutionStore;\n\n        "
        + text[pos:]
    )

# Inicialização do SolutionStore.
if "_solutionStore =" not in text:
    # Procura o primeiro construtor da classe.
    class_pos = text.find("public AgentSession")

    if class_pos == -1:
        raise SystemExit(
            "Não foi possível localizar o construtor de AgentSession."
        )

    brace = text.find("{", class_pos)

    if brace == -1:
        raise SystemExit(
            "Não foi possível localizar o corpo do construtor."
        )

    text = (
        text[:brace + 1]
        + "\n            _solutionStore = new SolutionStore();"
        + text[brace + 1:]
    )

# Adiciona método auxiliar antes do último fechamento da classe.
if "TryGetKnownSolution" not in text:
    method = r'''

        /// <summary>
        /// Procura uma solução operacional já validada.
        ///
        /// Esta etapa apenas consulta conhecimento conhecido.
        /// A execução e validação continuam separadas para que uma
        /// solução não seja considerada válida somente por ter sido
        /// encontrada.
        /// </summary>
        private SolutionRule? TryGetKnownSolution(
            RequestContext request)
        {
            if (request == null)
                return null;

            return _solutionStore.Find(
                request.Intent,
                request.Target,
                request.Goal);
        }
'''

    last = text.rfind("}")
    if last == -1:
        raise SystemExit("Classe AgentSession inválida.")

    text = text[:last] + method + "\n" + text[last:]

path.write_text(text)
PY

echo "[OK] Integração estrutural aplicada."

echo
echo "[6/9] Verificando código..."

git diff -- src/AURA.AI/AgentSession.cs
git diff -- src/AURA.AI/AURA.AI.csproj

echo
echo "[7/9] BUILD..."

PROJECT="src/AURA.CLI/AURA.CLI.csproj"

if [ -f "AURA.sln" ]; then
    PROJECT="AURA.sln"
elif [ -f "AURA.slnx" ]; then
    PROJECT="AURA.slnx"
fi

if ! dotnet build "$PROJECT" --nologo; then
    echo
    echo "[ERRO] BUILD FALHOU."
    echo "[ROLLBACK] Restaurando AgentSession.cs..."

    cp "$BACKUP_DIR/AgentSession.cs" \
       src/AURA.AI/AgentSession.cs

    # Remove eventual alteração automática do csproj.
    if git diff --quiet -- src/AURA.AI/AURA.AI.csproj; then
        :
    else
        git checkout -- src/AURA.AI/AURA.AI.csproj
    fi

    echo "[ROLLBACK] Concluído."
    echo "Backup: $BACKUP_DIR"

    exit 1
fi

echo
echo "[OK] BUILD PASSOU."

echo
echo "[8/9] Validação Git..."

git diff --check \
    || fail "git diff --check encontrou problemas."

git status --short

echo
echo "[9/9] Commit e push..."

git add \
    src/AURA.AI/AgentSession.cs \
    src/AURA.AI/AURA.AI.csproj

if git diff --cached --quiet; then
    echo "[INFO] Nenhuma alteração para commit."
else
    git commit \
        -m "feat: conecta conhecimento validado ao agente" \
        || fail "Commit falhou."
fi

git push origin HEAD \
    || fail "Push falhou. O commit permanece local."

echo
echo "=============================================="
echo " SUCESSO"
echo "=============================================="
echo
echo "AgentSession agora possui acesso ao SolutionStore."
echo
echo "IMPORTANTE:"
echo "Esta etapa NÃO executa automaticamente soluções."
echo "Ela somente prepara a consulta ao conhecimento validado."
echo
echo "Isso foi intencional."
echo
echo "Próxima fase:"
echo "RequestParser + executor + validação."
echo
echo "Backup: $BACKUP_DIR"
echo "Log:    $LOG"
echo "=============================================="
