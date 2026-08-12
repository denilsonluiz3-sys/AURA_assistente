#!/usr/bin/env bash
# Salva contexto atual da sessão no docs/contexto-sessao.json
# Uso: ./scripts/salvar-contexto.sh "descricao opcional do que foi feito"
# Esse script atualiza o arquivo de contexto com:
#  - ultima_atualizacao agora
#  - mensagem do commit ou descrição fornecida

set -euo pipefail

cd "$(dirname "$0")/.."

DESCRICAO="${1:-$(git log --oneline -1 2>/dev/null || echo 'atualizacao manual')}"
AGORA=$(date -Iseconds)

# Lê o arquivo atual ou cria template
if [ -f docs/contexto-sessao.json ]; then
  # Atualiza timestamp e descricao usando jq se disponível
  if command -v jq &>/dev/null; then
    jq --arg ts "$AGORA" \
       --arg desc "$DESCRICAO" \
       '.ultima_atualizacao = $ts | .ultima_acao = $desc' \
       docs/contexto-sessao.json > docs/contexto-sessao.json.tmp
    mv docs/contexto-sessao.json.tmp docs/contexto-sessao.json
    echo "✅ docs/contexto-sessao.json atualizado: $DESCRICAO"
  else
    # fallback: substitui a linha da data
    sed -i "s/\"ultima_atualizacao\": \".*\"/\"ultima_atualizacao\": \"$AGORA\"/" docs/contexto-sessao.json
    echo "⚠️  jq não encontrado. Só o timestamp foi atualizado."
  fi
else
  echo "❌ docs/contexto-sessao.json não encontrado."
  echo "   Crie-o primeiro com o conteúdo adequado."
  exit 1
fi