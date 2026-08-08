#!/data/data/com.termux/files/usr/bin/bash

set -e

cd ~/AURA

FILE="src/AURA.AI/AgentSession.cs"
BACKUP="${FILE}.bak-json-$(date +%Y%m%d-%H%M%S)"

echo "=========================================="
echo " AURA - FIX SYSTEM.TEXT.JSON"
echo "=========================================="

if [ ! -f "$FILE" ]; then
    echo "ERRO: $FILE não encontrado."
    exit 1
fi

echo
echo "[1/4] Backup..."
cp "$FILE" "$BACKUP"
echo "Backup: $BACKUP"

echo
echo "[2/4] Adicionando System.Text.Json..."

python3 <<'PY'
from pathlib import Path

file = Path("src/AURA.AI/AgentSession.cs")
text = file.read_text()

using_line = "using System.Text.Json;"

if using_line in text:
    print("System.Text.Json já está presente.")
else:
    marker = "using System.Threading.Tasks;"

    if marker not in text:
        raise SystemExit(
            "ERRO: não encontrei o bloco de using esperado."
        )

    text = text.replace(
        marker,
        marker + "\n" + using_line,
        1
    )

    file.write_text(text)
    print("OK: using System.Text.Json adicionado.")
PY

echo
echo "[3/4] Confirmando..."

head -n 15 "$FILE"

echo
echo "[4/4] Compilando AURA.AI..."

dotnet build \
    src/AURA.AI/AURA.AI.csproj \
    --no-restore \
    -v:minimal

echo
echo "=========================================="
echo " AURA.AI COMPILOU COM SUCESSO"
echo "=========================================="

echo
echo "Agora compilando AURA.CLI..."

dotnet build \
    src/AURA.CLI/AURA.CLI.csproj \
    --no-restore \
    -v:minimal

echo
echo "=========================================="
echo " AURA.CLI COMPILOU COM SUCESSO"
echo "=========================================="

echo
echo "Execute:"
echo
echo "cd ~/AURA"
echo "dotnet run --project src/AURA.CLI"
echo
echo "Depois teste:"
echo
echo 'agent "Liste os arquivos do workspace usando list_dir."'
echo
echo 'agent "Use run_shell para executar pwd."'
echo
echo 'agent "Crie teste_tools.txt contendo exatamente: AURA TOOL OK"'
echo
echo 'agent "Leia teste_tools.txt usando read_file."'
echo
echo 'agent "Altere teste_tools.txt de AURA TOOL OK para AURA TOOL EDIT OK usando edit_file."'
echo
echo 'agent "Leia teste_tools.txt usando read_file."'
echo
echo "=========================================="
