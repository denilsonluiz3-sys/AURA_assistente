#!/data/data/com.termux/files/usr/bin/bash
set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP="$ROOT/.aura/backup-free-providers-$STAMP"

mkdir -p "$BACKUP"

echo "=============================================="
echo " AURA — PROVEDORES GRATUITOS"
echo "=============================================="
echo

# ------------------------------------------------
# BACKUP
# ------------------------------------------------

if [ -f "$ROOT/config/providers.json" ]; then
    cp "$ROOT/config/providers.json" \
       "$BACKUP/providers.json.bak"
fi

echo "[OK] Backup:"
echo "     $BACKUP/providers.json.bak"

# ------------------------------------------------
# CATALOGO
# ------------------------------------------------

cat > "$ROOT/config/providers.json" <<'JSON'
{
  "defaultProvider": "openrouter",
  "defaultModel": "openrouter/free",

  "providers": [
    {
      "id": "openrouter",
      "name": "OpenRouter",
      "baseUrl": "https://openrouter.ai/api/v1/chat/completions",
      "needsKey": true,
      "keyEnv": "OPENROUTER_API_KEY",
      "keyHint": "OPENROUTER_API_KEY",
      "models": [
        {
          "id": "openrouter/free",
          "label": "Auto Free",
          "category": "Grátis",
          "isFree": true
        },
        {
          "id": "openai/gpt-oss-20b:free",
          "label": "GPT-OSS 20B",
          "category": "Grátis",
          "isFree": true
        },
        {
          "id": "google/gemma-4-26b-a4b-it:free",
          "label": "Gemma 4 26B",
          "category": "Grátis",
          "isFree": true
        },
        {
          "id": "nvidia/nemotron-3-nano-30b-a3b:free",
          "label": "Nemotron Nano 30B",
          "category": "Grátis",
          "isFree": true
        }
      ]
    },

    {
      "id": "groq",
      "name": "Groq",
      "baseUrl": "https://api.groq.com/openai/v1/chat/completions",
      "needsKey": true,
      "keyEnv": "GROQ_API_KEY",
      "keyHint": "GROQ_API_KEY",
      "models": [
        {
          "id": "llama-3.3-70b-versatile",
          "label": "Llama 3.3 70B",
          "category": "Grátis",
          "isFree": true
        },
        {
          "id": "llama-3.1-8b-instant",
          "label": "Llama 3.1 8B",
          "category": "Grátis",
          "isFree": true
        }
      ]
    },

    {
      "id": "cerebras",
      "name": "Cerebras",
      "baseUrl": "https://api.cerebras.ai/v1/chat/completions",
      "needsKey": true,
      "keyEnv": "CEREBRAS_API_KEY",
      "keyHint": "CEREBRAS_API_KEY",
      "models": [
        {
          "id": "llama-3.3-70b",
          "label": "Llama 3.3 70B",
          "category": "Grátis",
          "isFree": true
        },
        {
          "id": "llama-3.1-8b",
          "label": "Llama 3.1 8B",
          "category": "Grátis",
          "isFree": true
        }
      ]
    },

    {
      "id": "gemini",
      "name": "Google Gemini",
      "baseUrl": "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
      "needsKey": true,
      "keyEnv": "GEMINI_API_KEY",
      "keyHint": "GEMINI_API_KEY",
      "models": [
        {
          "id": "gemini-2.5-flash",
          "label": "Gemini 2.5 Flash",
          "category": "Grátis",
          "isFree": true
        }
      ]
    },

    {
      "id": "openai",
      "name": "OpenAI",
      "baseUrl": "https://api.openai.com/v1/chat/completions",
      "needsKey": true,
      "keyEnv": "OPENAI_API_KEY",
      "keyHint": "OPENAI_API_KEY",
      "models": [
        {
          "id": "gpt-5-mini",
          "label": "GPT-5 Mini",
          "category": "Pago",
          "isFree": false
        },
        {
          "id": "gpt-5",
          "label": "GPT-5",
          "category": "Pago",
          "isFree": false
        }
      ]
    },

    {
      "id": "ollama",
      "name": "Ollama (local/futuro)",
      "baseUrl": "http://127.0.0.1:11434/v1/chat/completions",
      "needsKey": false,
      "keyEnv": "",
      "keyHint": "não necessária",
      "models": [
        {
          "id": "qwen2.5-coder:1.5b",
          "label": "Qwen 2.5 Coder",
          "category": "Local",
          "isFree": true
        },
        {
          "id": "llama3.2",
          "label": "Llama 3.2",
          "category": "Local",
          "isFree": true
        }
      ]
    }
  ]
}
JSON

echo "[OK] config/providers.json atualizado."

# ------------------------------------------------
# VALIDA JSON
# ------------------------------------------------

echo
echo "===== VALIDANDO JSON ====="

python3 - "$ROOT/config/providers.json" <<'PY'
import json
import sys

path = sys.argv[1]

with open(path, "r", encoding="utf-8") as f:
    data = json.load(f)

providers = data.get("providers", [])

print(f"Providers: {len(providers)}")
print(f"Padrão: {data.get('defaultProvider')}")
print(f"Modelo padrão: {data.get('defaultModel')}")

for p in providers:
    print(
        f"- {p.get('name')} "
        f"[{p.get('id')}] "
        f"modelos={len(p.get('models', []))}"
    )
PY

# ------------------------------------------------
# GARANTE VARIAVEL OPENROUTER
# ------------------------------------------------

echo
echo "===== CONFIGURAÇÃO ====="

if [ -n "${OPENROUTER_API_KEY:-}" ]; then
    echo "[OK] OPENROUTER_API_KEY já configurada."
else
    echo "[AVISO] OPENROUTER_API_KEY não está configurada."
    echo
    echo "Para usar os modelos gratuitos do OpenRouter,"
    echo "configure a chave no ambiente do Termux."
fi

echo
echo "OpenAI continua disponível:"
printf 'OPENAI_API_KEY=%s\n' \
    "$([ -n "${OPENAI_API_KEY:-}" ] && echo CONFIGURADA || echo AUSENTE)"

# ------------------------------------------------
# BUILD
# ------------------------------------------------

echo
echo "===== BUILD ====="

dotnet build \
    "$ROOT/src/AURA.CLI/AURA.CLI.csproj" \
    --no-restore

echo
echo "=============================================="
echo " CONCLUÍDO"
echo "=============================================="
echo
echo "Padrão gratuito:"
echo "  OpenRouter"
echo "  openrouter/free"
echo
echo "OpenAI:"
echo "  continua disponível"
echo
echo "Ollama:"
echo "  mantido como opção futura"
echo
echo "Backup:"
echo "  $BACKUP"
echo
echo "Para selecionar manualmente:"
echo
echo '  export AURA_PROVIDER=openrouter'
echo '  export AURA_MODEL=openrouter/free'
echo
echo "Teste:"
echo
echo '  dotnet run --project src/AURA.CLI/AURA.CLI.csproj -- chat "Responda apenas: AURA GRATUITA OK"'
echo
