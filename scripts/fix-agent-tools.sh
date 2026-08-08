#!/data/data/com.termux/files/usr/bin/bash

set -u

ROOT="$HOME/AURA"
AI="$ROOT/src/AURA.AI"
BACKUP="$ROOT/.aura/backups/agent-tools-$(date +%Y%m%d_%H%M%S)"

echo "=========================================="
echo " AURA - FIX AGENT TOOLS / OLLAMA"
echo "=========================================="

cd "$ROOT" || exit 1

mkdir -p "$BACKUP"

echo "[1/6] Procurando ferramentas..."

find "$AI" -type f -name "*.cs" \
  \( -iname "*FileTool*.cs" -o \
     -iname "*Shell*.cs" -o \
     -iname "*Tool*.cs" \) \
  -print

echo
echo "[2/6] Criando backup..."

for f in \
  "$AI/AgentTools/FileTools.cs" \
  "$AI/AgentTools/ShellAgentTool.cs" \
  "$AI/AgentTool.cs" \
  "$AI/AgentSession.cs" \
  "$AI/OpenRouterClient.cs"
do
    if [ -f "$f" ]; then
        cp "$f" "$BACKUP/"
        echo "[OK] $(basename "$f")"
    fi
done

echo
echo "[3/6] Localizando definições..."

grep -R -n \
  "ListDirTool\|ReadFileTool\|WriteFileTool\|EditFileTool\|ShellAgentTool" \
  "$AI" --include="*.cs" 2>/dev/null || true

echo
echo "[4/6] Verificando workspace..."

WORKSPACE="$HOME/.aura/workspace"
mkdir -p "$WORKSPACE"

echo "Workspace:"
echo "$WORKSPACE"

echo
echo "Arquivos atuais:"
find "$WORKSPACE" -maxdepth 1 -type f -printf '%f\n' 2>/dev/null | sort

echo
echo "[5/6] Criando bateria de testes..."

TEST="$WORKSPACE/aura_agent_tools_test.py"

cat > "$TEST" <<'PY'
from pathlib import Path

workspace = Path.home() / ".aura" / "workspace"
arquivo = workspace / "teste_tools.txt"

print("=== AURA AGENT TOOLS TEST ===")
print("[1] workspace:", workspace)

# write_file
arquivo.write_text(
    "AURA Ollama funcionando 100%.\n",
    encoding="utf-8"
)
print("[2] write_file: OK")

# read_file
conteudo = arquivo.read_text(encoding="utf-8")
print("[3] read_file:", conteudo.strip())

# edit_file
conteudo = conteudo.replace(
    "AURA Ollama funcionando 100%.",
    "AURA Ollama EDIT funcionando 100%."
)

arquivo.write_text(conteudo, encoding="utf-8")
print("[4] edit_file: OK")

# verificar
final = arquivo.read_text(encoding="utf-8").strip()

if final == "AURA Ollama EDIT funcionando 100%.":
    print("[5] VERIFICAÇÃO: OK")
else:
    print("[5] VERIFICAÇÃO: FALHOU")

# list_dir
arquivos = sorted(p.name for p in workspace.iterdir() if p.is_file())

print("[6] list_dir:")
for nome in arquivos:
    print("   -", nome)

print("=== TESTE CONCLUÍDO ===")
PY

echo "[OK] Teste criado:"
echo "$TEST"

echo
echo "[6/6] Compilando AURA..."

PROJECT="$ROOT/src/AURA.CLI/AURA.CLI.csproj"

if [ ! -f "$PROJECT" ]; then
    echo "[ERRO] Projeto CLI não encontrado:"
    echo "$PROJECT"
    exit 1
fi

if dotnet build "$PROJECT" --no-restore; then
    echo
    echo "=========================================="
    echo " BUILD OK"
    echo "=========================================="
else
    echo
    echo "=========================================="
    echo " BUILD FALHOU"
    echo "=========================================="
    echo
    echo "Nenhuma alteração automática destrutiva foi feita."
    echo "Backup:"
    echo "$BACKUP"
    exit 1
fi

echo
echo "=========================================="
echo " PRÓXIMO TESTE"
echo "=========================================="

echo
echo "Inicie a AURA:"
echo
echo "dotnet run --project src/AURA.CLI/AURA.CLI.csproj"

echo
echo "Depois execute SOMENTE estes testes:"
echo
echo 'agent "Liste os arquivos do workspace usando list_dir."'
echo
echo 'agent "Crie teste_tools.txt contendo exatamente: AURA TOOL OK"'
echo
echo 'agent "Leia teste_tools.txt usando read_file."'
echo
echo 'agent "Altere teste_tools.txt para: AURA TOOL EDIT OK usando edit_file."'
echo
echo 'agent "Use run_shell para executar pwd."'
echo
echo "=========================================="
echo " BACKUP:"
echo "$BACKUP"
echo "=========================================="
