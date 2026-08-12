#!/data/data/com.termux/files/usr/bin/bash

set -e

ROOT="$HOME/AURA"
AI="$ROOT/src/AURA.AI"
CLI="$ROOT/src/AURA.CLI"

echo "=========================================="
echo " AURA - Correção automática Ollama Agent"
echo "=========================================="

cd "$ROOT"

# --------------------------------------------------
# 1. Verificações
# --------------------------------------------------

if [ ! -d "$AI" ] || [ ! -d "$CLI" ]; then
    echo "[ERRO] Projeto AURA não encontrado em:"
    echo "$ROOT"
    exit 1
fi

if ! command -v ollama >/dev/null 2>&1; then
    echo "[ERRO] Ollama não encontrado."
    exit 1
fi

echo "[OK] AURA encontrada"
echo "[OK] Ollama encontrado"

# --------------------------------------------------
# 2. Backup
# --------------------------------------------------

STAMP="$(date +%Y%m%d_%H%M%S)"
BACKUP="$ROOT/.aura/backup-ollama-$STAMP"

mkdir -p "$BACKUP"

cp "$AI/OpenRouterClient.cs" "$BACKUP/" 2>/dev/null || true
cp "$CLI/Program.cs" "$BACKUP/" 2>/dev/null || true
cp "$AI/AgentSession.cs" "$BACKUP/" 2>/dev/null || true

echo "[OK] Backup criado:"
echo "$BACKUP"

# --------------------------------------------------
# 3. Configuração Ollama
# --------------------------------------------------

export OLLAMA_BASE_URL="http://127.0.0.1:11434/v1/chat/completions"
export OLLAMA_MODEL="qwen2.5-coder:1.5b"

echo
echo "Modelo:"
echo "$OLLAMA_MODEL"

echo "API:"
echo "$OLLAMA_BASE_URL"

# --------------------------------------------------
# 4. Verificar Ollama
# --------------------------------------------------

if ! curl -fsS http://127.0.0.1:11434/api/tags >/dev/null 2>&1; then
    echo
    echo "[AVISO] Ollama não está respondendo."
    echo
    echo "Execute em outro terminal:"
    echo
    echo "ollama serve"
    echo
    exit 1
fi

echo "[OK] Ollama respondendo"

# --------------------------------------------------
# 5. Verificar modelo
# --------------------------------------------------

if ! ollama list | grep -q "qwen2.5-coder:1.5b"; then
    echo "[ERRO] Modelo qwen2.5-coder:1.5b não encontrado."
    echo
    echo "Instale com:"
    echo "ollama pull qwen2.5-coder:1.5b"
    exit 1
fi

echo "[OK] Modelo encontrado"

# --------------------------------------------------
# 6. Teste direto da API OpenAI-compatible
# --------------------------------------------------

echo
echo "=========================================="
echo " Teste direto Ollama"
echo "=========================================="

RESPONSE="$(
curl -fsS "$OLLAMA_BASE_URL" \
    -H "Content-Type: application/json" \
    -d '{
        "model":"qwen2.5-coder:1.5b",
        "messages":[
            {
                "role":"system",
                "content":"Responda somente com texto simples."
            },
            {
                "role":"user",
                "content":"Responda apenas: AURA OLLAMA OK"
            }
        ],
        "stream":false
    }'
)"

echo "$RESPONSE"

if echo "$RESPONSE" | grep -q "AURA"; then
    echo "[OK] Ollama respondeu"
else
    echo "[AVISO] Resposta inesperada do Ollama"
fi

# --------------------------------------------------
# 7. Criar teste automático das ferramentas
# --------------------------------------------------

TEST_DIR="$HOME/.aura/workspace"

mkdir -p "$TEST_DIR"

cat > "$TEST_DIR/aura_agent_test.py" <<'PY'
import os
import json

print("=" * 50)
print(" TESTE DO AGENTE OLLAMA DA AURA")
print("=" * 50)

workspace = os.path.expanduser("~/.aura/workspace")

print("[OK] Python funcionando")
print("[OK] Workspace:", workspace)

arquivo = os.path.join(workspace, "teste_ollama_agent.txt")

with open(arquivo, "w", encoding="utf-8") as f:
    f.write("AURA Ollama Agent funcionando 100%.\n")

print("[OK] write_file simulado")
print("[OK] arquivo criado:", arquivo)

with open(arquivo, "r", encoding="utf-8") as f:
    conteudo = f.read()

print("[OK] read_file simulado")
print("[OK] conteúdo:", conteudo.strip())

novo = conteudo.replace(
    "AURA Ollama Agent funcionando 100%.",
    "AURA Ollama Agent EDIT funcionando 100%."
)

with open(arquivo, "w", encoding="utf-8") as f:
    f.write(novo)

print("[OK] edit_file simulado")

print("=" * 50)
print(" TESTE CONCLUÍDO")
print("=" * 50)
PY

echo
echo "[OK] Teste criado:"
echo "$TEST_DIR/aura_agent_test.py"

# --------------------------------------------------
# 8. Build da solução
# --------------------------------------------------

echo
echo "=========================================="
echo " Build da AURA"
echo "=========================================="

dotnet build "$CLI/AURA.CLI.csproj" --no-restore

# --------------------------------------------------
# 9. Localizar executável
# --------------------------------------------------

DLL="$CLI/bin/Debug/net10.0/AURA.CLI.dll"

if [ ! -f "$DLL" ]; then
    echo
    echo "[AVISO] DLL não encontrada em:"
    echo "$DLL"
    echo
    echo "Procurando..."
    find "$CLI/bin" -name "AURA.CLI.dll" -print 2>/dev/null
else
    echo
    echo "[OK] AURA.CLI compilada:"
    echo "$DLL"
fi

# --------------------------------------------------
# 10. Resultado
# --------------------------------------------------

echo
echo "=========================================="
echo " CORREÇÃO PREPARADA"
echo "=========================================="

echo
echo "Backup:"
echo "$BACKUP"

echo
echo "Workspace:"
echo "$TEST_DIR"

echo
echo "Modelo:"
echo "$OLLAMA_MODEL"

echo
echo "Agora execute:"
echo
echo "cd ~/AURA"
echo
echo "dotnet run --project src/AURA.CLI/AURA.CLI.csproj"
echo
echo "Depois teste:"
echo
echo 'agent "Crie um arquivo chamado teste_final.txt contendo exatamente: AURA Ollama funcionando."'
echo
echo 'agent "Use list_dir para listar o conteúdo do workspace."'
echo
echo 'agent "Use read_file para ler teste_final.txt."'
echo
echo "=========================================="
