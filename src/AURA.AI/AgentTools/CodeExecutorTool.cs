using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions.Execution;

namespace AURA.AI
{
    /// <summary>
    /// Executa o código extraído (python/bash) num arquivo temporário do
    /// workspace e retorna o resultado.
    /// </summary>
    public sealed class CodeExecutorTool : AgentTool
    {
        private readonly IToolExecutor _shell;
        private readonly string _workspace;

        public CodeExecutorTool(IToolExecutor shell, string workspace)
        {
            _shell = shell ?? throw new ArgumentNullException(nameof(shell));
            _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "execute_code",
            Description = "Executa o código extraído e retorna o resultado.",
            Parameters =
            {
                ["code"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Código a ser executado"
                },
                ["language"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Linguagem do código (python, bash, csharp)"
                }
            },
            Required = { "code", "language" }
        };

        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string code = "";
            string language = "python";
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("code", out var c)) code = c.GetString() ?? "";
                if (root.TryGetProperty("language", out var l)) language = l.GetString() ?? "python";
            }

            if (string.IsNullOrWhiteSpace(code))
                return "ERRO: código vazio.";

            string extension = language switch
            {
                "bash" or "sh" => "sh",
                "csharp" => "cs",
                _ => "py"
            };
            string fileName = $"code_{Guid.NewGuid():N}.{extension}";
            string filePath = Path.Combine(_workspace, fileName);
            await File.WriteAllTextAsync(filePath, code, ct);

            string command = language switch
            {
                "bash" or "sh" => $"/bin/sh {fileName}",
                "csharp" => $"dotnet run {fileName}",
                _ => $"python3 {fileName}"
            };

            var request = new ExecutionRequest
            {
                Command = command,
                WorkingDirectory = _workspace,
                Timeout = TimeSpan.FromSeconds(60)
            };

            var result = await _shell.ExecuteAsync(request, ct);
            try { File.Delete(filePath); } catch { }

            string output = result.CombineOutput();
            return result.Success
                ? $"Código executado com sucesso!\n\nSaída:\n{output}"
                : $"Falha na execução (exit {result.ExitCode})\n\n{output}";
        }
    }
}