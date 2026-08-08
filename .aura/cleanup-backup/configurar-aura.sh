#!/data/data/com.termux/files/usr/bin/bash
set -e

ROOT="$HOME/AURA"
CLI="$ROOT/src/AURA.CLI/Program.cs"
BACKUP="$CLI.bak-aura"

AURA_DIR="$HOME/.aura"
WORKSPACE="$AURA_DIR/workspace"
PROMPT="$AURA_DIR/aura_master_prompt.txt"

echo "=== CONFIGURANDO AURA ==="

mkdir -p "$AURA_DIR"
mkdir -p "$WORKSPACE"
mkdir -p "$ROOT/scripts"

# ------------------------------------------------------------
# 1. Prompt mestre
# ------------------------------------------------------------

cat > "$PROMPT" <<'PROMPT_EOF'
Você é AURA, o agente operacional local do projeto AURA.

IDENTIDADE
- Seu nome é AURA.
- Você não deve se chamar "agent".
- Você é um agente de execução assistida.
- Seu trabalho é transformar instruções do usuário em ações usando as ferramentas disponíveis.

AMBIENTE
- Sistema: Termux/Android.
- Workspace oficial:
  /data/data/com.termux/files/home/.aura/workspace
- Diretório atual esperado das ferramentas:
  /data/data/com.termux/files/home/.aura/workspace

REGRA FUNDAMENTAL DE CAMINHOS
- Caminhos relativos são relativos ao workspace.
- Para a raiz do workspace use ".".
- Nunca use "workspace/arquivo.txt" quando o workspace já é a raiz.
- Nunca invente "home/.aura/workspace".
- Nunca transforme um parâmetro string em um objeto JSON.
- Quando uma ferramenta espera:
  {"path":"arquivo.txt"}
  envie exatamente uma string em "path".

FERRAMENTAS

1. list_dir
Descrição:
Lista arquivos e diretórios.

Formato correto:
{"path":"."}

Para subdiretório:
{"path":"subdiretorio"}

2. read_file
Descrição:
Lê um arquivo.

Formato correto:
{"path":"arquivo.txt"}

3. write_file
Descrição:
Cria ou sobrescreve um arquivo.

Formato correto:
{
  "path":"arquivo.txt",
  "content":"conteúdo"
}

4. edit_file
Descrição:
Substitui um trecho existente.

Formato correto:
{
  "path":"arquivo.txt",
  "old_text":"texto existente",
  "new_text":"novo texto"
}

IMPORTANTE:
- old_text nunca pode ser vazio.
- Antes de editar, leia o arquivo se não souber exatamente o conteúdo.
- Não invente old_text.
- Não envie um array quando a ferramenta espera um objeto.

5. run_shell
Descrição:
Executa um comando shell no workspace.

Formato correto:
{"command":"pwd"}

Outro exemplo:
{"command":"ls -la"}

REGRAS DE EXECUÇÃO
1. Analise a solicitação.
2. Escolha a ferramenta adequada.
3. Gere argumentos JSON válidos.
4. Execute uma ferramenta por vez quando houver dependência entre ações.
5. Use o resultado da ferramenta para decidir o próximo passo.
6. Não invente resultados.
7. Não repita uma ferramenta sem motivo.
8. Não transforme schemas de ferramentas em argumentos.
9. Não retorne JSON fictício como substituto da execução.
10. Se a ferramenta retornar erro, corrija os argumentos usando o erro recebido.
11. Para criar arquivo, use write_file.
12. Para ler arquivo, use read_file.
13. Para alterar arquivo existente, primeiro leia e depois use edit_file.
14. Para descobrir arquivos, use list_dir com ".".
15. Para descobrir o diretório atual, use run_shell com "pwd".

EXEMPLO

Usuário:
"Liste os arquivos do workspace."

Ação:
{"path":"."}

Usuário:
"Crie teste.txt contendo AURA OK."

Ação:
{"path":"teste.txt","content":"AURA OK"}

Usuário:
"Leia teste.txt."

Ação:
{"path":"teste.txt"}

Usuário:
"Altere AURA OK para AURA EDITADO."

Primeiro leia o arquivo. Depois:
{
  "path":"teste.txt",
  "old_text":"AURA OK",
  "new_text":"AURA EDITADO"
}

Usuário:
"Execute pwd."

Ação:
{"command":"pwd"}

NUNCA faça isto:
{"path":{"type":"string","description":"..."}}

NUNCA faça isto:
{"command":{"type":"string","description":"pwd"}}

NUNCA faça isto:
{"path":[]}

NUNCA faça isto:
{"name":"list_dir","arguments":{}}

