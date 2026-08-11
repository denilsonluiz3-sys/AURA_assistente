#!/data/data/com.termux/files/usr/bin/bash
set -e

ROOT="$HOME/AURA"
AI="$ROOT/src/AURA.AI"
CLI="$ROOT/src/AURA.CLI"
STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP="$ROOT/.aura/backup-provider-reload-$STAMP"

mkdir -p "$BACKUP"

cp "$AI/ProviderCatalog.cs" "$BACKUP/ProviderCatalog.cs.bak"
cp "$AI/ProviderRuntime.cs" "$BACKUP/ProviderRuntime.cs.bak"
cp "$CLI/Program.cs" "$BACKUP/Program.cs.bak"

echo "=============================================="
echo " AURA PROVIDER RELOAD"
echo "=============================================="
echo "Backup: $BACKUP"
echo

python3 - "$AI/ProviderCatalog.cs" "$AI/ProviderRuntime.cs" "$CLI/Program.cs" <<'PY'
import sys
from pathlib import Path

catalog_file = Path(sys.argv[1])
runtime_file = Path(sys.argv[2])
program_file = Path(sys.argv[3])

catalog = catalog_file.read_text()
runtime = runtime_file.read_text()
program = program_file.read_text()

# ============================================================
# 1. ProviderCatalog: adicionar Reload()
# ============================================================

if 'public static void Reload()' not in catalog:
    marker = '        public static List<ProviderInfo> Providers => ProvidersList;'

    if marker not in catalog:
        raise SystemExit(
            "ERRO: não encontrei a propriedade Providers em ProviderCatalog.cs"
        )

    replacement = '''        public static List<ProviderInfo> Providers => ProvidersList;

        /// <summary>
        /// Recarrega o catálogo de providers a partir de config/providers.json.
        /// </summary>
        public static void Reload()
        {
            ProvidersList.Clear();

            foreach (var provider in Load())
            {
                ProvidersList.Add(provider);
            }
        }'''

    catalog = catalog.replace(marker, replacement, 1)
    catalog_file.write_text(catalog)
    print("[OK] ProviderCatalog.Reload() adicionado.")
else:
    print("[OK] ProviderCatalog.Reload() já existe.")

# ============================================================
# 2. ProviderRuntime: adicionar Reload()
# ============================================================

if 'public static ProviderRuntime Reload()' not in runtime:
    marker = '        public static ProviderRuntime Load()'

    pos = runtime.find(marker)

    if pos < 0:
        raise SystemExit(
            "ERRO: método ProviderRuntime.Load() não encontrado."
        )

    # Encontrar o fim do método Load() contando chaves.
    brace = runtime.find('{', pos)

    if brace < 0:
        raise SystemExit(
            "ERRO: corpo de ProviderRuntime.Load() não encontrado."
        )

    depth = 0
    end = None

    for i in range(brace, len(runtime)):
        if runtime[i] == '{':
            depth += 1
        elif runtime[i] == '}':
            depth -= 1
            if depth == 0:
                end = i + 1
                break

    if end is None:
        raise SystemExit(
            "ERRO: não consegui determinar o fim de ProviderRuntime.Load()."
        )

    method = '''

        /// <summary>
        /// Recarrega o catálogo de providers e cria um novo runtime.
        /// </summary>
        public static ProviderRuntime Reload()
        {
            ProviderCatalog.Reload();
            return Load();
        }
'''

    runtime = runtime[:end] + method + runtime[end:]
    runtime_file.write_text(runtime)
    print("[OK] ProviderRuntime.Reload() adicionado.")
else:
    print("[OK] ProviderRuntime.Reload() já existe.")

# ============================================================
# 3. Program.cs: remover reload antigo incorreto
# ============================================================

bad_start = '            if (string.Equals(command, "reload", StringComparison.OrdinalIgnoreCase))'

while bad_start in program:
    start = program.find(bad_start)

    # Encontrar o fechamento do bloco pelo balanceamento.
    brace = program.find('{', start)

    if brace < 0:
        break

    depth = 0
    end = None

    for i in range(brace, len(program)):
        if program[i] == '{':
            depth += 1
        elif program[i] == '}':
            depth -= 1
            if depth == 0:
                end = i + 1
                break

    if end is None:
        break

    # Remove também o continue e quebra de linha.
    tail = program.find('\n', end)
    if tail >= 0:
        end = tail + 1

    program = program[:start] + program[end:]

# ============================================================
# 4. Program.cs: inserir reload no loop correto
# ============================================================

reload_block = '''            if (string.Equals(command, "reload", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ProviderRuntime.Reload();
                    _aiClient = null;

                    Console.WriteLine("[AURA] Catálogo de providers recarregado.");
                    Console.WriteLine("[AURA] Cliente LLM invalidado.");
                    Console.WriteLine("[AURA] Nova configuração será usada na próxima chamada.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ERRO] Falha ao recarregar providers: " + ex.Message);
                }

                continue;
            }

'''

if 'Catálogo de providers recarregado.' not in program:

    # Encontrar o loop principal.
    loop = program.find('while (true)')

    if loop < 0:
        raise SystemExit(
            "ERRO: while (true) do loop principal não encontrado."
        )

    # Encontrar onde command recebe a entrada.
    command_pos = program.find('command', loop)

    if command_pos < 0:
        raise SystemExit(
            "ERRO: variável command não encontrada dentro do loop."
        )

    # Procurar o primeiro despachante depois da leitura.
    candidates = []

    for token in [
        'switch (command)',
        'if (command ==',
        'if (string.Equals(command'
    ]:
        p = program.find(token, command_pos)
        if p >= 0:
            candidates.append(p)

    if not candidates:
        raise SystemExit(
            "ERRO: não encontrei o despachante de comandos."
        )

    insert = min(candidates)

    program = program[:insert] + reload_block + program[insert:]

    print("[OK] comando reload inserido no loop principal.")
else:
    print("[OK] comando reload já presente.")

# ============================================================
# 5. Ajuda
# ============================================================

if 'Recarrega providers, chaves e cliente LLM' not in program:
    p = program.find('  exit')

    if p >= 0:
        program = (
            program[:p]
            + '  reload                 Recarrega providers, chaves e cliente LLM\n'
            + program[p:]
        )
        print("[OK] reload adicionado à ajuda.")

program_file.write_text(program)

PY

echo
echo "===== COMPILAÇÃO ====="

cd "$ROOT"

dotnet build src/AURA.CLI/AURA.CLI.csproj --no-restore

echo
echo "===== VALIDAÇÃO ====="

grep -n -A15 -B3 \
    "Catálogo de providers recarregado" \
    src/AURA.CLI/Program.cs || true

echo
grep -n -A10 -B3 \
    "ProviderRuntime Reload" \
    src/AURA.AI/ProviderRuntime.cs || true

echo
grep -n -A10 -B3 \
    "static void Reload" \
    src/AURA.AI/ProviderCatalog.cs || true

echo
echo "=============================================="
echo " CONCLUÍDO"
echo "=============================================="
echo
echo "Backup:"
echo "$BACKUP"
echo
echo "Na AURA:"
echo
echo "  reload"
echo
echo "Depois:"
echo
echo '  chat "teste"'
echo
