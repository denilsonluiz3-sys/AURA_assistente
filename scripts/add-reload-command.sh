#!/data/data/com.termux/files/usr/bin/bash
set -e

ROOT="$HOME/AURA"
FILE="$ROOT/src/AURA.CLI/Program.cs"
STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP="$ROOT/.aura/backup-reload-$STAMP"

mkdir -p "$BACKUP"

echo "=============================================="
echo " AURA — ADICIONAR COMANDO RELOAD"
echo "=============================================="

if [ ! -f "$FILE" ]; then
    echo "[ERRO] Program.cs não encontrado:"
    echo "$FILE"
    exit 1
fi

cp "$FILE" "$BACKUP/Program.cs.bak"
echo "[OK] Backup: $BACKUP/Program.cs.bak"

python3 - "$FILE" <<'PY'
import sys
from pathlib import Path

file = Path(sys.argv[1])
text = file.read_text()

# ---------------------------------------------------------
# 1. Localizar o tratamento de comandos
# ---------------------------------------------------------

if '"reload"' in text and 'Reload' in text:
    print("[INFO] Parece que reload já existe. Nenhuma alteração feita.")
    sys.exit(0)

# Procuramos o bloco de comandos onde normalmente ficam
# config / plugins / ajuda / exit.
markers = [
    'if (command == "config")',
    'if (cmd == "config")',
    'case "config":',
]

marker = None
for m in markers:
    if m in text:
        marker = m
        break

if marker is None:
    print("[ERRO] Não consegui localizar automaticamente o dispatcher de comandos.")
    print("Procure manualmente por: config, plugins, ajuda ou exit.")
    sys.exit(2)

# ---------------------------------------------------------
# 2. Criar método Reload
# ---------------------------------------------------------

method = r'''
        private static void ReloadCommand()
        {
            try
            {
                Console.WriteLine("[AURA] Recarregando configuração de provedores...");

                // Descarta o cliente atual.
                _aiClient = null;

                // ProviderRuntime.Load() lê novamente:
                // - config/providers.json
                // - seleção do provider/model
                // - chaves configuradas
                //
                // O novo cliente só será criado quando necessário.
                ProviderRuntime runtime = ProviderRuntime.Load();

                Console.WriteLine("[OK] Catálogo de provedores recarregado.");
                Console.WriteLine("[INFO] " + ProviderRuntime.Describe(runtime));

                Console.WriteLine("[OK] Cliente LLM será recriado na próxima chamada.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERRO] Falha ao recarregar provedores.");
                Console.WriteLine(ex.Message);
            }
        }

'''

# Inserir antes do primeiro método encontrado após uma classe.
# Usamos um ponto seguro: antes de EnsureAiClient.
ensure_marker = '        private static OpenRouterClient EnsureAiClient('

if ensure_marker in text:
    text = text.replace(ensure_marker, method + ensure_marker, 1)
else:
    print("[ERRO] EnsureAiClient não encontrado.")
    sys.exit(3)

# ---------------------------------------------------------
# 3. Adicionar comando ao dispatcher
# ---------------------------------------------------------

reload_block = r'''
            if (command == "reload")
            {
                ReloadCommand();
                return;
            }

'''

text = text.replace(marker, reload_block + marker, 1)

# ---------------------------------------------------------
# 4. Adicionar reload na ajuda
# ---------------------------------------------------------

help_candidates = [
    'Console.WriteLine("  config',
    'Console.WriteLine("  plugins',
]

help_marker = None
for h in help_candidates:
    if h in text:
        help_marker = h
        break

if help_marker:
    line_start = text.rfind('\n', 0, text.index(help_marker)) + 1
    text = text[:line_start] + \
           '            Console.WriteLine("  reload                 Recarrega provedores, chaves e cliente LLM");\n' + \
           text[line_start:]
else:
    print("[WARN] Não consegui adicionar reload à ajuda automaticamente.")

file.write_text(text)
print("[OK] Program.cs atualizado.")
PY

echo
echo "===== BUILD ====="

cd "$ROOT"

dotnet build src/AURA.CLI/AURA.CLI.csproj --no-restore

echo
echo "=============================================="
echo " CONCLUÍDO"
echo "=============================================="
echo
echo "Novo comando:"
echo
echo "  reload"
echo
echo "Ele recarrega o catálogo e força a criação de um"
echo "novo cliente LLM sem fechar a AURA."
echo
echo "Backup:"
echo "  $BACKUP"
