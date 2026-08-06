#!/usr/bin/env bash
# AURA - Commit + Pull + Push

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if ! git rev-parse --git-dir >/dev/null 2>&1; then
    echo "Erro: este diretório não é um repositório Git."
    exit 1
fi

BRANCH=$(git branch --show-current)

MSG="${1:-chore: atualização automática $(date '+%Y-%m-%d %H:%M')}"

echo
echo "=== Branch: $BRANCH ==="
git status -sb
echo

if [[ -n "$(git status --porcelain)" ]]; then
    echo "=== Adicionando arquivos ==="
    git add -A

    echo "=== Commit ==="
    git commit -m "$MSG"
else
    echo "Nenhuma alteração encontrada."
fi

echo
echo "=== Sincronizando ==="
git pull --rebase origin "$BRANCH"

echo
echo "=== Enviando ==="
git push origin "$BRANCH"

echo
echo "✓ Concluído!"
echo "Commit: $(git rev-parse --short HEAD)"
