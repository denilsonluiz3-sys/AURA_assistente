#!/data/data/com.termux/files/usr/bin/bash
set -e

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP="$ROOT/.aura/backup-provider-runtime-$STAMP"

mkdir -p "$BACKUP"

echo "=============================================="
echo " AURA — PROVIDER RUNTIME"
echo "=============================================="
echo
echo "Backup: $BACKUP"
echo

# ------------------------------------------------
# BACKUP
# ------------------------------------------------

for f in \
    "$ROOT/src/AURA.AI/ProviderCatalog.cs" \
    "$ROOT/src/AURA.CLI/Program.cs" \
    "$ROOT/src/AURA.AI/OpenRouterClient.cs" \
    "$ROOT/src/AURA.AI/AiAssistantService.cs" \
    "$ROOT/config/providers.json"
do
    if [ -f "$f" ]; then
        cp "$f" "$BACKUP/$(basename "$f").bak"
    fi
done

# ------------------------------------------------
# GARANTE providers.json
# ------------------------------------------------

mkdir -p "$ROOT/config"

if [ ! -f "$ROOT/config/providers.json" ]; then
    echo "[ERRO] config/providers.json não existe."
    echo "Execute primeiro o script de migração do catálogo."
    exit 1
fi

# ------------------------------------------------
# CRIA RESOLVER CENTRAL
# ------------------------------------------------

cat > "$ROOT/src/AURA.AI/ProviderRuntime.cs" <<'CS'
using System;
using System.IO;
using System.Text.Json;

namespace AURA.AI
{
    /// <summary>
    /// Resolve o provedor/modelo ativo da AURA.
    /// A configuração vem do catálogo; segredos vêm somente do ambiente.
    /// </summary>
    public sealed class ProviderRuntime
    {
        public ProviderInfo Provider { get; }
        public ProviderModel Model { get; }
        public string ApiKey { get; }
        public string KeyEnv { get; }

        private ProviderRuntime(
            ProviderInfo provider,
            ProviderModel model,
            string apiKey,
            string keyEnv)
        {
            Provider = provider;
            Model = model;
            ApiKey = apiKey;
            KeyEnv = keyEnv;
        }

