#!/data/data/com.termux/files/usr/bin/bash

set -e

cd ~/AURA

FILE="src/AURA.AI/AgentSession.cs"
BACKUP="${FILE}.bak-tools-$(date +%Y%m%d-%H%M%S)"

echo "=========================================="
echo " AURA - FIX AGENT TOOLS / OLLAMA"
echo "=========================================="

if [ ! -f "$FILE" ]; then
    echo "ERRO: arquivo não encontrado:"
    echo "$FILE"
    exit 1
fi

echo
echo "[1/6] Backup..."
cp "$FILE" "$BACKUP"
echo "OK: $BACKUP"

echo
echo "[2/6] Aplicando normalizador de argumentos..."

python3 <<'PY'
from pathlib import Path

file = Path("src/AURA.AI/AgentSession.cs")
text = file.read_text()

if "NormalizeToolArguments" in text:
    print("Normalizador já existe.")
    raise SystemExit(0)

start = text.find("        private async Task<string> ExecuteToolAsync(")

if start < 0:
    raise SystemExit(
        "ERRO: ExecuteToolAsync não encontrado."
    )

# Encontrar o fechamento do método usando contagem de chaves.
brace_start = text.find("{", start)

if brace_start < 0:
    raise SystemExit(
        "ERRO: abertura de ExecuteToolAsync não encontrada."
    )

depth = 0
end = -1

for i in range(brace_start, len(text)):
    if text[i] == "{":
        depth += 1
    elif text[i] == "}":
        depth -= 1

        if depth == 0:
            end = i + 1
            break

if end < 0:
    raise SystemExit(
        "ERRO: fechamento de ExecuteToolAsync não encontrado."
    )

