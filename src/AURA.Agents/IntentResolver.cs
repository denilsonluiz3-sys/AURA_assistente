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

            // Programas AURA devem vencer a regra genérica de "dispositivo".
            if (ContainsAny(command,
                "diagnóstico do aparelho",
                "diagnostico do aparelho",
                "informações do dispositivo",
                "informacoes do dispositivo",
                "como está o aparelho",
                "como esta o aparelho"))
            {
                return AndroidResult(command, "device-diagnostic", 0.95,
                    "diagnóstico do aparelho",
                    "diagnostico do aparelho",
                    "informações do dispositivo",
                    "informacoes do dispositivo",
                    "como está o aparelho",
                    "como esta o aparelho");
            }

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
                return AndroidResult(command, "battery", 0.85, "bateria", "battery");

            if (ContainsAny(command, "sensor", "luz", "acelerometro", "acelerômetro", "giroscopio", "giroscópio", "magnetometro", "magnetômetro"))
                return AndroidResult(command, "sensor", 0.80, "sensor", "luz", "acelerometro", "acelerômetro", "giroscopio", "giroscópio", "magnetometro", "magnetômetro");

            if (ContainsAny(command, "gps", "localização", "localizacao"))
                return AndroidResult(command, "location", 0.80, "gps", "localização", "localizacao");

            if (ContainsAny(command, "camera", "câmera"))
                return AndroidResult(command, "camera", 0.80, "camera", "câmera");

            if (ContainsAny(command, "bluetooth"))
                return AndroidResult(command, "bluetooth", 0.80, "bluetooth");

            if (ContainsAny(command, "clipboard", "área de transferência", "area de transferencia"))
                return AndroidResult(command, "clipboard", 0.80, "clipboard", "área de transferência", "area de transferencia");

            if (ContainsAny(command, "memória do dispositivo", "memoria do dispositivo"))
                return AndroidResult(command, "memory", 0.80, "memória do dispositivo", "memoria do dispositivo");

            if (ContainsAny(command, "armazenamento", "storage"))
                return AndroidResult(command, "storage", 0.80, "armazenamento", "storage");

            if (ContainsAny(command, "aplicativos instalados", "apps instalados"))
                return AndroidResult(command, "apps", 0.80, "aplicativos instalados", "apps instalados");

            if (ContainsAny(command, "dispositivo", "device", "propriedades android", "properties"))
                return AndroidResult(command, "device", 0.75, "dispositivo", "device", "propriedades android", "properties");

            return new IntentResult("conversar", 0.50, new Dictionary<string, string>());
        }

        private static IntentResult AndroidResult(string command, string action, double confidence, params string[] triggers)
        {
            var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["action"] = action
            };

            foreach (string trigger in triggers)
            {
                int index = command.IndexOf(trigger, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    string value = command.Substring(index + trigger.Length).Trim();
                    if (!string.IsNullOrWhiteSpace(value)) parameters["query"] = value;
                    break;
                }
            }

            return new IntentResult("android", confidence, parameters);
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
