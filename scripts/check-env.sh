#!/usr/bin/env bash
# AURA — check-env: valida as ferramentas essenciais após uma remontagem do proot.
# O mount /data/data/com.termux pode sumir; /root sobrevive.
# Uso: bash scripts/check-env.sh  (exit 0 se tudo OK, 1 se algo faltar)
set -uo pipefail

OK=0

check() {
  local name="$1" path="$2" hint="$3"
  if [[ -e "$path" ]]; then
    printf '[ok]   %-12s %s\n' "$name" "$path"
  else
    printf '[FALTA] %-12s %s\n' "$name" "$path"
    printf '       → %s\n' "$hint"
    OK=1
  fi
}

echo "== AURA check-env =="

# Ferramentas que vivem em /root (estáveis por construção)
check "argc" "$HOME/bin/argc" "Baixe o binário p/ ~/bin/argc (github.com/sigoden/argc)"
check "aichat-functions" "$HOME/.config/aichat/functions/functions.json" "Rode: cd ~/.config/aichat/functions && argc build"
check "aichat-config" "$HOME/.config/aichat/config.yaml" "Recrie ~/.config/aichat/config.yaml (model+clients)"

# Ferramentas que dependem do mount /data/data/com.termux (podem sumir)
# Mas podem ter sido migradas para ~/bin (blindadas) — checar lá primeiro.
TERMUX=/data/data/com.termux/files
check "aichat" "$HOME/bin/aichat" "Rode: bash scripts/migrar-ferramentas.sh (se o mount voltou)"
check "termux-ai" "$HOME/.local/share/termux-ai/config.json" "Reconfigure termux-ai (provider openai + api_url OpenRouter)"
check "jq" "$HOME/bin/jq" "Rode: bash scripts/migrar-ferramentas.sh (ou pkg install jq)"

# Ferramentas do sistema proot
check "dotnet" "$(command -v dotnet || echo /usr/bin/dotnet)" "apt install dotnet-sdk-10.0"
check "git" "$(command -v git || echo /usr/bin/git)" "apt install git"

echo "== GitHub remote =="
if git -C "${AURA_ROOT:-/root/AURA}" remote get-url origin >/dev/null 2>&1; then
  printf '[ok]   remote     %s\n' "$(git -C "${AURA_ROOT:-/root/AURA}" remote get-url origin)"
else
  printf '[FALTA] remote     repo git sem origin — use o clone em /tmp/opencode/aura_clone\n'
  OK=1
fi

if [[ "$OK" == "0" ]]; then
  echo "check-env: TUDO OK"
  exit 0
else
  echo "check-env: FALTAM FERRAMENTAS (veja acima)" >&2
  exit 1
fi
