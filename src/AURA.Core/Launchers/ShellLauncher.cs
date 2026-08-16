using System;
using System.IO;

namespace AURA.Core.Launchers
{
    /// <summary>
    /// Runs shell scripts (.sh) inside a cell via "sh" no PATH (disponível
    /// out-of-the-box no Termux e em qualquer ambiente Linux/Android).
    /// </summary>
    public sealed class ShellLauncher : ILauncher
    {
        private static readonly string[] Extensions = { ".sh" };

        public string[] SupportedExtensions => Extensions;

        public bool Supports(string filePath)
        {
            return !string.IsNullOrWhiteSpace(filePath) &&
                Array.IndexOf(Extensions, Path.GetExtension(filePath)) >= 0;
        }

        public CellCommand BuildCommand(string filePath, string arguments)
        {
            // [ASSUMPTION: "sh" (não "bash") por ser o interpretador garantido
            // no Termux/Android sem dependências extras; scripts com bashismos
            // devem declarar #!/bin/bash e serem chamados via outro launcher
            // se isso virar um problema real.]
            return new CellCommand("sh", "\"" + filePath + "\" " + arguments);
        }
    }
}