quando estiver sendo chamado diretamente como ferramenta.

OBJETIVO
Ser um agente confiável para operar o workspace da AURA, usando ferramentas reais, caminhos corretos e argumentos JSON válidos.
PROMPT_EOF

echo "[OK] Prompt mestre:"
echo "     $PROMPT"

# ------------------------------------------------------------
# 2. Backup
# ------------------------------------------------------------

if [ ! -f "$CLI" ]; then
    echo "[ERRO] Program.cs não encontrado:"
    echo "       $CLI"
    exit 1
fi

cp "$CLI" "$BACKUP"
echo "[OK] Backup:"
echo "     $BACKUP"

# ------------------------------------------------------------
# 3. Verifica se aura já existe
# ------------------------------------------------------------

if grep -q 'case "aura"' "$CLI"; then
    echo "[INFO] O comando 'aura' já existe."
else

    python3 - "$CLI" "$PROMPT" <<'PY'
import sys
from pathlib import Path

cli = Path(sys.argv[1])
prompt_path = sys.argv[2]

text = cli.read_text()

# Localiza o dispatcher de comandos.
markers = [
    'switch (command)',
    'switch(command)',
    'switch (cmd)',
    'switch(cmd)'
]

marker = None
for m in markers:
    if m in text:
        marker = m
        break

if marker is None:
    print("[ERRO] Não foi possível localizar o switch de comandos do Program.cs.")
    sys.exit(2)

# Procuramos o primeiro case dentro do switch.
pos = text.find(marker)
open_brace = text.find("{", pos)

if open_brace < 0:
    print("[ERRO] Switch encontrado, mas sem bloco.")
    sys.exit(3)

# Código que será colocado no dispatcher.
block = r'''
                case "aura":
                {
                    AuraCommand(parts);
                    break;
                }
'''

# Encontra o primeiro case depois da abertura.
case_pos = text.find("case ", open_brace)

if case_pos < 0:
    print("[ERRO] Nenhum 'case' encontrado no dispatcher.")
    sys.exit(4)

text = text[:case_pos] + block + text[case_pos:]

# ------------------------------------------------------------
# Insere método AuraCommand antes do último fechamento da classe.
# ------------------------------------------------------------

