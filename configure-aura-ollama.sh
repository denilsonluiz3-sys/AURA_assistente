#!/bin/sh

set -eu

OLLAMA_URL="${OLLAMA_URL:-http://127.0.0.1:11435}"
OLLAMA_MODEL="${OLLAMA_MODEL:-aura-qwen}"

echo "=========================================="
echo " AURA — OLLAMA LOCAL"
echo "=========================================="
echo
echo "Endpoint: $OLLAMA_URL"
echo "Modelo:   $OLLAMA_MODEL"
echo

echo "[1/4] Testando API..."

if ! curl -fsS --max-time 10 "$OLLAMA_URL/api/version" >/dev/null; then
    echo "[ERRO] Ollama não respondeu."
    echo "Inicie o Ollama em $OLLAMA_URL"
    exit 1
fi

echo "[OK] API Ollama disponível."

echo
echo "[2/4] Verificando modelo..."

MODELS="$(curl -fsS --max-time 15 "$OLLAMA_URL/api/tags")"

echo "$MODELS" | grep -q "\"$OLLAMA_MODEL\"" || {
    echo "[ERRO] Modelo $OLLAMA_MODEL não encontrado."
    echo
    echo "$MODELS"
    exit 1
}

echo "[OK] Modelo encontrado."

echo
echo "[3/4] Testando geração..."

RESULT="$(
curl -fsS --max-time 60 \
    "$OLLAMA_URL/api/chat" \
    -H 'Content-Type: application/json' \
    -d "{
        \"model\":\"$OLLAMA_MODEL\",
        \"messages\":[
            {
                \"role\":\"system\",
                \"content\":\"Você é AURA, assistente local do projeto AURA.\"
            },
            {
                \"role\":\"user\",
                \"content\":\"Responda somente: AURA_OLLAMA_OK\"
            }
        ],
        \"stream\":false
    }"
)"

echo "$RESULT"

echo "$RESULT" | grep -q "AURA_OLLAMA_OK" || {
    echo "[ERRO] O modelo respondeu, mas o teste falhou."
    exit 1
}

echo
echo "[OK] Geração funcionando."

echo
echo "[4/4] Código configurado."

grep -n -A12 'Provider = "ollama"' \
    src/AURA.Mobile/MauiProgram.cs

echo
echo "=========================================="
echo " OLLAMA INTEGRADO À AURA"
echo "=========================================="
