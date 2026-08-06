#!/usr/bin/env bash
# AURA — commit + push rápido para o main.
# Uso:
#   bash scripts/git-push.sh "mensagem do commit"   (commit + pull --rebase + push)
#   bash scripts/git-push.sh                        (usa mensagem automática)
set -euo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

MSG="${1:-chore: atualizacoes $(date '+%Y-%m-%d %H:%M')}"

echo "=== status ==="
git status -sb

if [ -z "$(git status --porcelain)" ]; then
  echo "Nada para commitar; apenas sincronizando."
else
  echo "=== add + commit ==="
  git add -A
  git commit -m "$MSG"
fi

echo "=== pull --rebase ==="
git pull --rebase origin main

echo "=== push ==="
git push origin main

echo "=== pronto: $(git rev-parse --short HEAD) ==="
