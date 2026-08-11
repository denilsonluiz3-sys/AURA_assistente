#!/data/data/com.termux/files/usr/bin/bash
set -e

ROOT="$HOME/AURA"
FILE="$ROOT/src/AURA.CLI/Program.cs"
STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP="$ROOT/.aura/backup-reload-$STAMP"

mkdir -p "$BACKUP"
cp "$FILE" "$BACKUP/Program.cs.bak"

echo "===== BACKUP ====="
echo "$BACKUP/Program.cs.bak"

echo
echo "===== VERIFICANDO ProviderRuntime ====="

grep -RniE \
'class ProviderRuntime|static.*ProviderRuntime|Reload\(|Load\(|CreateOptions' \
src/AURA.AI \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj \
|| true

echo
echo "===== FIM DA VERIFICAÇÃO ====="
echo
echo "O script NÃO vai alterar Program.cs ainda."
echo "Primeiro precisamos confirmar a API real do ProviderRuntime."
echo
echo "Backup criado em:"
echo "$BACKUP/Program.cs.bak"
