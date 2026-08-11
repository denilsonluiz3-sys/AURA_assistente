#!/data/data/com.termux/files/usr/bin/bash
set -u

ROOT="$HOME/AURA"
FILE="$ROOT/src/AURA.AI/AgentSession.cs"
STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP="$ROOT/.aura/memory-fix-v2-$STAMP"

mkdir -p "$BACKUP"

echo "=========================================="
echo " AURA - CORREÇÃO DE MEMÓRIA v2 (marcador corrigido)"
echo "=========================================="

cd "$ROOT" || exit 1

if [ ! -f "$FILE" ]; then
    echo "[ERRO] Arquivo não encontrado: $FILE"
    exit 1
fi

echo "[1] Backup..."
cp "$FILE" "$BACKUP/AgentSession.cs"

python - "$FILE" <<'PY'
from pathlib import Path
import sys

p = Path(sys.argv[1])
s = p.read_text()
changed = []

# 1) Reduz o máximo de ciclos do agente.
old_rounds = "private const int MaxRounds = 20;"
new_rounds = "private const int MaxRounds = 8;"
if old_rounds in s:
    s = s.replace(old_rounds, new_rounds)
    changed.append("MaxRounds 20->8")
else:
    print("[AVISO] MaxRounds já alterado ou texto não encontrado")

# 2) Limite de histórico
old_field = "private readonly List<AgentMessage> _messages = new();"
new_field = old_field + "\n\n        // Limite de segurança para impedir crescimento indefinido\n        // do contexto enviado ao modelo.\n        private const int MaxHistoryMessages = 16;"
if old_field in s and "MaxHistoryMessages" not in s:
    s = s.replace(old_field, new_field)
    changed.append("MaxHistoryMessages adicionado")
else:
    print("[AVISO] MaxHistoryMessages já existe ou campo não encontrado")

# 3) Método TrimHistory - marcador CORRIGIDO para assinatura real (multi-linha)
marker = "        public async Task<string> RunAsync(string userText,\n            HttpClient? httpClient = null, CancellationToken ct = default)"

method = '''        private void TrimHistory()
        {
            if (_messages.Count <= MaxHistoryMessages)
                return;

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

            _logger.Info("agent: histórico podado para " + _messages.Count + " mensagens");
        }

'''

if marker in s and "private void TrimHistory()" not in s:
    s = s.replace(marker, method + marker)
    changed.append("TrimHistory() inserido (marcador corrigido)")
else:
    print("[ERRO CRÍTICO] Marcador do RunAsync não encontrado - abortando sem inserir chamada")
    sys.exit(2)

# 4) Chamada de TrimHistory antes de cada requisição ao modelo
target = '''                AgentChatResponse response = await _client.ChatToolsAsync(
                    _messages,'''

replacement = '''                TrimHistory();

                AgentChatResponse response = await _client.ChatToolsAsync(
                    _messages,'''

if target in s and "TrimHistory();" not in s.split(target)[0][-50:]:
    # só insere se ainda não tiver sido inserido nesse ponto
    if s.count("TrimHistory();") == 0:
        s = s.replace(target, replacement, 1)
        changed.append("Chamada TrimHistory() inserida antes do ChatToolsAsync")

# 5) Log estruturado do ciclo (P0.4 do relatório) - loga resposta bruta e decisão
old_log_point = 'if (response.ToolCalls is { Count: > 0 })'
new_log_point = '_logger.Info("agent: round=" + round + " toolCalls=" + (response.ToolCalls?.Count ?? 0) + " hasContent=" + !string.IsNullOrEmpty(response.Content));\n\n                if (response.ToolCalls is { Count: > 0 })'
if old_log_point in s and 'agent: round=' not in s:
    s = s.replace(old_log_point, new_log_point, 1)
    changed.append("Log estruturado do ciclo adicionado")

p.write_text(s)
print("MUDANÇAS APLICADAS: " + ", ".join(changed))
PY

PYTHON_STATUS=$?

if [ "$PYTHON_STATUS" -ne 0 ]; then
    echo
    echo "[ABORTADO] O patch Python encontrou um erro crítico e não alterou o arquivo por segurança."
    echo "Nada foi modificado. Restaurando por precaução..."
    cp "$BACKUP/AgentSession.cs" "$FILE"
    exit 1
fi

echo
echo "[2] Verificando alteração..."
grep -nE 'MaxRounds|MaxHistoryMessages|TrimHistory|agent: round=' "$FILE"

echo
echo "[3] Limpando build..."
dotnet clean -v:minimal > "$BACKUP/build-clean.log" 2>&1

echo
echo "[4] Compilando..."
if dotnet build -v:minimal > "$BACKUP/build-output.log" 2>&1; then
    echo
    echo "=========================================="
    echo " ✅ CORREÇÃO APLICADA E BUILD OK"
    echo "=========================================="
    echo "Backup: $BACKUP"
else
    echo
    echo "=========================================="
    echo " ❌ BUILD FALHOU - RESTAURANDO"
    echo "=========================================="
    echo "Log completo do erro salvo em: $BACKUP/build-output.log"
    echo
    echo "--- Últimas 30 linhas do erro ---"
    tail -30 "$BACKUP/build-output.log"
    echo
    cp "$BACKUP/AgentSession.cs" "$FILE"
    echo "[OK] Arquivo restaurado."
    exit 1
fi
