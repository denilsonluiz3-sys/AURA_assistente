using System;
using System.Linq;
using AURA.Core;
using AURA.Core.Bootstrap;
using AURA.Core.Launchers;
using AURA.Core.Logging;
using AURA.Core.Runtime;
using AURA.Modules;
using AURA.Network;
using AURA.SystemInfo;

namespace AURA.CLI
{
    /// <summary>
    /// A text-mode front-end for AURA. The user picks a program; AURA decides
    /// how to run it (launcher resolution) and inside which cell (isolation).
    /// </summary>
    internal class Program
    {
        private static SimulationRuntime _runtime;
        private static Runner _runner;
        private static PluginWatcher _pluginWatcher;
        private static ILogger _logger;

        private static void Main(string[] args)
        {
            try
            {
                Console.Title = VersionInfo.FullName;
            }
            catch
            {
                // Console.Title só existe no Windows; ignora no Linux/Termux.
            }

            Console.WriteLine("=================================");
            Console.WriteLine("        AURA ORCHESTRATOR");
            Console.WriteLine("=================================");
            Console.WriteLine();

            var bootstrap = new AuraBootstrap();
            bootstrap.Start();

            _logger = bootstrap.Logger;
            _runtime = new SimulationRuntime(_logger);
            _pluginWatcher = new PluginWatcher(_logger);
            _runner = new Runner(_pluginWatcher.Launchers.Concat(
                new ILauncher[] { new PythonLauncher(), new JavaLauncher(), new DotnetLauncher() }));

            _runtime.LoadFromStoreAsync().GetAwaiter().GetResult();

            PrintHelp();
            RunLoop(args);

            _pluginWatcher.Dispose();
            _runtime.Dispose();
        }

        private static void RunLoop(string[] args)
        {
            if (args.Length > 0)
            {
                RunCommand(string.Join(" ", args));
                return;
            }

            while (true)
            {
                Console.Write("AURA> ");
                string input = Console.ReadLine();

                if (input == null)
                {
                    // EOF (pipe fechado / modo não-interativo): sai limpo.
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                string cmd = input.Trim();

                if (cmd.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    cmd.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                RunCommand(cmd);
            }
        }

        private static void RunCommand(string cmd)
        {
            string[] parts = cmd.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            string verb = parts.Length > 0 ? parts[0].ToLowerInvariant() : string.Empty;

            try
            {
                switch (verb)
                {
                    case "diagnostico":
                    case "diag":
                        PrintDiagnostics();
                        break;
                    case "internet":
                        PrintNetwork();
                        break;
                    case "modulos":
                        PrintModules();
                        break;
                    case "launchers":
                        PrintLaunchers();
                        break;
                    case "plugins":
                        PrintPlugins();
                        break;
                    case "run":
                        RunFile(parts);
                        break;
                    case "cells":
                        PrintCells();
                        break;
                    case "persist":
                    case "save":
                        Console.WriteLine("Células persistidas em: " + _runtime.PersistNow());
                        break;
                    case "cell":
                        CellCommand(parts);
                        break;
                    case "ajuda":
                    case "help":
                        PrintHelp();
                        break;
                    default:
                        Console.WriteLine("Comando desconhecido: " + verb);
                        Console.WriteLine("Digite 'ajuda' para ver os comandos.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
                _logger.Error(ex.ToString());
            }
        }

        private static void RunFile(string[] parts)
        {
            if (parts.Length < 2)
            {
                Console.WriteLine("Uso: run <arquivo> [argumentos...] [--cell <id>]");
                return;
            }

            string filePath = parts[1];
            string cellId = null;
            var appArgs = new System.Collections.Generic.List<string>();
            var limits = new ResourceLimits();

            for (int i = 2; i < parts.Length; i++)
            {
                if (parts[i] == "--cell" && i + 1 < parts.Length)
                {
                    cellId = parts[++i];
                }
                else if (TryParseLimit(parts, ref i, limits))
                {
                    continue;
                }
                else
                {
                    appArgs.Add(parts[i]);
                }
            }

            string arguments = appArgs.Count > 0 ? string.Join(" ", appArgs) : null;

            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine("Arquivo não encontrado: " + filePath);
                return;
            }

            Cell cell = _runner.RunAsync(_runtime, cellId, filePath, arguments, null, limits.IsEmpty ? null : limits).GetAwaiter().GetResult();

            Console.WriteLine("Célula criada e iniciada:");
            Console.WriteLine("  id     : " + cell.Id);
            Console.WriteLine("  comando: " + cell.AppPath + " " + cell.Args);
            Console.WriteLine("  pid    : " + cell.ProcessId);
            Console.WriteLine("  log    : " + cell.LogFile);
        }

        private static bool TryParseLimit(string[] parts, ref int i, ResourceLimits limits)
        {
            string token = parts[i].ToLowerInvariant();
            long? value = null;

            if (token == "--mem" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long mem))
            {
                value = mem;
                limits.MemoryLimitMb = value;
                i++;
            }
            else if (token == "--cpu" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long cpu))
            {
                value = cpu;
                limits.CpuLimitSeconds = value;
                i++;
            }
            else if (token == "--files" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long files))
            {
                value = files;
                limits.MaxFiles = value;
                i++;
            }
            else if (token == "--procs" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long procs))
            {
                value = procs;
                limits.MaxProcesses = value;
                i++;
            }
            else
            {
                return false;
            }

            return true;
        }