        public static ProviderRuntime Load()
        {
            string providerName =
                Environment.GetEnvironmentVariable("AURA_PROVIDER")
                ?? "openai";

            string? modelName =
                Environment.GetEnvironmentVariable("AURA_MODEL");

            ProviderInfo? provider =
                ProviderCatalog.Find(providerName);

            if (provider == null)
            {
                throw new InvalidOperationException(
                    $"Provedor '{providerName}' não encontrado no catálogo.");
            }

            ProviderModel? model = null;

            if (!string.IsNullOrWhiteSpace(modelName))
            {
                foreach (ProviderModel candidate in provider.Models)
                {
                    if (string.Equals(
                        candidate.Id,
                        modelName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        model = candidate;
                        break;
                    }
                }
            }

            model ??= provider.Models.Count > 0
                ? provider.Models[0]
                : new ProviderModel
                {
                    Id = string.Empty,
                    Label = "modelo padrão"
                };

            string keyEnv = provider.KeyEnv ?? string.Empty;

            string apiKey = string.IsNullOrWhiteSpace(keyEnv)
                ? string.Empty
                : Environment.GetEnvironmentVariable(keyEnv)
                    ?? string.Empty;

            return new ProviderRuntime(
                provider,
                model,
                apiKey,
                keyEnv);
        }

        public OpenRouterOptions CreateOptions()
        {
            return new OpenRouterOptions
            {
                Provider = Provider.Id,
                ApiKey = ApiKey,
                BaseUrl = Provider.BaseUrl,
                Model = Model.Id,
                MaxTokens = 1500,
                TimeoutSeconds = 120,
                AppReference = "AURA"
            };
        }

        public static string Describe(ProviderRuntime runtime)
        {
            string keyStatus =
                string.IsNullOrWhiteSpace(runtime.KeyEnv)
                    ? "não necessária"
                    : string.IsNullOrWhiteSpace(runtime.ApiKey)
                        ? runtime.KeyEnv + " AUSENTE"
                        : runtime.KeyEnv + " CONFIGURADA";

            return
                $"Provedor: {runtime.Provider.Name}\n" +
                $"Modelo: {runtime.Model.Id}\n" +
                $"Endpoint: {runtime.Provider.BaseUrl}\n" +
                $"Chave: {keyStatus}";
        }
    }
}
CS

echo "[OK] ProviderRuntime.cs criado."

# ------------------------------------------------
# AJUSTA ProviderCatalog
# ------------------------------------------------

python3 - "$ROOT/src/AURA.AI/ProviderCatalog.cs" <<'PY'
from pathlib import Path
import sys

p = Path(sys.argv[1])
s = p.read_text()

# Adiciona Id e KeyEnv caso a versão atual ainda não tenha.
needle = 'public sealed class ProviderInfo\n    {'

if needle in s and 'public string Id' not in s:
    s = s.replace(
        needle,
        '''public sealed class ProviderInfo
    {
        public string Id { get; init; } = string.Empty;'''
    )

if 'public string KeyEnv' not in s:
    s = s.replace(
        'public string KeyHint { get; init; } = string.Empty;',
        '''public string KeyHint { get; init; } = string.Empty;
        public string KeyEnv { get; init; } = string.Empty;'''
    )

# OpenAI precisa ser explicitamente o primeiro/padrão.
# Não altera o catálogo existente além das propriedades necessárias.
p.write_text(s)
PY

echo "[OK] ProviderCatalog preparado."

# ------------------------------------------------
# CONVERTE A RESOLUÇÃO DO CLI
# ------------------------------------------------

python3 - "$ROOT/src/AURA.CLI/Program.cs" <<'PY'
from pathlib import Path
import sys

p = Path(sys.argv[1])
s = p.read_text()

start_marker = '        private static OpenRouterClient EnsureAiClient(string? model = null)'
start = s.find(start_marker)

if start == -1:
    raise SystemExit(
        "[ERRO] Método EnsureAiClient não encontrado."
    )

# Encontra o próximo método após EnsureAiClient.
next_marker = '        private static void Ask('
end = s.find(next_marker, start)

if end == -1:
    raise SystemExit(
        "[ERRO] Não foi possível localizar o fim de EnsureAiClient."
    )

replacement = r'''        private static OpenRouterClient EnsureAiClient(string? model = null)
        {
            if (_aiClient != null)
            {
                if (!string.IsNullOrWhiteSpace(model))
                {
                    _aiClient.Options.Model = model;
                }

                return _aiClient;
            }

            if (!string.IsNullOrWhiteSpace(model))
            {
                Environment.SetEnvironmentVariable("AURA_MODEL", model);
            }

            ProviderRuntime runtime = ProviderRuntime.Load();

            _aiClient = new OpenRouterClient(
                runtime.CreateOptions());

            Console.WriteLine(
                "[INFO] " + ProviderRuntime.Describe(runtime));

            return _aiClient;
        }

'''

s = s[:start] + replacement + s[end:]

# Mensagens antigas que citavam somente OpenRouter.
s = s.replace(
    'Pergunta direta à IA (OpenRouter)',
    'Pergunta direta à IA'
)

s = s.replace(
    'Ou defina a variável OPENROUTER_API_KEY.',
    'A chave é lida automaticamente do catálogo.'
)

p.write_text(s)
PY

echo "[OK] CLI passou a usar ProviderRuntime."

# ------------------------------------------------
# AJUSTA DEFAULT DO OpenRouterClient
# ------------------------------------------------

python3 - "$ROOT/src/AURA.AI/OpenRouterClient.cs" <<'PY'
from pathlib import Path
import sys

p = Path(sys.argv[1])
s = p.read_text()

s = s.replace(
    'public string Provider { get; set; } = "openrouter";',
    'public string Provider { get; set; } = "openai";'
)

s = s.replace(
    'public string ApiKey { get; set; } = string.Empty;',
    'public string ApiKey { get; set; } = string.Empty;'
)

s = s.replace(
    'public string Model { get; set; } = "qwen/qwen-plus";',
    'public string Model { get; set; } = "gpt-5-mini";'
)

p.write_text(s)
PY

echo "[OK] Defaults do OpenRouterClient ajustados."

# ------------------------------------------------
# VERIFICAÇÃO
# ------------------------------------------------

echo
echo "===== REFERÊNCIAS DE CONFIGURAÇÃO ====="

grep -RniE \
'OPENAI_API_KEY|OPENROUTER_API_KEY|AURA_PROVIDER|AURA_MODEL|api.openai.com|11434' \
"$ROOT/src/AURA.AI" \
"$ROOT/src/AURA.CLI" \
--include='*.cs' \
--exclude-dir=bin \
--exclude-dir=obj || true

echo
echo "===== BUILD ====="

dotnet build "$ROOT/src/AURA.CLI/AURA.CLI.csproj" --no-restore

echo
echo "===== TESTE DE CONFIGURAÇÃO ====="

printf 'OPENAI_API_KEY=%s\n' \
    "$([ -n "${OPENAI_API_KEY:-}" ] && echo CONFIGURADA || echo AUSENTE)"

printf 'AURA_PROVIDER=%s\n' \
    "${AURA_PROVIDER:-openai (padrão)}"

printf 'AURA_MODEL=%s\n' \
    "${AURA_MODEL:-primeiro modelo do provedor}"

echo
echo "=============================================="
echo " MIGRAÇÃO CONCLUÍDA"
echo "=============================================="
echo
echo "Fonte de configuração:"
echo "  config/providers.json"
echo
echo "Resolução:"
echo "  ProviderRuntime"
echo
echo "Padrão:"
echo "  OpenAI"
echo
echo "Segredo:"
echo "  OPENAI_API_KEY"
echo
echo "Backup:"
echo "  $BACKUP"
echo
echo "Para testar:"
echo '  dotnet run --project src/AURA.CLI/AURA.CLI.csproj -- chat "Responda apenas: AURA OK"'
echo
