#!/data/data/com.termux/files/usr/bin/bash
set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP="$ROOT/.aura/backup-ai-provider-$STAMP"

mkdir -p "$BACKUP"

cp "$ROOT/src/AURA.CLI/Program.cs" "$BACKUP/Program.cs.bak"
cp "$ROOT/src/AURA.AI/ProviderCatalog.cs" "$BACKUP/ProviderCatalog.cs.bak"

echo "===== AURA — CORREÇÃO DE RESOLUÇÃO DE PROVEDOR ====="
echo "Backup: $BACKUP"
echo

python3 - "$ROOT/src/AURA.CLI/Program.cs" <<'PY'
from pathlib import Path
import sys

p = Path(sys.argv[1])
s = p.read_text()

# 1. Chave antiga -> chave OpenAI
s = s.replace(
    'Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")',
    'Environment.GetEnvironmentVariable("OPENAI_API_KEY")'
)

# 2. Mensagem antiga
s = s.replace(
    'Ou defina a variável OPENROUTER_API_KEY.',
    'Ou defina a variável OPENAI_API_KEY.'
)

# 3. Modelo padrão antigo
s = s.replace(
    '"qwen2.5-coder:1.5b"',
    '"gpt-5-mini"'
)

# 4. Endpoint antigo já migrado, mas garantimos o valor
s = s.replace(
    '"http://127.0.0.1:11434/v1/chat/completions"',
    '"https://api.openai.com/v1/chat/completions"'
)

# 5. AppReference antigo
s = s.replace(
    'AppReference = "AURA-Ollama"',
    'AppReference = "AURA"'
)

p.write_text(s)
PY

echo "[OK] Program.cs atualizado."

echo
echo "===== VERIFICAÇÃO ====="

grep -nE \
'OPENROUTER_API_KEY|OPENAI_API_KEY|gpt-5-mini|api.openai.com|AURA-Ollama|11434|qwen2\.5-coder' \
"$ROOT/src/AURA.CLI/Program.cs" || true

echo
echo "===== BUILD ====="

dotnet build "$ROOT/src/AURA.CLI/AURA.CLI.csproj" --no-restore

echo
echo "=============================================="
echo " CONCLUÍDO"
echo "=============================================="
echo
echo "Padrão: OpenAI"
echo "Modelo: gpt-5-mini"
echo "Chave: OPENAI_API_KEY"
echo
echo "Backup:"
echo "  $BACKUP"
