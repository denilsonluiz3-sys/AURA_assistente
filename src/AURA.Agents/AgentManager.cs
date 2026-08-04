using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Logging;
using AURA.Core.Runtime;

namespace AURA.Agents
{
    /// <summary>
    /// Info about an available assistant tool (aichat, termux-ai, ...).
    /// "Available" means its executable can be resolved on PATH or ~/bin.
    /// </summary>
    public sealed class AgentInfo
    {
        public string Name { get; set; }

        public string Executable { get; set; }

        public string Description { get; set; }

        public override string ToString()
        {
            return Name + " -> " + Executable + (Description == null ? "" : " (" + Description + ")");
        }
    }

    /// <summary>
    /// F3: orchestrates the LLM assistants (aichat / termux-ai) as ordinary
    /// AURA apps. Provides one-shot questions (<c>aura ask</c>) and persistent
    /// assistant cells (<c>aura run aichat --cell chat</c>), all inside a cell
    /// so isolation, logging and persistence apply uniformly.
    /// </summary>
    public sealed class AgentManager
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyList<AgentInfo> _assistants;

        public AgentManager(ILogger logger)
            : this(logger, new AgentInfo[]
            {
                new AgentInfo
                {
                    Name = "aichat",
                    Description = "aichat CLI (OpenRouter)",
                    Executable = ResolveExecutable("aichat")
                },
                new AgentInfo
                {
                    Name = "termux-ai",
                    Description = "termux-ai (Python, on-device)",
                    Executable = ResolveExecutable("termux-ai")
                }
            })
        {
        }

        public AgentManager(ILogger logger, IEnumerable<AgentInfo> assistants)
        {
            _logger = logger ?? new ConsoleLogger();
            _assistants = (assistants ?? Array.Empty<AgentInfo>()).ToList();
        }

        public IReadOnlyList<AgentInfo> Assistants => _assistants;

        public IReadOnlyList<AgentInfo> AvailableAssistants()
        {
            System.Collections.Generic.List<AgentInfo> available =
                new System.Collections.Generic.List<AgentInfo>();
            foreach (AgentInfo a in _assistants)
            {
                if (a.Executable != null && File.Exists(a.Executable))
                {
                    available.Add(a);
                }
            }

            return available;
        }

        public AgentInfo Resolve(string name)
        {
            foreach (AgentInfo a in _assistants)
            {
                if (a.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return a;
                }
            }

            return null;
        }

        /// <summary>
        /// Runs a one-shot question through an assistant, inside a fresh cell.
        /// Returns the assistant's answer (from the cell log).
        /// </summary>
        public async Task<string> AskAsync(SimulationRuntime runtime, string question,
            string assistantName = "aichat", string cellId = null)
        {
            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }

            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("A pergunta não pode ser vazia.", nameof(question));
            }

            AgentInfo assistant = Resolve(assistantName);
            if (assistant == null)
            {
                throw new InvalidOperationException("Assistente desconhecido: " + assistantName);
            }

            if (assistant.Executable == null || !File.Exists(assistant.Executable))
            {
                throw new InvalidOperationException(
                    "Assistente '" + assistantName + "' não está disponível. " +
                    "Instale-o ou rode: bash scripts/migrar-ferramentas.sh");
            }

            if (string.IsNullOrWhiteSpace(cellId))
            {
                cellId = "ask-" + Guid.NewGuid().ToString("N").Substring(0, 6);
            }

            Definition definition = BuildDefinition(assistant);
            Cell cell = runtime.CreateCell(cellId,
                definition.FileName, definition.Arguments + " \"" + EscapeArg(question) + "\"",
                templatePath: null, workingDirectory: Path.GetDirectoryName(assistant.Executable));

            await runtime.StartCellAsync(cell.Id);

            // aichat exit-0 when the answer is produced; recycle() would restart
            // on crash, so stop recycling to keep the answer in place.
            await WaitFinishedAsync(runtime, cell);

            string log = runtime.ReadCellLog(cell.Id);

            _logger.Info("ask: assistente='" + assistant.Name + "' célula='" + cell.Id + "'");
            return log;
        }

        /// <summary>
        /// Starts a long-lived assistant cell (interactive/session mode).
        /// Returns the created cell; the assistant keeps running as a normal app.
        /// </summary>
        public Cell StartAssistantCell(SimulationRuntime runtime, string id, string assistantName = "aichat")
        {
            if (runtime == null)
            {
                throw new ArgumentNullException(nameof(runtime));
            }

            AgentInfo assistant = Resolve(assistantName);
            if (assistant == null)
            {
                throw new InvalidOperationException("Assistente desconhecido: " + assistantName);
            }

            if (assistant.Executable == null || !File.Exists(assistant.Executable))
            {
                throw new InvalidOperationException(
                    "Assistente '" + assistantName + "' não está disponível. " +
                    "Instale-o ou rode: bash scripts/migrar-ferramentas.sh");
            }

            Definition definition = BuildDefinition(assistant);

            // aichat uses -s <session> to keep conversational state across asks.
            Cell cell = runtime.CreateCell(id, definition.FileName,
                definition.SessionArgs, templatePath: null,
                workingDirectory: Path.GetDirectoryName(assistant.Executable));

            return cell;
        }

        private static async Task WaitFinishedAsync(SimulationRuntime runtime, Cell cell)
        {
            for (int i = 0; i < 300; i++)
            {
                if (cell.State != CellState.Running && cell.State != CellState.Paused)
                {
                    return;
                }

                await Task.Delay(200);
            }
        }

        private Definition BuildDefinition(AgentInfo assistant)
        {
            if (assistant.Name.Equals("aichat", StringComparison.OrdinalIgnoreCase))
            {
                // aichat "<question>" answers one-shot; -s keeps a session.
                return new Definition(assistant.Executable, string.Empty, "-s aura-ask");
            }

            // termux-ai: default behavior answers a prompt.
            return new Definition(assistant.Executable, string.Empty, string.Empty);
        }

        private sealed class Definition
        {
            public Definition(string fileName, string arguments, string sessionArgs)
            {
                FileName = fileName;
                Arguments = arguments;
                SessionArgs = sessionArgs;
            }

            public string FileName { get; }

            public string Arguments { get; }

            public string SessionArgs { get; }
        }

        private string EscapeArg(string value)
        {
            return value.Replace("\"", "\\\"");
        }

        /// <summary>Finds an executable on PATH or ~/bin (mirror of PythonLauncher).</summary>
        public static string ResolveExecutable(string name)
        {
            string directly = FindOnPath(name);
            if (directly != null)
            {
                return directly;
            }

            string homeBin = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "bin", name);
            return File.Exists(homeBin) ? homeBin : null;
        }

        private static string FindOnPath(string name)
        {
            string pathVar = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathVar))
            {
                return null;
            }

            foreach (string dir in pathVar.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir))
                {
                    continue;
                }

                string candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
