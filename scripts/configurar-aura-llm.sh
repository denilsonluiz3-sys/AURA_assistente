#!/usr/bin/env bash
set -euo pipefail

AURA_DIR="${AURA_HOME:-$HOME/.aura}"
CONFIG_FILE="$AURA_DIR/llm.env"
mkdir -p "$AURA_DIR"

provider="${AURA_PROVIDER:-openrouter}"
model="${1:-${AURA_MODEL:-}}"
base_url="${AURA_BASE_URL:-}"

if [[ -z "$model" ]]; then
  echo "Uso: $0 <modelo>"
  echo "Exemplo: $0 nvidia/nemotron-3-ultra:free"
  echo "Nenhum modelo padrão é imposto pelo AURA."
  exit 2
fi

if [[ "$provider" == "openrouter" && -z "$base_url" ]]; then
  base_url="https://openrouter.ai/api/v1/chat/completions"
fi

cat > "$CONFIG_FILE" <<ENV
AURA_PROVIDER=$provider
AURA_MODEL=$model
AURA_BASE_URL=$base_url
ENV

chmod 600 "$CONFIG_FILE"

echo "AURA LLM configuration saved to $CONFIG_FILE"
echo "Provider: $provider"
echo "Model: $model"
echo "Base URL: $base_url"
