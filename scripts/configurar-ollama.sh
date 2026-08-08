#!/data/data/com.termux/files/usr/bin/bash
set -e

ROOT="$HOME/AURA"

echo "=========================================="
echo " AURA — CONFIGURAÇÃO OLLAMA LOCAL"
echo "=========================================="

cd "$ROOT"

echo "[1/6] Verificando Ollama..."

if ! command -v ollama >/dev/null 2>&1; then
    echo "ERRO: Ollama não está instalado."
    exit 1
fi

echo "Ollama encontrado:"
ollama --version || true

echo
echo "[2/6] Verificando modelo..."

if ! ollama list | grep -q "qwen2.5-coder:1.5b"; then
    echo "Modelo qwen2.5-coder:1.5b não encontrado."
    echo "Instale com:"
    echo "  ollama pull qwen2.5-coder:1.5b"
    exit 1
fi

echo "Modelo OK."

echo
echo "[3/6] Criando backup..."

mkdir -p "$ROOT/.aura-backup-ollama"

cp src/AURA.CLI/Program.cs \
   "$ROOT/.aura-backup-ollama/Program.cs.bak"

cp src/AURA.AI/OpenRouterClient.cs \
   "$ROOT/.aura-backup-ollama/OpenRouterClient.cs.bak"

cp src/AURA.AI/AiAssistantService.cs \
   "$ROOT/.aura-backup-ollama/AiAssistantService.cs.bak"

echo "Backup criado em:"
echo "$ROOT/.aura-backup-ollama"

echo
echo "[4/6] Alterando configuração do cliente..."

python3 <<'PY'
from pathlib import Path

root = Path.home() / "AURA"

# -------------------------------------------------
# OpenRouterClient.cs
# -------------------------------------------------

p = root / "src/AURA.AI/OpenRouterClient.cs"
s = p.read_text()

s = s.replace(
    'public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";',
    'public string BaseUrl { get; set; } = "http://127.0.0.1:11434/v1/chat/completions";'
)

s = s.replace(
    'public string Model { get; set; } = "qwen/qwen-plus";',
    'public string Model { get; set; } = "qwen2.5-coder:1.5b";'
)

p.write_text(s)

# -------------------------------------------------
# Program.cs
# -------------------------------------------------

p = root / "src/AURA.CLI/Program.cs"
s = p.read_text()

# Padrão de modelo
s = s.replace(
    '"qwen/qwen-plus"',
    '"qwen2.5-coder:1.5b"'
)

# Variável de chave deixa de ser obrigatória para Ollama.
# Mantemos compatibilidade com código existente.
s = s.replace(
    'string apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty;',
    'string apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? "ollama";'
)

# Se encontrar arquivo antigo de chave, não queremos que ele seja
# necessário para o Ollama.
old = '''if (File.Exists(keyFile))
            {
                apiKey = File.ReadAllText(keyFile).Trim();
            }'''

new = '''if (File.Exists(keyFile))
            {
                var savedKey = File.ReadAllText(keyFile).Trim();

                // Ollama local não precisa de chave.
                // Só usa a chave antiga se ela parecer realmente uma
                // chave OpenRouter e o código estiver configurado para isso.
                if (savedKey.StartsWith("sk-or-", StringComparison.OrdinalIgnoreCase))
                {
                    apiKey = savedKey;
                }
            }'''

s = s.replace(old, new)

# Base URL dos novos clientes
s = s.replace(
    'AppReference = "CLI"',
    'BaseUrl = "http://127.0.0.1:11434/v1/chat/completions",\n                    AppReference = "AURA-Ollama"'
)

p.write_text(s)

# -------------------------------------------------
# AiAssistantService.cs
# -------------------------------------------------

p = root / "src/AURA.AI/AiAssistantService.cs"
s = p.read_text()

s = s.replace(
    'BaseUrl = "https://openrouter.ai/api/v1/chat/completions"',
    'BaseUrl = "http://127.0.0.1:11434/v1/chat/completions"'
)

s = s.replace(
    'Model = "qwen/qwen-plus"',
    'Model = "qwen2.5-coder:1.5b"'
)

s = s.replace(
    'Environment.GetEnvironmentVariable("AURA_OPENROUTER_KEY") ?? string.Empty',
    'Environment.GetEnvironmentVariable("AURA_OPENROUTER_KEY") ?? "ollama"'
)

p.write_text(s)

PY

echo "Configuração alterada."

echo
echo "[5/6] Limpando e compilando..."

dotnet clean src/AURA.CLI/AURA.CLI.csproj

dotnet build src/AURA.CLI/AURA.CLI.csproj

echo
echo "[6/6] Testando API Ollama..."

curl -sS \
  http://127.0.0.1:11434/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen2.5-coder:1.5b","messages":[{"role":"user","content":"Responda apenas: AURA Ollama funcionando"}],"stream":false}'

echo
echo
echo "=========================================="
echo " CONFIGURAÇÃO CONCLUÍDA"
echo "=========================================="
echo
echo "Modelo : qwen2.5-coder:1.5b"
echo "API    : http://127.0.0.1:11434/v1/chat/completions"
echo
echo "Agora execute:"
echo
echo "  dotnet run --project src/AURA.CLI/AURA.CLI.csproj"
echo
echo "E dentro da AURA:"
echo
echo '  chat "Responda apenas: AURA usando Ollama"'
echo
