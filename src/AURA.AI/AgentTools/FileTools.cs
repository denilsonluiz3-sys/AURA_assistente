using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.AI
{
    public sealed class ListDirTool : WorkspaceAgentTool
    {
        public ListDirTool(string workspaceRoot) : base(workspaceRoot)
        {
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "list_dir",
            Description = "Lista o conteúdo de um diretório do workspace (pastas e arquivos com tamanho).",
            Parameters =
            {
                ["path"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Caminho relativo ao workspace (vazio ou '.' = raiz)."
                }
            }
        };

        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string path;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                path = ReadString(doc.RootElement, "path") ?? ".";
            }

            string dir = ResolvePath(path);
            if (!Directory.Exists(dir))
            {
                return Task.FromResult("ERRO: diretório não existe: " + path);
            }

            var sb = new StringBuilder();
            foreach (string entry in Directory.GetFileSystemEntries(dir)
                         .Where(e => !string.Equals(Path.GetFileName(e), "README_AURA.txt", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(e => e, StringComparer.OrdinalIgnoreCase))
            {
                ct.ThrowIfCancellationRequested();
                string name = Path.GetFileName(entry);
                if (Directory.Exists(entry))
                {
                    sb.AppendLine(name + "/");
                }
                else
                {
                    var fi = new FileInfo(entry);
                    sb.AppendLine(name + " (" + fi.Length + " bytes)");
                }
            }

            if (sb.Length == 0)
            {
                sb.AppendLine("(vazio)");
            }

            return Task.FromResult(sb.ToString().TrimEnd());
        }
    }

    public sealed class ReadFileTool : WorkspaceAgentTool
    {
        private const int MaxCharacters = 40000;
        private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".md", ".log", ".csv", ".json", ".xml", ".yaml", ".yml", ".toml", ".ini", ".conf",
            ".cs", ".csproj", ".sln", ".props", ".targets", ".xaml", ".java", ".kt", ".c", ".cpp", ".h",
            ".js", ".ts", ".tsx", ".jsx", ".py", ".sh", ".sql", ".html", ".css"
        };

        public ReadFileTool(string workspaceRoot) : base(workspaceRoot)
        {
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "read_file",
            Description = "Lê um arquivo textual útil do workspace (máx. 40.000 caracteres). Rejeita arquivos binários.",
            Parameters =
            {
                ["path"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Caminho relativo ao workspace."
                }
            },
            Required = { "path" }
        };

        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string path;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                path = ReadString(doc.RootElement, "path") ?? string.Empty;
            }

            string file = ResolvePath(path);
            if (!File.Exists(file))
                return "ERRO: arquivo não existe: " + path;

            string extension = Path.GetExtension(file);
            if (!string.IsNullOrEmpty(extension) && !TextExtensions.Contains(extension))
                return "ERRO: tipo não textual; o Agente não analisa este arquivo: " + path;

            await using FileStream stream = new(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            char[] buffer = new char[MaxCharacters + 1];
            int count = 0;
            while (count < buffer.Length)
            {
                int read = await reader.ReadAsync(buffer.AsMemory(count, buffer.Length - count), ct).ConfigureAwait(false);
                if (read == 0) break;
                count += read;
            }

            string content = new(buffer, 0, Math.Min(count, MaxCharacters));
            if (content.IndexOf('\0') >= 0)
                return "ERRO: conteúdo binário detectado; o Agente não analisa este arquivo: " + path;

            if (count > MaxCharacters)
                content += "\n... (truncado: arquivo excede 40.000 caracteres)";
            return content;
        }
    }

    public sealed class WriteFileTool : WorkspaceAgentTool
    {
        public WriteFileTool(string workspaceRoot) : base(workspaceRoot)
        {
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "write_file",
            Description = "Cria ou sobrescreve um arquivo do workspace com o conteúdo informado.",
            Parameters =
            {
                ["path"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Caminho relativo ao workspace."
                },
                ["content"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Conteúdo completo a gravar no arquivo."
                }
            },
            Required = { "path", "content" }
        };

        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string path;
            string content;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                JsonElement root = doc.RootElement;
                path = ReadString(root, "path") ?? string.Empty;
                content = ReadString(root, "content") ?? string.Empty;
            }

            string file = ResolvePath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? WorkspaceRoot);
            ct.ThrowIfCancellationRequested();
            File.WriteAllText(file, content);
            return Task.FromResult("OK: arquivo gravado (" + content.Length + " chars): " + path);
        }
    }

    public sealed class EditFileTool : WorkspaceAgentTool
    {
        public EditFileTool(string workspaceRoot) : base(workspaceRoot)
        {
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "edit_file",
            Description = "Substitui a primeira ocorrência de um trecho exato em um arquivo do workspace.",
            Parameters =
            {
                ["path"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Caminho relativo ao workspace."
                },
                ["old_text"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Trecho exato a ser substituído."
                },
                ["new_text"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Novo trecho que substitui old_text."
                }
            },
            Required = { "path", "old_text", "new_text" }
        };

        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string path;
            string oldText;
            string newText;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                JsonElement root = doc.RootElement;
                path = ReadString(root, "path") ?? string.Empty;
                oldText = ReadString(root, "old_text") ?? string.Empty;
                newText = ReadString(root, "new_text") ?? string.Empty;
            }

            if (string.IsNullOrEmpty(oldText))
            {
                return Task.FromResult("ERRO: old_text não pode ser vazio.");
            }

            string file = ResolvePath(path);
            if (!File.Exists(file))
            {
                return Task.FromResult("ERRO: arquivo não existe: " + path);
            }

            ct.ThrowIfCancellationRequested();
            string current = File.ReadAllText(file);
            int index = current.IndexOf(oldText, StringComparison.Ordinal);
            if (index < 0)
            {
                return Task.FromResult("ERRO: old_text não encontrado no arquivo: " + path);
            }

            string updated = current.Substring(0, index) + newText +
                             current.Substring(index + oldText.Length);
            File.WriteAllText(file, updated);
            return Task.FromResult("OK: substituída 1 ocorrência em " + path +
                " (" + newText.Length + " chars no lugar de " + oldText.Length + ").");
        }
    }
}
