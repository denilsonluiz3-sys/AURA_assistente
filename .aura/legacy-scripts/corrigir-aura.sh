#!/data/data/com.termux/files/usr/bin/bash
set -e

ROOT="$HOME/AURA"
PROGRAM="$ROOT/src/AURA.CLI/Program.cs"
PROMPT="$HOME/.aura/aura_master_prompt.txt"
BACKUP="$PROGRAM.bak-aura-final"

echo "=== CONFIGURANDO AURA ==="

if [ ! -f "$PROGRAM" ]; then
    echo "[ERRO] Não encontrei:"
    echo "$PROGRAM"
    exit 1
fi

mkdir -p "$HOME/.aura/workspace"

cp "$PROGRAM" "$BACKUP"
echo "[OK] Backup criado:"
echo "$BACKUP"

cat > "$PROMPT" <<PROMPT
Você é Aura, o agente operacional local da AURA.

IDENTIDADE:
- Seu nome é Aura.
- Você é o agente local da AURA.
- Trabalhe diretamente no workspace.
- Responda em português.

WORKSPACE:
$HOME/.aura/workspace

FERRAMENTAS DISPONÍVEIS:

list_dir
Argumentos obrigatórios:
{"path":"."}

Para listar a raiz use exatamente:
{"path":"."}

read_file
Argumentos:
{"path":"arquivo.txt"}

write_file
Argumentos:
{"path":"arquivo.txt","content":"conteúdo"}

edit_file
Argumentos:
{"path":"arquivo.txt","old_text":"texto antigo","new_text":"texto novo"}

run_shell
Argumentos:
{"command":"pwd"}

REGRAS ABSOLUTAS:

1. Todos os paths são relativos ao workspace.
2. Nunca coloque "workspace/" no início do path.
3. Nunca use path vazio.
4. Nunca transforme um parâmetro string em objeto.
5. Nunca invente parâmetros.
6. Nunca invente ferramentas.
7. list_dir deve receber um objeto com path string.
8. read_file deve receber um objeto com path string.
9. write_file deve receber path e content como strings.
10. edit_file deve receber path, old_text e new_text como strings.
11. old_text nunca pode ser vazio.
12. run_shell deve receber command como string.
13. Nunca use:
{"path":{"type":"string","description":"..."}}
14. Nunca use:
{"command":{"type":"string","description":"..."}}
15. Gere JSON válido.
16. Não gere schemas de ferramentas.
17. Não gere markdown no lugar de chamadas de ferramentas.
18. Não repita uma ferramenta sem necessidade.
19. Depois de executar a tarefa, informe o resultado.

EXEMPLOS:

Usuário: liste os arquivos

list_dir:
{"path":"."}

Usuário: crie teste.txt

write_file:
{"path":"teste.txt","content":"AURA OK"}

Usuário: leia teste.txt

read_file:
{"path":"teste.txt"}

Usuário: execute pwd

run_shell:
{"command":"pwd"}

Você é Aura.
Execute as tarefas usando as ferramentas.
PROMPT

echo "[OK] Prompt mestre:"
echo "$PROMPT"

python3 - "$PROGRAM" <<'PY'
from pathlib import Path
import sys

p = Path(sys.argv[1])
s = p.read_text()

# agent -> aura, mantendo agent como alias
old = '''case "agent":
                        AgentCommand(parts);
                        break;'''

new = '''case "aura":
                    case "agent":
                        AuraCommand(parts);
                        break;'''

if old in s:
    s = s.replace(old, new)
else:
    print("[INFO] Dispatcher já pode ter sido alterado.")

# Renomear método
s = s.replace(
    "private static void AgentCommand(string[] parts)",
    "private static void AuraCommand(string[] parts)"
)

# Substituir prompt antigo
start = s.find("string systemPrompt =")
if start >= 0:
    end = s.find(";", start)
    if end >= 0:
        novo = '''string systemPrompt = LoadAuraMasterPrompt()
                .Replace("__WORKSPACE__", workspace)'''
        s = s[:start] + novo + s[end+1:]

# Corrigir mensagens
s = s.replace(
    'Uso: agent \\"instrução\\"',
    'Uso: aura \\"instrução\\"'
)

p.write_text(s)
PY

echo "[OK] Program.cs atualizado."

echo
echo "=== DISPATCHER ==="
grep -n -A5 -B3 'case "aura"' "$PROGRAM" || true

echo
echo "=== AURACOMMAND ==="
grep -n 'AuraCommand' "$PROGRAM" || true

echo
echo "=== BUILD AURA.AI ==="
dotnet build src/AURA.AI/AURA.AI.csproj --no-restore

echo
echo "=== BUILD AURA.CLI ==="
dotnet build src/AURA.CLI/AURA.CLI.csproj --no-restore

echo
echo "================================"
echo " AURA CONFIGURADA"
echo "================================"
echo
echo "Teste:"
echo 'dotnet run --project src/AURA.CLI -- aura "Use list_dir para listar os arquivos do workspace."'
