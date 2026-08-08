#!/data/data/com.termux/files/usr/bin/bash
set -e

ROOT="$HOME/AURA"
AI="$ROOT/src/AURA.AI/OpenRouterClient.cs"
CLI="$ROOT/src/AURA.CLI/Program.cs"

echo "======================================"
echo " AURA - CONFIGURAR OLLAMA LOCAL"
echo "======================================"

cd "$ROOT"

echo "[1/6] Verificando arquivos..."

[ -f "$AI" ] || { echo "ERRO: $AI não encontrado"; exit 1; }
[ -f "$CLI" ] || { echo "ERRO: $CLI não encontrado"; exit 1; }

echo "[2/6] Criando backups..."

cp "$AI" "$AI.bak-ollama"
cp "$CLI" "$CLI.bak-ollama"

echo "[3/6] Verificando Ollama..."

if ! curl -fsS http://127.0.0.1:11434/api/tags >/dev/null 2>&1; then
    echo "ERRO: Ollama não está acessível em 127.0.0.1:11434"
    echo "Execute 'ollama serve' em outra sessão do Termux."
    exit 1
fi

echo "[OK] Ollama acessível."

echo "[4/6] Alterando OpenRouterClient..."

python3 - "$AI" <<'PY'
from pathlib import Path
import sys

p = Path(sys.argv[1])
s = p.read_text()

old = '''public sealed class OpenRouterOptions
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";'''

new = '''public sealed class OpenRouterOptions
    {
        public string Provider { get; set; } = "openrouter";
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";'''

if old not in s:
    raise SystemExit("ERRO: bloco OpenRouterOptions não encontrado.")

s = s.replace(old, new, 1)

old = '''private void EnsureValidApiKey()
        {
            if (string.IsNullOrWhiteSpace(Options.ApiKey))
            {
                throw new InvalidOperationException(
                    "ApiKey do provedor LLM não configurada. Defina OpenRouterOptions.ApiKey.");
            }

            if (Options.ApiKey.Length > 200 ||
                Options.ApiKey.IndexOfAny(new[] { ' ', '\\t', '\\r', '\\n' }) >= 0)
            {
                throw new InvalidOperationException(
                    "Chave de API inválida (parece conter conteúdo de log). " +
                    "Toque em 'Restaurar padrão' na aba Correções e digite a chave manualmente na aba Assistente.");
            }
        }'''

new = '''private void EnsureValidApiKey()
        {
            if (string.Equals(Options.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(Options.BaseUrl))
                {
                    throw new InvalidOperationException(
                        "Endpoint do Ollama não configurado.");
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(Options.ApiKey))
            {
                throw new InvalidOperationException(
                    "ApiKey do provedor LLM não configurada. Defina OpenRouterOptions.ApiKey.");
            }

            if (Options.ApiKey.Length > 200 ||
                Options.ApiKey.IndexOfAny(new[] { ' ', '\\t', '\\r', '\\n' }) >= 0)
            {
                throw new InvalidOperationException(
                    "Chave de API inválida (parece conter conteúdo de log). " +
                    "Toque em 'Restaurar padrão' na aba Correções e digite a chave manualmente na aba Assistente.");
            }
        }'''

if old not in s:
    raise SystemExit("ERRO: método EnsureValidApiKey não encontrado.")

s = s.replace(old, new, 1)

old = '''var request = new HttpRequestMessage(HttpMethod.Post, Options.BaseUrl);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + Options.ApiKey);
            if (Options.AppReference != null)
            {
                request.Headers.TryAddWithoutValidation("X-Title", "AURA");
                request.Headers.TryAddWithoutValidation("X-URL", Options.AppReference);
            }'''

