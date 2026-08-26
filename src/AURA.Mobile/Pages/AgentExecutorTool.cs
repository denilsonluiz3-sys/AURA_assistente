using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions.Execution;
using AURA.AI;
using AURA.Mobile.Diagnostics;
using AURA.Modules.Executors;

namespace AURA.Mobile.Pages;

public sealed class AgentExecutorTool : AgentTool
{
    private readonly Dictionary<string, IToolExecutor> _executors;

    public AgentExecutorTool(ShellExecutor shell, GitExecutor git, PythonExecutor python, NodeExecutor node)
    {
        _executors = new Dictionary<string, IToolExecutor>(StringComparer.OrdinalIgnoreCase)
        {
            ["shell"] = shell,
            ["git"] = git,
            ["python"] = python,
            ["node"] = node
        };
    }

    public override AgentToolDefinition Definition => new AgentToolDefinition
    {
        Name = "run_executor",
        Description = "Executa um comando usando um executor específico do app. " +
                     "Use 'shell' para comandos gerais, 'git', 'python' ou 'node'.",
        Parameters =
        {
            ["executor"] = new AgentToolParameter
            {
                Type = "string",
                Description = "Nome do executor: shell, git, python ou node."
            },
            ["command"] = new AgentToolParameter
            {
                Type = "string",
                Description = "Comando ou script a executar."
            },
            ["args"] = new AgentToolParameter
            {
                Type = "string",
                Description = "Argumentos extras (separados por espaço)."
            }
        },
        Required = { "executor", "command" }
    };

    public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
    {
        string executor, command, args;
        using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
        {
            JsonElement root = doc.RootElement;
            executor = ReadString(root, "executor") ?? "shell";
            command = ReadString(root, "command") ?? string.Empty;
            args = ReadString(root, "args") ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(command))
            return "ERRO: comando vazio.";

        if (!_executors.TryGetValue(executor, out IToolExecutor? tool) || !tool.IsAvailable())
            return $"ERRO: executor '{executor}' não disponível.";

        var request = new ExecutionRequest
        {
            Command = command,
            Arguments = string.IsNullOrWhiteSpace(args)
                ? new List<string>()
                : new List<string>(args.Split(' ', StringSplitOptions.RemoveEmptyEntries)),
            WorkingDirectory = AgentWorkspace.ActiveRoot,
            Timeout = TimeSpan.FromSeconds(60)
        };

        ExecutionResult result = await tool.ExecuteAsync(request, ct).ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.Append("exit=").Append(result.ExitCode).Append('\n');
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            sb.AppendLine(result.StandardOutput.TrimEnd());
        if (!string.IsNullOrWhiteSpace(result.StandardError))
            sb.Append("stderr: ").AppendLine(result.StandardError.TrimEnd());
        if (sb.Length <= 6) sb.AppendLine("(sem saída)");

        string output = sb.ToString().TrimEnd();
        return output.Length > 8000 ? output[..8000] + "\n... (truncado)" : output;
    }
}
