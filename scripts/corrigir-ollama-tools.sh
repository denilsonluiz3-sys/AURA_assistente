#!/data/data/com.termux/files/usr/bin/bash
set -e

cd "$HOME/AURA"

echo "=========================================="
echo " AURA — CORREÇÃO DE FERRAMENTAS OLLAMA"
echo "=========================================="

FILE="src/AURA.AI/OpenRouterClient.cs"

cp "$FILE" "$FILE.bak-tools"

python3 <<'PY'
from pathlib import Path

p = Path("src/AURA.AI/OpenRouterClient.cs")
s = p.read_text()

marker = "private static string? GetProp(JsonElement el, string name)"

if marker not in s:
    raise SystemExit("ERRO: ponto de inserção não encontrado.")

method = r'''
        private static List<AgentToolCall>? TryParseTextToolCall(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            string text = content.Trim();

            // Remove bloco Markdown ```json ... ```
            if (text.StartsWith("```"))
            {
                int firstNewline = text.IndexOf('\n');
                int lastFence = text.LastIndexOf("```");

                if (firstNewline >= 0 && lastFence > firstNewline)
                    text = text.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
            }

            try
            {
                using var doc = JsonDocument.Parse(text);
                JsonElement root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                    return null;

                if (!root.TryGetProperty("name", out JsonElement nameEl))
                    return null;

                if (nameEl.ValueKind != JsonValueKind.String)
                    return null;

                string? name = nameEl.GetString();

                if (string.IsNullOrWhiteSpace(name))
                    return null;

                string arguments = "{}";

                if (root.TryGetProperty("arguments", out JsonElement argsEl))
                {
                    arguments = argsEl.GetRawText();

                    // Alguns modelos retornam arguments como string JSON.
                    if (argsEl.ValueKind == JsonValueKind.String)
                    {
                        string? str = argsEl.GetString();

                        if (!string.IsNullOrWhiteSpace(str))
                            arguments = str;
                    }
                }

                return new List<AgentToolCall>
                {
                    new AgentToolCall
                    {
                        Id = "ollama-tool-" + Guid.NewGuid().ToString("N"),
                        Name = name,
                        ArgumentsJson = arguments
                    }
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

'''

s = s.replace(marker, method + "        " + marker)

# Localiza o retorno normal do ChatToolsAsync.
old = '''return new AgentChatResponse
                        {
                            Content = content,
                            ToolCalls = calls.Count > 0 ? calls : null
                        };'''

new = '''// Ollama/Qwen pequeno pode retornar a chamada de ferramenta
                        // como JSON no campo content, em vez de usar tool_calls.
                        if (calls.Count == 0)
                        {
                            List<AgentToolCall>? textCalls = TryParseTextToolCall(content);

                            if (textCalls is { Count: > 0 })
                            {
                                return new AgentChatResponse
                                {
                                    Content = null,
                                    ToolCalls = textCalls
                                };
                            }
                        }

                        return new AgentChatResponse
                        {
                            Content = content,
                            ToolCalls = calls.Count > 0 ? calls : null
                        };'''

if old not in s:
    raise SystemExit("ERRO: retorno do ChatToolsAsync não encontrado.")

s = s.replace(old, new)

p.write_text(s)

print("OpenRouterClient.cs atualizado.")
PY

echo
echo "[1/3] Compilando..."

dotnet build src/AURA.CLI/AURA.CLI.csproj

echo
echo "[2/3] Verificando compilação..."

if [ ! -f "src/AURA.CLI/bin/Debug/net10.0/AURA.CLI.dll" ]; then
    echo "ERRO: AURA.CLI.dll não foi gerada."
    exit 1
fi

echo "Build OK."

echo
echo "[3/3] Correção instalada."
echo
echo "Agora execute:"
echo
echo "dotnet run --project src/AURA.CLI/AURA.CLI.csproj"
echo
echo 'E teste:'
echo
echo 'agent "Crie um arquivo chamado teste_agente.txt contendo uma mensagem dizendo que o agente Ollama da AURA está funcionando."'
echo
