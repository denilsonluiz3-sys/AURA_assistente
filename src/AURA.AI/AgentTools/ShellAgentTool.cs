using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions.Execution;

namespace AURA.AI
{
    /// <summary>
    /// Adaptador cognitivo para execução de shell no workspace.
    /// No Android: /bin/sh (toybox) — sem apt/pip/git por padrão.
    /// </summary>
    public sealed class ShellAgentTool : AgentTool
    {
        private const int DefaultTimeoutSeconds = 30;
        private const int MaxOutputChars = 30000;

        private readonly string _workspaceRoot;
        private readonly IToolExecutor _executor;

        public ShellAgentTool(string workspaceRoot, IToolExecutor executor)
        {
            _workspaceRoot = workspaceRoot ?? throw new ArgumentNullException(nameof(workspaceRoot));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "run_shell",
            Description =
                "Executa um comando no /bin/sh do dispositivo (workspace). " +
                "Comandos úteis: ls, cat, grep, sed, find, echo, pwd, du, df, date, ps. " +
                "NÃO use apt, apt-get, yum, pip, npm, node, python3 ou git a menos que um comando anterior tenha confirmado que existem. " +
                "Se receber 'not found', pare de tentar instalar e use outra abordagem. Timeout 30s.",
            Parameters =
            {
                ["command"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Comando shell completo (ex.: 'ls -la' ou 'cat arquivo.txt')."
                }
            },
            Required = { "command" }
        };

        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string command;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                command = ReadString(doc.RootElement, "command") ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                return "ERRO: comando vazio.";
            }

            if (!_executor.IsAvailable())
            {
                return "ERRO: shell não encontrado neste dispositivo.";
            }

            var request = new ExecutionRequest
            {
                Command = command,
                WorkingDirectory = _workspaceRoot,
                Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds)
            };

            ExecutionResult result = await _executor.ExecuteAsync(request, ct).ConfigureAwait(false);
            return FormatForLlm(result);
        }

        public override async Task<AgentToolResult> ExecuteStructuredAsync(
            string argumentsJson, CancellationToken ct = default)
        {
            string command;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                command = ReadString(doc.RootElement, "command") ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                return AgentToolResult.Error("ERRO: comando vazio.");
            }

            if (!_executor.IsAvailable())
            {
                return AgentToolResult.Error("ERRO: shell não encontrado neste dispositivo.");
            }

            var request = new ExecutionRequest
            {
                Command = command,
                WorkingDirectory = _workspaceRoot,
                Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds)
            };

            ExecutionResult result = await _executor.ExecuteAsync(request, ct).ConfigureAwait(false);
            bool success = result != null && result.Success &&
                (result.StandardError?.IndexOf("[AURA] Execução cancelada", StringComparison.Ordinal) ?? -1) < 0;
            return success
                ? AgentToolResult.Ok(FormatForLlm(result))
                : AgentToolResult.Error(FormatForLlm(result));
        }

        public static string FormatForLlm(ExecutionResult result)
        {
            if (result == null)
            {
                return "ERRO: resultado de execução nulo.";
            }

            if (!string.IsNullOrWhiteSpace(result.StandardError) &&
                result.StandardError.IndexOf("[AURA] Execução cancelada", StringComparison.Ordinal) >= 0)
            {
                return "ERRO: comando cancelado (timeout de " + DefaultTimeoutSeconds + "s).";
            }

            var sb = new StringBuilder();
            sb.Append("exit=").Append(result.ExitCode).Append('\n');

            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                sb.AppendLine(result.StandardOutput.TrimEnd());
            }

            if (!string.IsNullOrWhiteSpace(result.StandardError))
            {
                sb.Append("stderr: ").AppendLine(result.StandardError.TrimEnd());
            }

            string header = "exit=" + result.ExitCode + "\n";
            if (sb.Length <= header.Length)
            {
                sb.AppendLine("(sem saída)");
            }

            string output = sb.ToString().TrimEnd();
            if (output.Length > MaxOutputChars)
            {
                output = output.Substring(0, MaxOutputChars) +
                         "\n... (saída truncada: " + sb.Length + " chars)";
            }

            return output;
        }
    }
}
