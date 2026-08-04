#!/usr/bin/env bash
# AURA — migrar-ferramentas: copia aichat/jq para ~/bin (blindagem contra
# remontagem do proot, onde /data/data/com.termux some).
# Executar quando o mount do Termux estiver disponível.
# Idempotente: só copia se origem existir e destino não for idêntico.
set -uo pipefail

TERMUX=/data/data/com.termux/files
mkdir -p "$HOME/bin"

migrar() {
  local name="$1" src="$2" dst="$HOME/bin/$name"
  if [[ ! -e "$src" ]]; then
    printf '[skip]  %s origem não existe: %s\n' "$name" "$src"
    return 1
  fi
  if [[ -e "$dst" ]] && cmp -s "$src" "$dst"; then
    printf '[ok]    %s já em %s\n' "$name" "$dst"
    return 0
  fi
  if cp "$src" "$dst" && chmod +x "$dst"; then
    printf '[ok]    %s → %s\n' "$name" "$dst"
  else
    printf '[ERRO]  falha ao copiar %s\n' "$name"
    return 1
  fi
}

echo "== AURA migrar-ferramentas =="

# aichat: binário + deps do diretório usr/bin (pode ter libs ao lado)
if [[ -d "$TERMUX/usr/bin" ]]; then
  migrar aichat "$TERMUX/usr/bin/aichat"
else
  printf '[skip]  mount do Termux não disponível (%s)\n' "$TERMUX/usr/bin"
fi

migrar jq "$TERMUX/usr/bin/jq"

# valida os novos binários
echo "== validação =="
for b in aichat jq; do
  if [[ -x "$HOME/bin/$b" ]]; then
    printf '[ok]    %s versão: ' "$b"
    "$HOME/bin/$b" --version 2>&1 | head -1
  fi
done

echo "migrar-ferramentas: concluído"