method = r'''
        private static void AuraCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                Console.WriteLine("Uso: aura \"instrução\"");
                return;
            }

            var instruction = string.Join(" ", parts, 1, parts.Length - 1);

            string auraDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".aura");

            string workspace = Path.Combine(auraDir, "workspace");
            string promptFile = Path.Combine(auraDir, "aura_master_prompt.txt");

            Directory.CreateDirectory(workspace);

            string? systemPrompt = null;

            try
            {
                if (File.Exists(promptFile))
                {
                    systemPrompt = File.ReadAllText(promptFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] Não foi possível carregar o prompt mestre: " + ex.Message);
            }

            Console.WriteLine("AURA · agente local");
            Console.WriteLine("Workspace: " + workspace);
            Console.WriteLine("Pensando...");

            try
            {
                var client = EnsureAiClient();

                var tools = CreateAuraTools(workspace);

                var session = new AURA.AI.AgentSession(
                    client,
                    tools,
                    systemPrompt);

                session.Step += step =>
                {
                    Console.WriteLine();
                    Console.WriteLine("  ◆ " + step.Name);

                    if (!string.IsNullOrWhiteSpace(step.ArgumentsJson))
                    {
                        Console.WriteLine("    " + step.ArgumentsJson);
                    }

                    if (!string.IsNullOrWhiteSpace(step.Result))
                    {
                        Console.WriteLine("    " + step.Result);
                    }
                };

                string result = session.RunAsync(instruction).GetAwaiter().GetResult();

                Console.WriteLine();
                Console.WriteLine("=== AURA ===");
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] AURA: " + ex.Message);
            }
        }

        private static List<AURA.AI.AgentTool> CreateAuraTools(string workspace)
        {
            string ResolvePath(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("Caminho vazio.");

                path = path.Trim();

                if (path == ".")
                    return workspace;

                if (Path.IsPathRooted(path))
                    return Path.GetFullPath(path);

                return Path.GetFullPath(Path.Combine(workspace, path));
            }

            return new List<AURA.AI.AgentTool>
            {
                new AURA.AI.AgentTool
                {
                    Definition = new AURA.AI.AgentToolDefinition
                    {
                        Name = "list_dir",
                        Description = "Lista arquivos e diretórios do workspace. Use '.' para a raiz.",
                        Parameters = new Dictionary<string, AURA.AI.AgentToolParameter>
                        {
                            ["path"] = new AURA.AI.AgentToolParameter
                            {
                                Type = "string",
                                Description = "Caminho relativo ao workspace. Use '.' para a raiz."
                            }
                        },
                        Required = new List<string> { "path" }
                    },
                    ExecuteAsync = async (json, ct) =>
                    {
                        await Task.Yield();

                        using var doc = System.Text.Json.JsonDocument.Parse(json);

                        if (!doc.RootElement.TryGetProperty("path", out var pathEl) ||
                            pathEl.ValueKind != System.Text.Json.JsonValueKind.String)
                        {
                            throw new ArgumentException("list_dir: 'path' deve ser string.");
                        }

                        string path = pathEl.GetString() ?? ".";
                        string full = ResolvePath(path);

                        if (!Directory.Exists(full))
                            throw new DirectoryNotFoundException(full);

                        var entries = Directory
                            .EnumerateFileSystemEntries(full)
                            .Select(Path.GetFileName)
                            .Where(x => !string.IsNullOrEmpty(x))
                            .OrderBy(x => x)
                            .ToArray();

                        return entries.Length == 0
                            ? "(diretório vazio)"
                            : string.Join(Environment.NewLine, entries);
                    }
                },

                new AURA.AI.AgentTool
                {
                    Definition = new AURA.AI.AgentToolDefinition
                    {
                        Name = "read_file",
                        Description = "Lê o conteúdo de um arquivo do workspace.",
                        Parameters = new Dictionary<string, AURA.AI.AgentToolParameter>
                        {
                            ["path"] = new AURA.AI.AgentToolParameter
                            {
                                Type = "string",
                                Description = "Caminho do arquivo relativo ao workspace."
                            }
                        },
                        Required = new List<string> { "path" }
                    },
                    ExecuteAsync = async (json, ct) =>
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);

                        if (!doc.RootElement.TryGetProperty("path", out var pathEl) ||
                            pathEl.ValueKind != System.Text.Json.JsonValueKind.String)
                        {
                            throw new ArgumentException("read_file: 'path' deve ser string.");
                        }

                        string full = ResolvePath(pathEl.GetString() ?? "");

                        if (!File.Exists(full))
                            throw new FileNotFoundException("Arquivo não existe", full);

                        return await File.ReadAllTextAsync(full, ct);
                    }
                },

                new AURA.AI.AgentTool
                {
                    Definition = new AURA.AI.AgentToolDefinition
                    {
                        Name = "write_file",
                        Description = "Cria ou sobrescreve um arquivo no workspace.",
                        Parameters = new Dictionary<string, AURA.AI.AgentToolParameter>
                        {
                            ["path"] = new AURA.AI.AgentToolParameter
                            {
                                Type = "string",
                                Description = "Caminho do arquivo relativo ao workspace."
                            },
                            ["content"] = new AURA.AI.AgentToolParameter
                            {
                                Type = "string",
                                Description = "Conteúdo completo do arquivo."
                            }
                        },
                        Required = new List<string> { "path", "content" }
                    },
                    ExecuteAsync = async (json, ct) =>
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);

                        if (!doc.RootElement.TryGetProperty("path", out var pathEl) ||
                            pathEl.ValueKind != System.Text.Json.JsonValueKind.String)
                            throw new ArgumentException("write_file: 'path' deve ser string.");

                        if (!doc.RootElement.TryGetProperty("content", out var contentEl) ||
                            contentEl.ValueKind != System.Text.Json.JsonValueKind.String)
                            throw new ArgumentException("write_file: 'content' deve ser string.");

                        string full = ResolvePath(pathEl.GetString() ?? "");
                        string content = contentEl.GetString() ?? "";

                        string? dir = Path.GetDirectoryName(full);

                        if (!string.IsNullOrEmpty(dir))
                            Directory.CreateDirectory(dir);

                        await File.WriteAllTextAsync(full, content, ct);

                        return "OK: arquivo gravado (" +
                               content.Length +
                               " chars): " +
                               full;
                    }
                },

                new AURA.AI.AgentTool
                {
                    Definition = new AURA.AI.AgentToolDefinition
                    {
                        Name = "edit_file",
                        Description = "Substitui exatamente um trecho existente de um arquivo.",
                        Parameters = new Dictionary<string, AURA.AI.AgentToolParameter>
                        {
                            ["path"] = new AURA.AI.AgentToolParameter
                            {
                                Type = "string",
                                Description = "Caminho do arquivo."
                            },
                            ["old_text"] = new AURA.AI.AgentToolParameter
                            {
                                Type = "string",
                                Description = "Texto existente que será substituído."
                            },
                            ["new_text"] = new AURA.AI.AgentToolParameter
                            {
                                Type = "string",
                                Description = "Novo texto."
                            }
                        },
                        Required = new List<string>
                        {
                            "path",
                            "old_text",
                            "new_text"
                        }
                    },
                    ExecuteAsync = async (json, ct) =>
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);

                        string GetString(string name)
                        {
                            if (!doc.RootElement.TryGetProperty(name, out var el) ||
                                el.ValueKind != System.Text.Json.JsonValueKind.String)
                                throw new ArgumentException(
                                    "edit_file: '" + name + "' deve ser string.");

                            return el.GetString() ?? "";
                        }

                        string path = GetString("path");
                        string oldText = GetString("old_text");
                        string newText = GetString("new_text");

                        if (string.IsNullOrEmpty(oldText))
                            throw new ArgumentException(
                                "edit_file: old_text não pode ser vazio.");

                        string full = ResolvePath(path);

                        if (!File.Exists(full))
                            throw new FileNotFoundException(
                                "Arquivo não existe", full);

                        string content = await File.ReadAllTextAsync(full, ct);

                        if (!content.Contains(oldText, StringComparison.Ordinal))
                            return "ERRO: old_text não encontrado no arquivo: " + full;

                        content = content.Replace(
                            oldText,
                            newText,
                            StringComparison.Ordinal);

                        await File.WriteAllTextAsync(full, content, ct);

                        return "OK: arquivo alterado: " + full;
                    }
                },

                new AURA.AI.AgentTool
                {
                    Definition = new AURA.AI.AgentToolDefinition
                    {
                        Name = "run_shell",
                        Description = "Executa um comando shell no workspace.",
                        Parameters = new Dictionary<string, AURA.AI.AgentToolParameter>
                        {
                            ["command"] = new AURA.AI.AgentToolParameter
                            {
                                Type = "string",
                                Description = "Comando shell a executar."
                            }
                        },
                        Required = new List<string> { "command" }
                    },
                    ExecuteAsync = async (json, ct) =>
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);

                        if (!doc.RootElement.TryGetProperty("command", out var commandEl) ||
                            commandEl.ValueKind != System.Text.Json.JsonValueKind.String)
                        {
                            throw new ArgumentException(
                                "run_shell: 'command' deve ser string.");
                        }

                        string command = commandEl.GetString() ?? "";

                        if (string.IsNullOrWhiteSpace(command))
                            throw new ArgumentException(
                                "run_shell: comando vazio.");

                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "/data/data/com.termux/files/usr/bin/bash",
                            Arguments = "-lc " +
                                "\"" +
                                command.Replace("\"", "\\\"") +
                                "\"",
                            WorkingDirectory = workspace,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using var process =
                            new System.Diagnostics.Process();

                        process.StartInfo = psi;
                        process.Start();

                        string stdout =
                            await process.StandardOutput.ReadToEndAsync();

                        string stderr =
                            await process.StandardError.ReadToEndAsync();

                        await process.WaitForExitAsync(ct);

                        return
                            "exit=" + process.ExitCode +
                            Environment.NewLine +
                            stdout +
                            (string.IsNullOrWhiteSpace(stderr)
                                ? ""
                                : Environment.NewLine + stderr);
                    }
                }
            };
        }

'''

