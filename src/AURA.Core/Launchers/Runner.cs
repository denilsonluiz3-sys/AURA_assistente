using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AURA.Core.Runtime;

namespace AURA.Core.Launchers
{
    /// <summary>
    /// "AURA decide como rodar": resolve launcher e sobe a célula.
    /// Sempre garante o conjunto padrão de launchers (nunca lista vazia).
    /// </summary>
    public sealed class Runner
    {
        private readonly IReadOnlyList<ILauncher> _launchers;

        public Runner()
            : this(CreateDefaultLaunchers())
        {
        }

        public Runner(IEnumerable<ILauncher> launchers)
        {
            var list = (launchers ?? Array.Empty<ILauncher>()).Where(l => l != null).ToList();
            if (list.Count == 0)
                list.AddRange(CreateDefaultLaunchers());
            _launchers = list;
        }

        private static List<ILauncher> CreateDefaultLaunchers() => new()
        {
            new PythonLauncher(),
            new ShellLauncher(),
            new JarLauncher(),
            new DllLauncher(),
            new NodeLauncher(),
            new GoLauncher(),
        };

        public IReadOnlyList<ILauncher> Launchers => _launchers;

        public bool CanRun(string filePath) => ResolveLauncher(filePath) != null;

        public ILauncher ResolveLauncher(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            foreach (ILauncher launcher in _launchers)
            {
                if (launcher.Supports(filePath))
                    return launcher;
            }
            return null;
        }

        public async System.Threading.Tasks.Task<Cell> RunAsync(
            SimulationRuntime runtime,
            string id,
            string filePath,
            string arguments = null,
            string templatePath = null,
            ResourceLimits? limits = null)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));

            ILauncher launcher = ResolveLauncher(filePath);
            if (launcher == null)
            {
                string supported = string.Join(", ", SupportedExtensions());
                throw new NotSupportedException(
                    "Nenhum launcher registrado para '" + filePath + "'. Extensões suportadas: " + supported);
            }

            CellCommand command = launcher.BuildCommand(filePath, arguments);

            if (string.IsNullOrWhiteSpace(id))
            {
                id = Path.GetFileNameWithoutExtension(filePath) + "-" +
                    Guid.NewGuid().ToString("N").Substring(0, 6);
            }

            Cell cell = runtime.CreateCell(
                id, command.FileName, command.Arguments,
                templatePath, Path.GetDirectoryName(filePath), limits);

            await runtime.StartCellAsync(cell.Id);
            return cell;
        }

        private IEnumerable<string> SupportedExtensions()
        {
            return _launchers.SelectMany(l => l.SupportedExtensions).Distinct().OrderBy(e => e);
        }
    }
}
