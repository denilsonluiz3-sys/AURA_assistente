using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.AI
{
    /// <summary>
    /// Executa comandos shell (sh -c) dentro do workspace. Usado para git,
    /// dotnet, grep e qualquer utilitário disponível no dispositivo.
    /// </summary>
    public sealed class ShellAgentTool : AgentTool
    {
        private const int DefaultTimeoutSeconds = 30;
        private const int MaxOutputChars = 30000;

        private readonly string _workspaceRoot;
        private readonly string _shellPath;

        public ShellAgentTool(string workspaceRoot)
        {
            _workspaceRoot = workspaceRoot;
            _shellPath = File.Exists("/system/bin/sh") ? "/system/bin/sh" : "/bin/sh";
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "run_shell",
            Description = "Executa um comando shell (sh -c) no diretório do workspace. " +
                "Use para git status/add/commit, dotnet build, grep, ls, etc. Timeout de 30s.",
            Parameters =
            {
                ["command"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Comando shell completo (ex.: 'git status --short')."
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

            if (!File.Exists(_shellPath))
            {
                return "ERRO: shell não encontrado neste dispositivo (" + _shellPath + ").";
            }

            var psi = new ProcessStartInfo
            {
                FileName = _shellPath,
                Arguments = "-c \"" + command.Replace("\"", "\\\"") + "\"",
                WorkingDirectory = _workspaceRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(DefaultTimeoutSeconds));
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* já encerrado */ }
                return "ERRO: comando cancelado (timeout de " + DefaultTimeoutSeconds + "s).";
            }
            catch (Exception ex)
            {
                return "ERRO ao iniciar comando: " + ex.Message;
            }

            var result = new StringBuilder();
            if (stdout.Length > 0)
            {
                result.AppendLine(stdout.ToString().TrimEnd());
            }

            if (stderr.Length > 0)
            {
                result.Append("stderr: ").AppendLine(stderr.ToString().TrimEnd());
            }

            if (result.Length == 0)
            {
                result.AppendLine("(sem saída)");
            }

            string output = result.ToString().TrimEnd();
            if (output.Length > MaxOutputChars)
            {
                output = output.Substring(0, MaxOutputChars) +
                         "\n... (saída truncada: " + result.Length + " chars)";
            }

            return "exit=" + process.ExitCode + "\n" + output;
        }
    }
}