new = '''var request = new HttpRequestMessage(HttpMethod.Post, Options.BaseUrl);

            if (!string.Equals(Options.Provider, "ollama", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.TryAddWithoutValidation(
                    "Authorization",
                    "Bearer " + Options.ApiKey);

                if (Options.AppReference != null)
                {
                    request.Headers.TryAddWithoutValidation("X-Title", "AURA");
                    request.Headers.TryAddWithoutValidation("X-URL", Options.AppReference);
                }
            }'''

# Existem dois lugares semelhantes no arquivo.
# Para ChatToolsAsync, substituímos a última ocorrência relevante.
if old not in s:
    raise SystemExit("ERRO: bloco de headers não encontrado.")

s = s.replace(old, new, 1)

p.write_text(s)
PY

echo "[OK] OpenRouterClient atualizado."

echo "[5/6] Alterando Program.cs..."

python3 - "$CLI" <<'PY'
from pathlib import Path
import sys

p = Path(sys.argv[1])
s = p.read_text()

start = s.find("private static OpenRouterClient EnsureAiClient(string? model = null)")
if start < 0:
    raise SystemExit("ERRO: EnsureAiClient não encontrado.")

brace = s.find("{", start)
if brace < 0:
    raise SystemExit("ERRO: abertura de EnsureAiClient não encontrada.")

depth = 0
end = None

for i in range(brace, len(s)):
    if s[i] == "{":
        depth += 1
    elif s[i] == "}":
        depth -= 1
        if depth == 0:
            end = i + 1
            break

if end is None:
    raise SystemExit("ERRO: fechamento de EnsureAiClient não encontrado.")

new_method = '''private static OpenRouterClient EnsureAiClient(string? model = null)
        {
            if (_aiClient != null)
            {
                if (!string.IsNullOrWhiteSpace(model))
                {
                    _aiClient.Options.Model = model;
                }

                return _aiClient;
            }

            string provider =
                Environment.GetEnvironmentVariable("AURA_PROVIDER")
                ?? "ollama";

            provider = provider.Trim().ToLowerInvariant();

            if (provider == "ollama")
            {
                _aiClient = new OpenRouterClient(
                    new OpenRouterOptions
                    {
                        Provider = "ollama",
                        ApiKey = string.Empty,
                        BaseUrl = "http://127.0.0.1:11434/v1/chat/completions",
                        Model = string.IsNullOrWhiteSpace(model)
                            ? "qwen2.5-coder:1.5b"
                            : model,
                        MaxTokens = 1500,
                        TimeoutSeconds = 120
                    });

                return _aiClient;
            }

            string apiKey =
                Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                string keyFile = Path.Combine(UserAuraDir(), "ai_key.txt");

                if (File.Exists(keyFile))
                {
                    apiKey = File.ReadAllText(keyFile).Trim();
                }
            }

            _aiClient = new OpenRouterClient(
                new OpenRouterOptions
                {
                    Provider = "openrouter",
                    ApiKey = apiKey,
                    Model = string.IsNullOrWhiteSpace(model)
                        ? "qwen/qwen-plus"
                        : model,
                    AppReference = "CLI"
                });

            return _aiClient;
        }'''

s = s[:start] + new_method + s[end:]

p.write_text(s)
PY

echo "[OK] Program.cs atualizado."

echo "[6/6] Compilando AURA.CLI..."

dotnet build src/AURA.CLI/AURA.CLI.csproj

echo
echo "======================================"
echo " CONFIGURAÇÃO CONCLUÍDA"
echo "======================================"
echo
echo "Provider padrão: Ollama"
echo "Modelo: qwen2.5-coder:1.5b"
echo "Endpoint: http://127.0.0.1:11434/v1/chat/completions"
echo
echo "Backups:"
echo "  $AI.bak-ollama"
echo "  $CLI.bak-ollama"
echo
echo "Próximo teste:"
echo "  dotnet run --project src/AURA.CLI/AURA.CLI.csproj"
echo
echo "Depois, dentro da AURA:"
echo '  chat "Responda apenas: AURA usando Ollama"'
echo
