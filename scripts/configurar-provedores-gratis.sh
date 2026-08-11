#!/data/data/com.termux/files/usr/bin/bash

set -e

ROOT="$HOME/AURA"
cd "$ROOT"

CONFIG="$ROOT/config/providers.json"
STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP="$ROOT/.aura/backup-free-providers-$STAMP"

mkdir -p "$ROOT/config" "$BACKUP"

echo "=============================================="
echo " AURA — CONFIGURAÇÃO DE PROVEDORES GRATUITOS"
echo "=============================================="
echo

if [ -f "$CONFIG" ]; then
    cp "$CONFIG" "$BACKUP/providers.json.bak"
    echo "[OK] Backup: $BACKUP/providers.json.bak"
fi

cat > "$CONFIG" <<'JSON'
{
  "defaultProvider": "auto-free",
  "defaultModel": "auto",
  "selectionMode": "automatic",
  "fallbackEnabled": true,

  "providers": [

    {
      "id": "openrouter",
      "name": "OpenRouter Free",
      "baseUrl": "https://openrouter.ai/api/v1/chat/completions",
      "needsKey": true,
      "keyEnv": "OPENROUTER_API_KEY",
      "free": true,
      "priority": 10,
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
          "label": "Nemotron Nano",
          "category": "Grátis",
          "isFree": true
        }
      ]
    },

    {
      "id": "groq",
      "name": "Groq Free",
      "baseUrl": "https://api.groq.com/openai/v1/chat/completions",
      "needsKey": true,
      "keyEnv": "GROQ_API_KEY",
      "free": true,
      "priority": 20,
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
      "name": "Cerebras Free",
      "baseUrl": "https://api.cerebras.ai/v1/chat/completions",
      "needsKey": true,
      "keyEnv": "CEREBRAS_API_KEY",
      "free": true,
      "priority": 30,
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
      "name": "Google Gemini Free",
      "baseUrl": "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
      "needsKey": true,
      "keyEnv": "GEMINI_API_KEY",
      "free": true,
      "priority": 40,
      "models": [
        {
          "id": "gemini-2.5-flash",
          "label": "Gemini 2.5 Flash",
          "category": "Grátis",
          "isFree": true
        },
        {
          "id": "gemini-2.5-flash-lite",
          "label": "Gemini 2.5 Flash-Lite",
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
      "free": false,
      "priority": 100,
      "models": [
        {
          "id": "gpt-5-mini",
          "label": "GPT-5 mini",
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
      "name": "Ollama (opcional/local)",
      "baseUrl": "http://127.0.0.1:11434/v1/chat/completions",
      "needsKey": false,
      "keyEnv": "",
      "free": true,
      "optional": true,
      "priority": 200,
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
        },
        {
          "id": "qwen2.5",
          "label": "Qwen 2.5",
          "category": "Local",
          "isFree": true
        }
      ]
    }

  ]
}
JSON

echo "[OK] Catálogo atualizado."
echo

echo "===== CHAVES DISPONÍVEIS ====="

FOUND=0

check_key() {
    local NAME="$1"
    local ENV="$2"

    if [ -n "${!ENV:-}" ]; then
        echo "[OK] $NAME -> $ENV configurada"
        FOUND=$((FOUND + 1))
    else
        echo "[--] $NAME -> $ENV ausente"
    fi
}

check_key "OpenRouter" "OPENROUTER_API_KEY"
check_key "Groq" "GROQ_API_KEY"
check_key "Cerebras" "CEREBRAS_API_KEY"
check_key "Gemini" "GEMINI_API_KEY"
check_key "OpenAI" "OPENAI_API_KEY"

echo

if [ "$FOUND" -eq 0 ]; then
    echo "[AVISO] Nenhum provedor remoto gratuito possui chave configurada."
    echo
    echo "A AURA está preparada para:"
    echo "  OPENROUTER_API_KEY"
    echo "  GROQ_API_KEY"
    echo "  CEREBRAS_API_KEY"
    echo "  GEMINI_API_KEY"
else
    echo "[OK] $FOUND provedor(es) com credencial disponível."
fi

echo
echo "===== REGRAS ====="
echo "1. AURA tenta primeiro um provedor gratuito."
echo "2. O modelo pode ser escolhido pelo catálogo."
echo "3. Nenhuma chave é gravada no JSON."
echo "4. OPENAI_API_KEY continua disponível."
echo "5. Ollama continua disponível futuramente."
echo "6. AURA pode adicionar novos provedores pelo catálogo."
echo
echo "Backup:"
echo "  $BACKUP"
echo
echo "=============================================="
echo " CONCLUÍDO"
echo "=============================================="
