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
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            string ext = Path.GetExtension(filePath);
            return Array.IndexOf(Extensions, ext) >= 0
                || Array.IndexOf(Extensions, ext.ToLowerInvariant()) >= 0;
        }

        public CellCommand BuildCommand(string filePath, string arguments)
        {
            string shell = FindShell();
            string args = "\"" + filePath + "\" " + (arguments ?? "").Trim();
            return new CellCommand(shell, args);
        }

        private static string FindShell()
        {
            foreach (string c in new[] { "/system/bin/sh", "/bin/sh", "sh", "bash" })
            {
                if (c.StartsWith("/") && File.Exists(c)) return c;
                string onPath = FindOnPath(c);
                if (onPath != null) return onPath;
            }
            return "/system/bin/sh";
        }

        private static string FindOnPath(string name)
        {
            string pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (string dir in pathVar.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                string full = Path.Combine(dir, name);
                if (File.Exists(full)) return full;
            }
            return null;
        }
    }
}
