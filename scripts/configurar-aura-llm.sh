#!/usr/bin/env bash
set -euo pipefail

AURA_DIR="${AURA_HOME:-$HOME/.aura}"
mkdir -p "$AURA_DIR"

# Persist provider/model for every future AURA shell session.
cat > "$AURA_DIR/llm.env" <<'ENV'
AURA_PROVIDER=openrouter
AURA_MODEL=nvidia/nemotron-3-ultra:free
ENV

chmod 600 "$AURA_DIR/llm.env"

echo "AURA LLM configuration saved to $AURA_DIR/llm.env"
echo "Provider: openrouter"
echo "Model: nvidia/nemotron-3-ultra:free"
