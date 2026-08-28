#!/bin/sh
set -eu

echo "=========================================="
echo " AURA — VALIDAÇÃO OLLAMA NO AGENTE"
echo "=========================================="

echo
echo "[1/6] Git"
git status --short

echo
echo "[2/6] Configuração do agente"
grep -n -A12 'Provider = "ollama"' \
    src/AURA.Mobile/MauiProgram.cs

echo
echo "[3/6] AgentSession"
grep -n -B3 -A5 'new AgentSession' \
    src/AURA.Mobile/Pages/AgentPage.xaml.cs

echo
echo "[4/6] Ollama"
curl -fsS --max-time 10 \
    http://127.0.0.1:11435/api/version

echo
echo
curl -fsS --max-time 15 \
    http://127.0.0.1:11435/api/tags

echo
echo
echo "[5/6] Teste aura-qwen"
curl -fsS --max-time 60 \
    http://127.0.0.1:11435/api/chat \
    -H 'Content-Type: application/json' \
    -d '{
      "model":"aura-qwen",
      "messages":[
        {
          "role":"system",
          "content":"Você é AURA. Responda de forma curta."
        },
        {
          "role":"user",
          "content":"Responda somente AURA_OLLAMA_OK"
        }
      ],
      "stream":false
    }'

echo
echo
echo "[6/6] Referências críticas"

grep -RniE \
'OpenRouterClient|Provider = "ollama"|11435|aura-qwen|new AgentSession' \
src/AURA.AI src/AURA.Mobile \
--exclude-dir=bin \
--exclude-dir=obj \
2>/dev/null || true

echo
echo "=========================================="
echo " VALIDAÇÃO CONCLUÍDA"
echo "=========================================="