        private static void PrintCells()
        {
            Cell[] cells = _runtime.Cells.ToArray();

            if (cells.Length == 0)
            {
                Console.WriteLine("Nenhuma célula. Use 'run <arquivo>' para criar uma.");
                return;
            }

            Console.WriteLine("Células (" + _runtime.CellsRoot + "):");
            Console.WriteLine("{0,-24} {1,-10} {2,-8} {3}", "ID", "ESTADO", "PID", "APLICATIVO");

            foreach (Cell cell in cells)
            {
                Console.WriteLine("{0,-24} {1,-10} {2,-8} {3}",
                    cell.Id,
                    cell.State,
                    cell.ProcessId.HasValue ? cell.ProcessId.Value.ToString() : "-",
                    cell.AppPath);
            }
        }

        private static void CellCommand(string[] parts)
        {
            if (parts.Length < 3)
            {
                Console.WriteLine("Uso: cell <start|stop|pause|resume|delete|log|limits> <id>");
                return;
            }

            string action = parts[1].ToLowerInvariant();
            string id = parts[2];

            switch (action)
            {
                case "start":
                    _runtime.StartCellAsync(id).GetAwaiter().GetResult();
                    Console.WriteLine("Célula iniciada: " + id);
                    break;
                case "stop":
                    _runtime.StopCell(id);
                    break;
                case "pause":
                    _runtime.PauseCell(id);
                    break;
                case "resume":
                    _runtime.ResumeCell(id);
                    break;
                case "delete":
                    _runtime.DeleteCell(id);
                    break;
                case "log":
                    Console.WriteLine(_runtime.ReadCellLog(id));
                    break;
                case "limits":
                    SetLimits(id, parts);
                    break;
                default:
                    Console.WriteLine("Ação desconhecida: " + action);
                    break;
            }
        }

        private static void SetLimits(string id, string[] parts)
        {
            var limits = new ResourceLimits();

            for (int i = 3; i < parts.Length; i++)
            {
                int idx = i;
                if (TryParseLimit(parts, ref idx, limits))
                {
                    i = idx;
                }
            }

            _runtime.SetCellLimits(id, limits);
            Console.WriteLine("Limites aplicados na célula '" + id + "'.");
        }

        private static void PrintLaunchers()
        {
            Console.WriteLine("Launchers registrados (AURA decide como rodar):");
            foreach (ILauncher launcher in _runner.Launchers)
            {
                Console.WriteLine("  " + launcher.GetType().Name +
                    " -> " + string.Join(", ", launcher.SupportedExtensions));
            }
        }

        private static void PrintPlugins()
        {
            Console.WriteLine("Plugins (" + _pluginWatcher.PluginsRoot + "):");
            string[] paths = _pluginWatcher.PluginPaths.ToArray();
            if (paths.Length == 0)
            {
                Console.WriteLine("  (nenhum plugin .dll encontrado)");
                return;
            }

            foreach (string path in paths)
            {
                Console.WriteLine("  " + System.IO.Path.GetFileName(path));
            }

            Console.WriteLine("Launchers de plugins : " + _pluginWatcher.Launchers.Count);
            Console.WriteLine("Plugins IPlugin      : " + _pluginWatcher.Plugins.Count);
        }

        private static void PrintDiagnostics()
        {
            SystemDiagnosticsResult result = new SystemAnalyzer().Analyze();
            Console.WriteLine("Sistema operacional : " + result.OperatingSystem);
            Console.WriteLine("Arquitetura         : " + result.Architecture);
            Console.WriteLine("Processador          : " + result.ProcessorCount + " núcleos");
            Console.WriteLine("Memória              : " + result.AvailableMemoryGb + " GB livres de " + result.TotalMemoryGb + " GB");
            Console.WriteLine("Disco (" + result.SystemDrive + ")        : " + result.FreeDiskSpaceGb + " GB livres de " + result.TotalDiskSpaceGb + " GB");
        }

        private static void PrintNetwork()
        {
            NetworkStatus status = new NetworkManager().CheckConnection();
            Console.WriteLine("Rede local ativa  : " + (status.IsConnected ? "Sim" : "Não"));
            Console.WriteLine("Acesso à Internet : " + (status.HasInternetAccess ? "Sim" : "Não"));
            Console.WriteLine("IP local          : " + status.LocalIpAddress);
            Console.WriteLine("Status            : " + status.Message);
        }

        private static void PrintModules()
        {
            foreach (ModuleInfo module in ModuleCatalog.GetAll())
            {
                Console.WriteLine(module.Icon + " " + module.DisplayName + " - " + module.ShortDescription);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Comandos:");
            Console.WriteLine("  run <arquivo> [args]   Escolhe um programa; AURA decide como rodar");
            Console.WriteLine("  run --mem 256 --cpu 30 app.py   Aplica limites (prlimit) à célula");
            Console.WriteLine("  cells                   Lista as células");
            Console.WriteLine("  cell start|stop|pause|resume|delete|log|limits <id>");
            Console.WriteLine("  persist                 Salva o índice de células em disco");
            Console.WriteLine("  diagnostico             Diagnóstico do sistema");
            Console.WriteLine("  internet                Verifica conexão");
            Console.WriteLine("  modulos                 Lista módulos disponíveis");
            Console.WriteLine("  launchers               Lista resolutores de extensão");
            Console.WriteLine("  plugins                 Lista plugins carregados");
            Console.WriteLine("  ajuda                   Mostra esta ajuda");
            Console.WriteLine("  exit                    Sai");
            Console.WriteLine();
        }
    }
}
