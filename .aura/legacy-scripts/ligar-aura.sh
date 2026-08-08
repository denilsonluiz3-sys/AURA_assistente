#!/data/data/com.termux/files/usr/bin/bash

set -e

cd "$HOME/AURA"

PROGRAM="src/AURA.CLI/Program.cs"

if [ ! -f "$PROGRAM" ]; then
    echo "[ERRO] $PROGRAM não encontrado."
    exit 1
fi

cp "$PROGRAM" "$PROGRAM.bak.aura"

echo "[OK] Backup criado:"
echo "$PROGRAM.bak.aura"

python3 - "$PROGRAM" <<'PY'
import sys

path = sys.argv[1]

with open(path, "r", encoding="utf-8") as f:
    s = f.read()

# Procura a chamada atual do AgentSession.
patterns = [
    ".RunAsync(userText, httpClient, ct)",
    ".RunAsync(userText, httpClient, cancellationToken)",
]

replacement = ".RunAsync(userText, httpClient, ct, LoadAuraMasterPrompt())"

changed = False

for old in patterns:
    if old in s:
        s = s.replace(old, replacement, 1)
        changed = True
        print("[OK] AgentSession conectado ao prompt mestre.")
        break

# Adiciona o carregador do prompt se ainda não existir.
if "LoadAuraMasterPrompt()" not in s:

    marker = "private static void AgentCommand"

    if marker not in s:
        print("[ERRO] AgentCommand não encontrado.")
        print("[ERRO] Nenhuma alteração adicional foi feita.")
        sys.exit(1)

    method = r'''
private static string LoadAuraMasterPrompt()
{
    string promptFile = Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile),
        ".aura",
        "aura_master_prompt.txt");

    if (File.Exists(promptFile))
    {
        return File.ReadAllText(promptFile);
    }

    return
        "Você é Aura, o agente local da AURA. " +
        "Use somente as ferramentas disponíveis.";
}

'''

    s = s.replace(marker, method + marker, 1)

    # Tenta novamente a chamada.
    for old in patterns:
        if old in s:
            s = s.replace(old, replacement, 1)
            changed = True
            print("[OK] Chamada RunAsync atualizada.")
            break

if not changed:
    print("[AVISO] Não foi encontrada a chamada esperada de RunAsync.")
    print("[AVISO] Nenhuma alteração automática na chamada foi realizada.")

# Nome visual do agente.
s = s.replace(
    "Executando agente...",
    "Executando Aura..."
)

s = s.replace(
    "=== RESPOSTA DO AGENTE ===",
    "=== RESPOSTA DA AURA ==="
)

with open(path, "w", encoding="utf-8") as f:
    f.write(s)

print("[OK] Program.cs salvo.")
PY

echo
echo "=== VERIFICAÇÃO ==="

grep -n \
    -E "LoadAuraMasterPrompt|RunAsync\(userText|Executando Aura|RESPOSTA DA AURA" \
    "$PROGRAM" || true

echo
echo "=== COMPILANDO AURA.AI ==="

dotnet build src/AURA.AI/AURA.AI.csproj

echo
echo "=== COMPILANDO AURA.CLI ==="

dotnet build src/AURA.CLI/AURA.CLI.csproj

echo
echo "======================================"
echo " AURA BUILD CONCLUÍDO"
echo "======================================"
