using System;
using System.Collections.Generic;

namespace AURA.Agents
{
    public sealed record IntentResult(
        string Intent,
        double Confidence,
        Dictionary<string, string> Parameters);

    public interface IIntentResolver
    {
        IntentResult Resolve(string normalizedCommand);
    }

    /// <summary>
    /// Primeiro resolvedor local da AURA: determinístico, leve e sem modelo externo.
    /// </summary>
    public sealed class HeuristicIntentResolver : IIntentResolver
    {
        public IntentResult Resolve(string normalizedCommand)
        {
            string command = normalizedCommand ?? string.Empty;

            if (ContainsAny(command, "pesquise", "busque", "procure", "search"))
                return Result("search", 0.95, command, "pesquise", "busque", "procure", "search");

            if (ContainsAny(command, "execute", "rode", "rodar", "run ") || HasKnownScript(command))
                return Result("execute", 0.90, command, "execute", "rode", "rodar", "run ");

            if (ContainsAny(command, "crie", "criar arquivo", "novo arquivo"))
                return Result("create_file", 0.90, command, "crie", "criar arquivo", "novo arquivo");

            if (command == "ls" || command.StartsWith("ls ", StringComparison.Ordinal) ||
                ContainsAny(command, "liste", "listar", "diretórios", "diretorios"))
                return Result("list_files", 0.90, command, "liste", "listar", "ls");

            if (ContainsAny(command, "bateria", "battery"))
                return Result("android_battery", 0.85, command, "bateria", "battery");

            if (ContainsAny(command, "sensor", "luz", "acelerometro", "acelerômetro"))
                return Result("android_sensor", 0.80, command, "sensor", "luz", "acelerometro", "acelerômetro");

            if (ContainsAny(command, "gps", "localização", "localizacao"))
                return Result("android_location", 0.80, command, "gps", "localização", "localizacao");

            if (ContainsAny(command, "camera", "câmera"))
                return Result("android_camera", 0.80, command, "camera", "câmera");

            return new IntentResult("conversar", 0.50, new Dictionary<string, string>());
        }

        private static IntentResult Result(string intent, double confidence, string command, params string[] triggers)
        {
            foreach (string trigger in triggers)
            {
                int index = command.IndexOf(trigger, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    string value = command.Substring(index + trigger.Length).Trim();
                    var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    if (!string.IsNullOrWhiteSpace(value)) parameters["query"] = value;
                    return new IntentResult(intent, confidence, parameters);
                }
            }

            return new IntentResult(intent, confidence, new Dictionary<string, string>());
        }

        private static bool ContainsAny(string value, params string[] terms)
        {
            foreach (string term in terms)
                if (value.Contains(term, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool HasKnownScript(string value) =>
            value.EndsWith(".py", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".sh", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            value.EndsWith(".bash", StringComparison.OrdinalIgnoreCase);
    }
}
