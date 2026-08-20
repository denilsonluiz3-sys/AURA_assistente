using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.AI
{
    /// <summary>
    /// Interpreta o comando do usuário em linguagem natural e retorna uma
    /// estrutura JSON com intenção, alvo, objetivo e parâmetros.
    /// </summary>
    public sealed class InterpretCommandTool : AgentTool
    {
        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "interpret_command",
            Description = "Interpreta o comando do usuário em linguagem natural e retorna uma estrutura JSON com: " +
                         "intent (pesquisar|executar|criar|editar|analisar|configurar|listar|conversar), " +
                         "target (o que o usuário quer fazer), " +
                         "goal (objetivo específico), " +
                         "parameters (parâmetros adicionais).",
            Parameters =
            {
                ["command"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Comando completo do usuário em linguagem natural."
                }
            },
            Required = { "command" }
        };

        public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string command;
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                command = ReadString(doc.RootElement, "command") ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                return Task.FromResult("ERRO: comando vazio.");
            }

            var result = Interpret(command);
            string json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return Task.FromResult(json);
        }

        private static InterpretResult Interpret(string command)
        {
            string lower = command.ToLowerInvariant();
            var result = new InterpretResult();

            if (lower.Contains("pesquise") || lower.Contains("busque") ||
                lower.Contains("procure") || lower.Contains("o que é") ||
                lower.Contains("como ") || lower.Contains("search") ||
                lower.Contains("what is") || lower.Contains("how to"))
            {
                result.Intent = "pesquisar";
            }
            else if (lower.Contains("execute") || lower.Contains("rode") ||
                     lower.Contains("rodar") || lower.Contains("run ") ||
                     lower.Contains("executar"))
            {
                result.Intent = "executar";
            }
            else if (lower.Contains("crie") || lower.Contains("criar") ||
                     lower.Contains("novo") || lower.Contains("make") ||
                     lower.Contains("create"))
            {
                result.Intent = "criar";
            }
            else if (lower.Contains("edite") || lower.Contains("editar") ||
                     lower.Contains("modifique") || lower.Contains("edit") ||
                     lower.Contains("altere") || lower.Contains("modify"))
            {
                result.Intent = "editar";
            }
            else if (lower.Contains("analise") || lower.Contains("analisar") ||
                     lower.Contains("debug") || lower.Contains("diagnostico") ||
                     lower.Contains("check") || lower.Contains("verify"))
            {
                result.Intent = "analisar";
            }
            else if (lower.Contains("configure") || lower.Contains("configurar") ||
                     lower.Contains("set") || lower.Contains("config") ||
                     lower.Contains("setup"))
            {
                result.Intent = "configurar";
            }
            else if (lower.Contains("liste") || lower.Contains("listar") ||
                     lower.Contains("mostre") || lower.Contains("show") ||
                     lower.Contains("ls"))
            {
                result.Intent = "listar";
            }
            else
            {
                result.Intent = "conversar";
            }

            result.Target = ExtractTarget(command);
            result.Goal = command.Length > 100 ? command.Substring(0, 100) + "..." : command;
            ExtractParameters(command, result.Parameters);

            return result;
        }

        private static string ExtractTarget(string command)
        {
            string lower = command.ToLowerInvariant();

            if (lower.Contains(".py")) return "python";
            if (lower.Contains(".sh") || lower.Contains(".bash")) return "shell";
            if (lower.Contains(".js") || lower.Contains(".ts")) return "javascript";
            if (lower.Contains(".cs")) return "csharp";
            if (lower.Contains(".java")) return "java";
            if (lower.Contains(".go")) return "golang";
            if (lower.Contains(".dll") || lower.Contains(".exe")) return "dotnet";
            if (lower.Contains(".jar")) return "java_jar";

            string[] targets = {
                "arquivo", "pasta", "diretório", "código", "script", "programa",
                "comando", "sistema", "rede", "memória", "processo", "célula",
                "modulo", "agente", "ia", "dados", "configuração", "log"
            };

            foreach (string target in targets)
            {
                if (lower.Contains(target))
                    return target;
            }

            return "geral";
        }

        private static void ExtractParameters(string command, Dictionary<string, string> parameters)
        {
            var pathMatch = Regex.Match(command, @"([/\w.-]+\.\w+)");
            if (pathMatch.Success)
            {
                parameters["file_path"] = pathMatch.Groups[1].Value;
            }

            var numMatch = Regex.Match(command, @"\b(\d+)\b");
            if (numMatch.Success)
            {
                parameters["number"] = numMatch.Groups[1].Value;
            }

            var urlMatch = Regex.Match(command, @"https?://[^\s]+");
            if (urlMatch.Success)
            {
                parameters["url"] = urlMatch.Groups[0].Value;
            }
        }

        private class InterpretResult
        {
            public string Intent { get; set; } = "conversar";
            public string Target { get; set; } = "geral";
            public string Goal { get; set; } = string.Empty;
            public Dictionary<string, string> Parameters { get; set; } = new();
        }
    }
}