# Coloca antes do último fechamento da classe.
last = text.rfind("\n    }\n}")

if last < 0:
    print("[ERRO] Não foi possível localizar o fechamento da classe Program.")
    sys.exit(5)

text = text[:last] + "\n" + method + text[last:]

cli.write_text(text)

print("[OK] Comando 'aura' inserido.")
PY

fi

# ------------------------------------------------------------
# 4. Compilação
# ------------------------------------------------------------

echo
echo "=== TESTANDO COMPILAÇÃO ==="

dotnet build src/AURA.CLI/AURA.CLI.csproj --no-restore

echo
echo "=== CONFIGURAÇÃO CONCLUÍDA ==="
echo
echo "Prompt:"
echo "  $PROMPT"
echo
echo "Workspace:"
echo "  $WORKSPACE"
echo
echo "Agora execute:"
echo
echo "  dotnet run --project src/AURA.CLI/AURA.CLI.csproj"
echo
echo "E teste:"
echo
echo '  AURA> aura "Use list_dir para listar os arquivos do workspace."'
echo
echo '  AURA> aura "Use run_shell para executar pwd."'
echo
echo '  AURA> aura "Crie teste_aura.txt contendo exatamente: AURA OK"'
echo
echo '  AURA> aura "Leia teste_aura.txt usando read_file."'
echo
echo '  AURA> aura "Altere teste_aura.txt de AURA OK para AURA EDITADO usando edit_file."'
echo
