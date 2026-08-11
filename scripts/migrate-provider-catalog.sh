#!/data/data/com.termux/files/usr/bin/bash

set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIG_DIR="$ROOT/config"
CONFIG_FILE="$CONFIG_DIR/providers.json"
SOURCE="$ROOT/src/AURA.AI/ProviderCatalog.cs"

echo "========================================"
echo " AURA - MIGRAÇÃO DO PROVIDER CATALOG"
echo "========================================"
echo

mkdir -p "$CONFIG_DIR"

# Backup do catálogo atual
if [ -f "$SOURCE" ]; then
    BACKUP="$SOURCE.bak-provider-migration-$(date +%Y%m%d-%H%M%S)"
    cp "$SOURCE" "$BACKUP"
    echo "[OK] Backup:"
    echo "     ${BACKUP#$ROOT/}"
else
    echo "[AVISO] ProviderCatalog.cs não encontrado."
fi

# Não sobrescrever configuração existente sem backup
if [ -f "$CONFIG_FILE" ]; then
    BACKUP_JSON="$CONFIG_FILE.bak-$(date +%Y%m%d-%H%M%S)"
    cp "$CONFIG_FILE" "$BACKUP_JSON"
    echo "[OK] Backup do providers.json existente:"
    echo "     ${BACKUP_JSON#$ROOT/}"
fi

cat > "$CONFIG_FILE" <<'JSON'
{
  "$schemaVersion": 1,
  "description": "Catálogo de provedores e modelos da AURA. Credenciais ficam somente em variáveis de ambiente.",
  "providers": [
    {
      "id": "openai",
      "name": "OpenAI",
      "baseUrl": "https://api.openai.com/v1/chat/completions",
      "needsKey": true,
      "keyEnv": "OPENAI_API_KEY",
      "keyHint": "OPENAI_API_KEY",
      "models": [
        {
          "id": "gpt-5",
          "label": "GPT-5",
          "category": "Flagship",
          "isFree": false
        },
        {
          "id": "gpt-5-mini",
          "label": "GPT-5 Mini",
          "category": "Eficiente",
          "isFree": false
        }
      ]
    },
    {
      "id": "openrouter",
      "name": "OpenRouter",
      "baseUrl": "https://openrouter.ai/api/v1/chat/completions",
      "needsKey": true,
      "keyEnv": "OPENROUTER_API_KEY",
      "keyHint": "OPENROUTER_API_KEY",
      "models": [
        {
          "id": "qwen/qwen-plus",
          "label": "Qwen Plus",
          "category": "Razoável",
          "isFree": false
        },
        {
          "id": "qwen/qwen3.7-plus",
          "label": "Qwen 3.7 Plus",
          "category": "Flagship",
          "isFree": false
        },
        {
          "id": "qwen/qwen3.5-plus-20260420",
          "label": "Qwen 3.5 Plus",
          "category": "Flagship",
          "isFree": false
        },
        {
          "id": "openrouter/free",
          "label": "Auto (qualquer grátis)",
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
        },
        {
          "id": "poolside/laguna-s-2.1:free",
          "label": "Laguna S 2.1",
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
        },
        {
          "id": "llama-3.2-3b-preview",
          "label": "Llama 3.2 3B",
          "category": "Grátis",
          "isFree": true
        },
        {
          "id": "qwen-2.5-32b",
          "label": "Qwen 2.5 32B",
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
        },
        {
          "id": "gemini-2.5-pro",
          "label": "Gemini 2.5 Pro",
          "category": "Pago",
          "isFree": false
        }
      ]
    },
    {
      "id": "ollama",
      "name": "Ollama (local)",
      "baseUrl": "http://127.0.0.1:11434/v1/chat/completions",
      "needsKey": false,
      "keyEnv": "",
      "keyHint": "deixe vazio",
      "models": [
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
        },
        {
          "id": "mistral",
          "label": "Mistral",
          "category": "Local",
          "isFree": true
        },
        {
          "id": "qwen2.5-coder:1.5b",
          "label": "Qwen 2.5 Coder 1.5B",
          "category": "Local",
          "isFree": true
        }
      ]
    }
  ]
}
JSON

# Validar JSON
if command -v python >/dev/null 2>&1; then
    python - "$CONFIG_FILE" <<'PY'
import json
import sys

path = sys.argv[1]

with open(path, encoding="utf-8") as f:
    data = json.load(f)

assert isinstance(data.get("providers"), list)
assert len(data["providers"]) >= 1

for provider in data["providers"]:
    assert provider.get("id")
    assert provider.get("name")
    assert provider.get("baseUrl")
    assert isinstance(provider.get("models"), list)

print("[OK] JSON válido.")
print("[OK] Providers:", len(data["providers"]))
print("[OK] Modelos:", sum(len(p["models"]) for p in data["providers"]))
PY
else
    echo "[AVISO] Python não encontrado; JSON criado sem validação automática."
fi

# Proteção contra inclusão acidental de credenciais
if grep -qE '"(apiKey|token|secret|password)"[[:space:]]*:' "$CONFIG_FILE"; then
    echo
    echo "[ERRO] providers.json contém campo potencial de segredo."
    exit 1
fi

echo
echo "========================================"
echo " MIGRAÇÃO CONCLUÍDA"
echo "========================================"
echo
echo "Arquivo:"
echo "  ${CONFIG_FILE#$ROOT/}"
echo
echo "Providers:"
grep -E '^[[:space:]]*"id":' "$CONFIG_FILE" | sed 's/.*"id":[[:space:]]*"\([^"]*\)".*/  - \1/'
echo
echo "IMPORTANTE:"
echo "  providers.json NÃO contém API keys."
echo "  As credenciais continuam nas variáveis de ambiente."
echo
echo "Próximo passo:"
echo "  integrar ProviderCatalog.cs para carregar providers.json"
