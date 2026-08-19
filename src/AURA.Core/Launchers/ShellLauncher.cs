using System;
using System.IO;

namespace AURA.Core.Launchers
{
    public sealed class ShellLauncher : ILauncher
    {
        private static readonly string[] Extensions = { ".sh", ".bash" };

        public string[] SupportedExtensions => Extensions;

        public bool Supports(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            string extension = Path.GetExtension(filePath);
            return Array.Exists(Extensions, e => string.Equals(e, extension, StringComparison.OrdinalIgnoreCase));
        }

        public CellCommand BuildCommand(string filePath, string arguments)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("O caminho do script não pode ser vazio.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Script não encontrado.", filePath);

            string shell = FindShell();
            string script = Quote(filePath);
            string suffix = string.IsNullOrWhiteSpace(arguments) ? string.Empty : " " + arguments.Trim();

            return new CellCommand(shell, script + suffix);
        }

        private static string FindShell()
        {
            if (!OperatingSystem.IsWindows())
            {
                foreach (string candidate in new[] { "/system/bin/sh", "/bin/sh", "sh", "bash" })
                {
                    string resolved = candidate.StartsWith("/", StringComparison.Ordinal)
                        ? (File.Exists(candidate) ? candidate : null)
                        : FindOnPath(candidate);

                    if (!string.IsNullOrWhiteSpace(resolved))
                        return resolved;
                }
            }
            else
            {
                foreach (string candidate in new[] { "bash", "sh" })
                {
                    string resolved = FindOnPath(candidate);
                    if (!string.IsNullOrWhiteSpace(resolved))
                        return resolved;
                }
            }

            throw new PlatformNotSupportedException(
                "Nenhum interpretador de shell compatível foi encontrado no ambiente atual.");
        }

        private static string FindOnPath(string name)
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = Path.Combine(directory, name);
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
        }
    }
}
