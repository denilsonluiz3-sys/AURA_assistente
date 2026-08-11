#!/data/data/com.termux/files/usr/bin/bash
set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP="$ROOT/.aura/backup-provider-defaults-$STAMP"

mkdir -p "$BACKUP"

echo "=============================================="
echo " AURA — CORREÇÃO DOS DEFAULTS DE PROVEDOR"
echo "=============================================="

echo "[1/5] Criando backup..."

cp src/AURA.AI/OpenRouterClient.cs "$BACKUP/"
cp src/AURA.AI/AiAssistantService.cs "$BACKUP/"
cp src/AURA.CLI/Program.cs "$BACKUP/"

echo "[OK] Backup: $BACKUP"

echo
echo "[2/5] Corrigindo OpenRouterClient..."

python3 - <<'PY'
from pathlib import Path

p = Path("src/AURA.AI/OpenRouterClient.cs")
s = p.read_text()

s = s.replace(
    'public string BaseUrl { get; set; } = "http://127.0.0.1:11434/v1/chat/completions";',
    'public string BaseUrl { get; set; } = "https://api.openai.com/v1/chat/completions";'
)

s = s.replace(
    'public string Model { get; set; } = "qwen2.5-coder:1.5b";',
    'public string Model { get; set; } = "gpt-5-mini";'
)

p.write_text(s)
PY

echo "[OK] Defaults do cliente agora apontam para OpenAI."

echo
echo "[3/5] Corrigindo AiAssistantService..."

python3 - <<'PY'
from pathlib import Path

p = Path("src/AURA.AI/AiAssistantService.cs")
s = p.read_text()

s = s.replace(
    'Environment.GetEnvironmentVariable("AURA_OPENROUTER_KEY") ?? "ollama"',
    'Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty'
)

s = s.replace(
    'BaseUrl = "http://127.0.0.1:11434/v1/chat/completions"',
    'BaseUrl = "https://api.openai.com/v1/chat/completions"'
)

s = s.replace(
    'Model = "qwen2.5-coder:1.5b"',
    'Model = "gpt-5-mini"'
)

p.write_text(s)
PY

echo "[OK] AiAssistantService corrigido."

echo
echo "[4/5] Corrigindo seleção padrão da CLI..."

python3 - <<'PY'
from pathlib import Path

p = Path("src/AURA.CLI/Program.cs")
s = p.read_text()

# Somente muda o fallback.
# O bloco "if (provider == ollama)" permanece intacto.
s = s.replace(
    '?? "ollama";',
    '?? "openai";'
)

# No caminho OpenAI, remove o antigo fallback de modelo Ollama.
s = s.replace(
    '? "qwen2.5-coder:1.5b"\n                        : model,',
    '? "gpt-5-mini"\n                        : model,'
)

# Endpoint do caminho NÃO-Ollama.
# O bloco explícito do Ollama continua usando 127.0.0.1.
old = '''BaseUrl = "http://127.0.0.1:11434/v1/chat/completions",
                    AppReference = "AURA-Ollama"'''

new = '''BaseUrl = "https://api.openai.com/v1/chat/completions",
                    AppReference = "AURA"'''

s = s.replace(old, new)

p.write_text(s)
PY

echo "[OK] CLI corrigida."
echo "[OK] Ollama continua disponível somente quando explicitamente selecionado."

echo
echo "[5/5] Verificando referências..."

grep -RniE \
'ollama|11434|qwen2\.5-coder:1\.5b' \
src/AURA.AI src/AURA.CLI \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
|| true

echo
echo "===== BUILD ====="

dotnet build src/AURA.CLI/AURA.CLI.csproj --no-restore

echo
echo "=============================================="
echo " CONCLUÍDO"
echo "=============================================="
echo
echo "Padrão: OpenAI"
echo "Modelo padrão: gpt-5-mini"
echo "Chave: OPENAI_API_KEY"
echo
echo "Ollama NÃO foi removido do código."
echo "Para usá-lo futuramente:"
echo
echo '  export AURA_PROVIDER=ollama'
echo
echo "Backup:"
echo "  $BACKUP"
