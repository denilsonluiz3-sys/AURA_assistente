#!/data/data/com.termux/files/usr/bin/bash

set -u

ROOT="$HOME/AURA"
FILE="$ROOT/src/AURA.AI/AgentSession.cs"
STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP="$ROOT/.aura/memory-fix-$STAMP"

mkdir -p "$BACKUP"

echo "=========================================="
echo " AURA - CORREÇÃO DE MEMÓRIA DO AGENTE"
echo "=========================================="

cd "$ROOT" || exit 1

if [ ! -f "$FILE" ]; then
    echo "[ERRO] Arquivo não encontrado:"
    echo "$FILE"
    exit 1
fi

echo "[1] Backup..."
cp "$FILE" "$BACKUP/AgentSession.cs"

echo "[2] Estado atual..."
grep -nE 'MaxRounds|_messages|ChatToolsAsync' "$FILE" || true

python - "$FILE" <<'PY'
from pathlib import Path
import sys

p = Path(sys.argv[1])
s = p.read_text()

# Reduz o máximo de ciclos do agente.
s = s.replace(
    "private const int MaxRounds = 20;",
    "private const int MaxRounds = 8;"
)

# Adiciona limite de histórico imediatamente após a declaração da lista.
old = 'private readonly List<AgentMessage> _messages = new();'

new = '''private readonly List<AgentMessage> _messages = new();

        // Limite de segurança para impedir crescimento indefinido
        // do contexto enviado ao modelo.
        private const int MaxHistoryMessages = 16;'''

if old in s and "MaxHistoryMessages" not in s:
    s = s.replace(old, new)

# Insere método de poda antes de RunAsync.
marker = "        public async Task<string> RunAsync(string userText)"

method = '''        private void TrimHistory()
        {
            if (_messages.Count <= MaxHistoryMessages)
                return;

            // Preserva o system prompt, quando existir.
            AgentMessage? system = _messages
                .FirstOrDefault(m =>
                    string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase));

            var recent = _messages
                .Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase))
                .TakeLast(MaxHistoryMessages - (system != null ? 1 : 0))
                .ToList();

            _messages.Clear();

            if (system != null)
                _messages.Add(system);

            _messages.AddRange(recent);
        }

'''

if marker in s and "private void TrimHistory()" not in s:
    s = s.replace(marker, method + marker)

# Depois de cada adição relevante ao histórico, poda antes da próxima chamada.
target = '''                AgentChatResponse response = await _client.ChatToolsAsync(
                    _messages,'''

replacement = '''                TrimHistory();

                AgentChatResponse response = await _client.ChatToolsAsync(
                    _messages,'''

if target in s:
    s = s.replace(target, replacement)

p.write_text(s)
PY

echo "[3] Verificando alteração..."
grep -nE 'MaxRounds|MaxHistoryMessages|TrimHistory|ChatToolsAsync' "$FILE"

echo
echo "[4] Limpando build..."
dotnet clean -v:minimal

echo
echo "[5] Compilando..."
if dotnet build -v:minimal; then
    echo
    echo "=========================================="
    echo " CORREÇÃO APLICADA E BUILD OK"
    echo "=========================================="
    echo "Backup:"
    echo "$BACKUP"
    exit 0
else
    echo
    echo "=========================================="
    echo " BUILD FALHOU - RESTAURANDO"
    echo "=========================================="

    cp "$BACKUP/AgentSession.cs" "$FILE"

    echo "[OK] Arquivo restaurado."

    exit 1
fi