method = r'''        private async Task<string> ExecuteToolAsync(
            AgentToolCall call,
            CancellationToken ct)
        {
            AgentTool? tool = _tools.FirstOrDefault(
                t => t.Definition.Name == call.Name);

            if (tool == null)
            {
                return "ERRO: ferramenta desconhecida: " +
                       call.Name;
            }

            try
            {
                string normalized =
                    NormalizeToolArguments(
                        call.Name,
                        call.ArgumentsJson);

                _logger.Info(
                    "agent: argumentos normalizados='" +
                    normalized + "'");

                return await tool.ExecuteAsync(
                    normalized,
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "agent: falha na ferramenta '" +
                    call.Name +
                    "': " +
                    ex.Message);

                return "ERRO na ferramenta " +
                       call.Name +
                       ": " +
                       ex.Message;
            }
        }

        private static string NormalizeToolArguments(
            string toolName,
            string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return DefaultToolArguments(toolName);
            }

            string json = raw.Trim();

            // Remove markdown ```json ... ```
            if (json.StartsWith("```"))
            {
                int nl = json.IndexOf('\n');

                if (nl >= 0)
                {
                    json = json.Substring(nl + 1);
                }

                int fence = json.LastIndexOf("```");

                if (fence >= 0)
                {
                    json = json.Substring(0, fence);
                }

                json = json.Trim();
            }

            try
            {
                using JsonDocument doc =
                    JsonDocument.Parse(json);

                JsonElement root = doc.RootElement;

                // Qwen às vezes devolve:
                // [{"path":"teste.txt"}]
                if (root.ValueKind ==
                    JsonValueKind.Array &&
                    root.GetArrayLength() > 0)
                {
                    root = root[0];
                }

                if (root.ValueKind !=
                    JsonValueKind.Object)
                {
                    return DefaultToolArguments(toolName);
                }

                var output =
                    new Dictionary<string, object?>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (JsonProperty property
                    in root.EnumerateObject())
                {
                    JsonElement value =
                        property.Value;

                    if (value.ValueKind ==
                        JsonValueKind.String)
                    {
                        output[property.Name] =
                            value.GetString();

                        continue;
                    }

                    if (value.ValueKind ==
                        JsonValueKind.Number)
                    {
                        output[property.Name] =
                            value.ToString();

                        continue;
                    }

                    if (value.ValueKind ==
                            JsonValueKind.True ||
                        value.ValueKind ==
                            JsonValueKind.False)
                    {
                        output[property.Name] =
                            value.GetBoolean();

                        continue;
                    }

                    // Corrige:
                    //
                    // "path": {
                    //   "type": "string",
                    //   "description": "."
                    // }
                    //
                    if (value.ValueKind ==
                        JsonValueKind.Object)
                    {
                        if (value.TryGetProperty(
                                "value",
                                out JsonElement val) &&
                            val.ValueKind ==
                                JsonValueKind.String)
                        {
                            output[property.Name] =
                                val.GetString();

                            continue;
                        }

                        if (value.TryGetProperty(
                                "description",
                                out JsonElement desc) &&
                            desc.ValueKind ==
                                JsonValueKind.String)
                        {
                            output[property.Name] =
                                CleanModelValue(
                                    desc.GetString() ??
                                    "");

                            continue;
                        }

                        if (value.TryGetProperty(
                                "default",
                                out JsonElement def) &&
                            def.ValueKind ==
                                JsonValueKind.String)
                        {
                            output[property.Name] =
                                def.GetString();

                            continue;
                        }

                        output[property.Name] = "";
                        continue;
                    }
                }

                // list_dir
                if (toolName.Equals(
                        "list_dir",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!output.TryGetValue(
                            "path",
                            out object? path) ||
                        string.IsNullOrWhiteSpace(
                            Convert.ToString(path)))
                    {
                        output["path"] = ".";
                    }
                }

                // read_file
                if (toolName.Equals(
                        "read_file",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!output.ContainsKey("path"))
                    {
                        output["path"] = "";
                    }
                }

                // run_shell
                if (toolName.Equals(
                        "run_shell",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (output.TryGetValue(
                            "command",
                            out object? command))
                    {
                        string cmd =
                            Convert.ToString(command) ??
                            "";

                        output["command"] =
                            CleanModelValue(cmd);
                    }
                }

                // write_file
                if (toolName.Equals(
                        "write_file",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!output.ContainsKey("path"))
                    {
                        output["path"] = "";
                    }

                    if (!output.ContainsKey("content"))
                    {
                        output["content"] = "";
                    }
                }

                // edit_file
                if (toolName.Equals(
                        "edit_file",
                        StringComparison.OrdinalIgnoreCase))
                {
                    if (!output.ContainsKey("path"))
                    {
                        output["path"] = "";
                    }

                    if (!output.ContainsKey("old_text"))
                    {
                        output["old_text"] = "";
                    }

                    if (!output.ContainsKey("new_text"))
                    {
                        output["new_text"] = "";
                    }
                }

                return JsonSerializer.Serialize(output);
            }
            catch (JsonException)
            {
                // Tenta extrair JSON escondido em texto.
                int begin =
                    json.IndexOf('{');

                int finish =
                    json.LastIndexOf('}');

                if (begin >= 0 &&
                    finish > begin)
                {
                    string extracted =
                        json.Substring(
                            begin,
                            finish - begin + 1);

                    return NormalizeToolArguments(
                        toolName,
                        extracted);
                }

                return DefaultToolArguments(toolName);
            }
        }

        private static string DefaultToolArguments(
            string toolName)
        {
            if (toolName.Equals(
                    "list_dir",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "{\"path\":\".\"}";
            }

            if (toolName.Equals(
                    "read_file",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "{\"path\":\"\"}";
            }

            if (toolName.Equals(
                    "run_shell",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "{\"command\":\"\"}";
            }

            if (toolName.Equals(
                    "write_file",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "{\"path\":\"\",\"content\":\"\"}";
            }

            if (toolName.Equals(
                    "edit_file",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "{\"path\":\"\",\"old_text\":\"\",\"new_text\":\"\"}";
            }

            return "{}";
        }

        private static string CleanModelValue(
            string value)
        {
            string result =
                value.Trim();

            if (result.Length >= 2)
            {
                char first =
                    result[0];

                char last =
                    result[result.Length - 1];

                if ((first == '\'' &&
                     last == '\'') ||
                    (first == '"' &&
                     last == '"'))
                {
                    result =
                        result.Substring(
                            1,
                            result.Length - 2);
                }
            }

            return result.Trim();
        }
'''

text = text[:start] + method + text[end:]

file.write_text(text)

print("OK: AgentSession.cs atualizado.")
PY

echo
echo "[3/6] Verificando código..."

grep -n "NormalizeToolArguments" \
    src/AURA.AI/AgentSession.cs

echo
echo "[4/6] Compilando AURA.AI..."

dotnet build \
    src/AURA.AI/AURA.AI.csproj \
    --no-restore \
    -v:minimal

echo
echo "[5/6] Compilando AURA.CLI..."

dotnet build \
    src/AURA.CLI/AURA.CLI.csproj \
    --no-restore \
    -v:minimal

echo
echo "[6/6] TESTE RÁPIDO..."

echo
echo "=========================================="
echo " CORREÇÃO CONCLUÍDA"
echo "=========================================="
echo
echo "Backup:"
echo "$BACKUP"
echo
echo "Agora execute:"
echo
echo "cd ~/AURA"
echo "dotnet run --project src/AURA.CLI"
echo
echo "E teste nesta ordem:"
echo
echo 'agent "Liste os arquivos do workspace usando list_dir."'
echo
echo 'agent "Use run_shell para executar pwd."'
echo
echo 'agent "Crie teste_tools.txt contendo exatamente: AURA TOOL OK"'
echo
echo 'agent "Leia teste_tools.txt usando read_file."'
echo
echo 'agent "Altere teste_tools.txt de AURA TOOL OK para AURA TOOL EDIT OK usando edit_file."'
echo
echo 'agent "Leia teste_tools.txt usando read_file."'
echo
echo "=========================================="
