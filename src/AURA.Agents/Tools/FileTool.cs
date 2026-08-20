using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.Agents
{
    /// <summary>Operações básicas de arquivo limitadas a um workspace explícito.</summary>
    public sealed class FileTool : ITool
    {
        private readonly string _workspace;

        public FileTool(string workspace)
        {
            if (string.IsNullOrWhiteSpace(workspace))
                throw new ArgumentException("Workspace obrigatório.", nameof(workspace));

            _workspace = Path.GetFullPath(workspace);
            Directory.CreateDirectory(_workspace);
        }

        public string Intent => "file";

        public Task<ToolResult> ExecuteAsync(
            string command,
            Dictionary<string, string> parameters,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            string operation = parameters.TryGetValue("operation", out string? op) ? op : "list";
            string relativePath = parameters.TryGetValue("path", out string? path) ? path : string.Empty;

            try
            {
                return operation.ToLowerInvariant() switch
                {
                    "list" => Task.FromResult(ListFiles()),
                    "read" => Task.FromResult(ReadFile(relativePath)),
                    "write" => Task.FromResult(WriteFile(parameters)),
                    _ => Task.FromResult(new ToolResult(false, "Operação de arquivo desconhecida."))
                };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return Task.FromResult(new ToolResult(false, "Erro de arquivo: " + ex.Message));
            }
        }

        private ToolResult ListFiles()
        {
            string[] files = Directory.GetFiles(_workspace, "*", SearchOption.TopDirectoryOnly);
            return new ToolResult(true, files.Length == 0
                ? "Workspace vazio."
                : string.Join(Environment.NewLine, files));
        }

        private ToolResult ReadFile(string relativePath)
        {
            string path = ResolvePath(relativePath);
            if (!File.Exists(path)) return new ToolResult(false, "Arquivo não encontrado: " + relativePath);
            return new ToolResult(true, File.ReadAllText(path));
        }

        private ToolResult WriteFile(Dictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue("path", out string? relativePath) || string.IsNullOrWhiteSpace(relativePath))
                return new ToolResult(false, "Caminho do arquivo não informado.");

            string content = parameters.TryGetValue("content", out string? value) ? value : string.Empty;
            string path = ResolvePath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            return new ToolResult(true, "Arquivo escrito: " + path);
        }

        private string ResolvePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Caminho vazio.", nameof(relativePath));

            string full = Path.GetFullPath(Path.Combine(_workspace, relativePath));
            string root = _workspace.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Caminho fora do workspace.");

            return full;
        }
    }
}
