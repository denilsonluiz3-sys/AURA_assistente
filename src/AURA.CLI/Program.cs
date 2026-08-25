using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using AURA.AI;
using AURA.Abstractions.Execution;
using AURA.Agents;
using AURA.Core;
using AURA.Core.Bootstrap;
using AURA.Core.Events;
using AURA.Core.Launchers;
using AURA.Core.Logging;
using AURA.Core.Runtime;
using AURA.Memory;
using AURA.Modules;
using AURA.Modules.Executors;
using AURA.Network;
using AURA.SystemInfo;

namespace AURA.CLI
{
    internal class Program
    {
        private static SimulationRuntime _runtime;
        private static Runner _runner;
        private static PluginWatcher _pluginWatcher;
        private static AgentManager _agentManager;
        private static ILogger _logger;
        private static AuraBootstrap _bootstrap;
        private static readonly ShellExecutor Shell = new();
        private static readonly GitExecutor Git = new();
        private static readonly PythonExecutor Python = new();
        private static readonly NodeExecutor Node = new();
        private static OpenRouterClient _aiClient;
        private static readonly HttpClient SharedHttpClient = new HttpClient();
        private static MemoryStore _memory;

        private static void Main(string[] args)
        {
            try { Console.Title = VersionInfo.FullName; } catch { }

            Console.WriteLine("=================================");
            Console.WriteLine("        AURA ORCHESTRATOR");
            Console.WriteLine("=================================");
            Console.WriteLine();

            var bootstrap = new AuraBootstrap();
            bootstrap.Start();
            _bootstrap = bootstrap;

            _logger = bootstrap.Logger;
            _runtime = new SimulationRuntime(_logger);
            _runtime.Events = bootstrap.Events;
            _pluginWatcher = new PluginWatcher(_logger);
            _runner = new Runner(_pluginWatcher.Launchers.Concat(
                new ILauncher[] { new PythonLauncher(), new JarLauncher(), new DllLauncher(), new NodeLauncher(), new GoLauncher() }));
            _memory = new MemoryStore(_logger);
            _agentManager = new AgentManager(_logger);
            _agentManager.Events = bootstrap.Events;

            bootstrap.Events.Subscribe<CellStateChangedEvent>(evt =>
                _logger.Info("[evento] célula " + evt.CellId + ": " + evt.From + " -> " + evt.To));
            bootstrap.Events.Subscribe<AssistantRespondedEvent>(evt =>
                _logger.Info("[evento] assistente " + evt.Assistant + " respondeu (célula " + evt.CellId + ")"));

            _runtime.LoadFromStoreAsync().GetAwaiter().GetResult();

            if (!bootstrap.Settings.FirstRunCompleted)
            {
                PrintWelcome();
                bootstrap.Settings.FirstRunCompleted = true;
                bootstrap.SaveSettings();
            }

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
                if (input == null) break;
                if (string.IsNullOrWhiteSpace(input)) continue;

                string cmd = input.Trim();
                if (cmd.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    cmd.Equals("quit", StringComparison.OrdinalIgnoreCase))
                    break;

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
                    case "config":
                        PrintConfig();
                        break;
                    case "launchers":
                        PrintLaunchers();
                        break;
                    case "plugins":
                        PrintPlugins();
                        break;
                    case "modelos":
                    case "models":
                        PrintModels();
                        break;
                    case "agents":
                        PrintAgents();
                        break;
                    case "ask":
                        Ask(parts);
                        break;
                    case "chat":
                        ChatCommand(parts);
                        break;
                    case "agent":
                        AgentCommand(parts);
                        break;
                    case "ensinar":
                    case "aprender":
                    case "professora":
                        EnsinarCommand(parts);
                        break;
                    case "aichave":
                        AiKeyCommand(parts);
                        break;
                    case "exec":
                        ExecCommand(parts);
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
                Console.WriteLine("Uso: run <arquivo> [argumentos...] [--cell <id>] [--wait]");
                return;
            }

            string filePath = parts[1];
            if (_agentManager.Resolve(filePath) != null)
            {
                RunAssistant(parts);
                return;
            }

            string cellId = null;
            var appArgs = new System.Collections.Generic.List<string>();
            var limits = new ResourceLimits();
            bool wait = false;

            for (int i = 2; i < parts.Length; i++)
            {
                if (parts[i] == "--cell" && i + 1 < parts.Length)
                    cellId = parts[++i];
                else if (parts[i] == "--wait")
                    wait = true;
                else if (TryParseLimit(parts, ref i, limits))
                    continue;
                else
                    appArgs.Add(parts[i]);
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

            if (wait)
            {
                _runtime.WaitCellAsync(cell.Id).GetAwaiter().GetResult();
                Console.WriteLine();
                Console.WriteLine("--- saída da célula ---");
                Console.WriteLine(_runtime.ReadCellLog(cell.Id));
            }
        }

        private static void RunAssistant(string[] parts)
        {
            string assistant = parts[1];
            string cellId = null;
            for (int i = 2; i < parts.Length; i++)
            {
                if (parts[i] == "--cell" && i + 1 < parts.Length)
                    cellId = parts[++i];
            }

            if (string.IsNullOrWhiteSpace(cellId))
                cellId = assistant;

            Cell cell = _agentManager.StartAssistantCell(_runtime, cellId, assistant);
            Console.WriteLine("Célula assistente criada (iniciar com 'cell start " + cell.Id + "'):");
            Console.WriteLine("  id     : " + cell.Id);
            Console.WriteLine("  app    : " + cell.AppPath);
            Console.WriteLine("  log    : " + cell.LogFile);
        }

        private static void PrintAgents()
        {
            Console.WriteLine("Assistentes configurados:");
            AgentInfo[] available = _agentManager.AvailableAssistants().ToArray();
            foreach (AgentInfo agent in _agentManager.Assistants)
            {
                bool ok = agent.Executable != null && System.IO.File.Exists(agent.Executable);
                Console.WriteLine("  " + (ok ? "[ok]   " : "[ausente] ") + agent);
            }

            if (available.Length == 0)
                Console.WriteLine("Nenhum assistente disponível. Rode: bash scripts/migrar-ferramentas.sh");
        }

        private static void PrintModels()
        {
            string current = Environment.GetEnvironmentVariable("AURA_MODEL") ?? "(não definido)";
            string provider = Environment.GetEnvironmentVariable("AURA_PROVIDER") ?? "(não definido)";
            string baseUrl = Environment.GetEnvironmentVariable("AURA_BASE_URL") ?? "(não definido)";
            Console.WriteLine("Configuração LLM (env):" );
            Console.WriteLine("  AURA_PROVIDER = " + provider);
            Console.WriteLine("  AURA_MODEL    = " + current);
            Console.WriteLine("  AURA_BASE_URL = " + baseUrl);
            Console.WriteLine();
            Console.WriteLine("Exemplos de modelo (você escolhe; nada é imposto):");
            Console.WriteLine("  nvidia/nemotron-3-ultra:free");
            Console.WriteLine("  openai/gpt-oss-20b:free");
            Console.WriteLine("  google/gemma-4-26b-a4b-it:free");
            Console.WriteLine();
            Console.WriteLine("Configure com: bash scripts/configurar-aura-llm.sh <modelo>");
            Console.WriteLine("Ou exporte AURA_PROVIDER / AURA_MODEL / AURA_BASE_URL.");
        }

        private static void ExecCommand(string[] parts)
        {
            if (parts.Length < 3)
            {
                Console.WriteLine("Uso: exec <shell|git|python|node> <comando> [argumentos...]");
                return;
            }

            IToolExecutor executor = parts[1].ToLowerInvariant() switch
            {
                "shell" => Shell,
                "git" => Git,
                "python" or "python3" or "py" => Python,
                "node" => Node,
                _ => null
            };

            if (executor == null)
            {
                Console.WriteLine("Executor desconhecido: " + parts[1] + " (use shell, git, python ou node)");
                return;
            }

            if (!executor.IsAvailable())
            {
                Console.WriteLine("Executor '" + executor.Name + "' não está disponível neste ambiente.");
                return;
            }

            var request = new ExecutionRequest
            {
                Command = parts[2],
                Arguments = parts.Skip(3).ToList(),
                Timeout = TimeSpan.FromSeconds(60)
            };

            Console.WriteLine("Executando via " + executor.Name + ": " + request.Command +
                (request.Arguments.Count > 0 ? " " + string.Join(" ", request.Arguments) : string.Empty));
            Console.WriteLine();

            ExecutionResult result = executor.ExecuteAsync(request).GetAwaiter().GetResult();
            string output = result.CombineOutput();
            Console.WriteLine(string.IsNullOrWhiteSpace(output) ? "(sem saída)" : output);
            Console.WriteLine();
            Console.WriteLine("== exit " + result.ExitCode + " (" + (result.Success ? "OK" : "FALHOU") + ") em " +
                result.Duration.TotalSeconds.ToString("0.0") + "s ==");
        }

        private static void ChatCommand(string[] parts)
        {
            string question = string.Join(" ", parts.Skip(1)).Trim();
            if (string.IsNullOrWhiteSpace(question))
            {
                Console.WriteLine("Uso: chat \"sua pergunta\" [--model <modelo>]");
                return;
            }

            string? model = null;
            int modelIdx = Array.IndexOf(parts, "--model");
            if (modelIdx >= 0 && modelIdx + 1 < parts.Length)
                model = parts[modelIdx + 1];

            OpenRouterClient client = EnsureAiClient(model);
            Console.WriteLine("Modelo: " + client.Options.Model + " · " + client.Options.BaseUrl);
            Console.WriteLine("Pensando...");

            try
            {
                string answer = client.ChatAsync(question, SharedHttpClient).GetAwaiter().GetResult();
                Console.WriteLine();
                Console.WriteLine(answer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
                _logger.Error(ex.ToString());
            }
        }

        private static void AgentCommand(string[] parts)
        {
            string instruction = string.Join(" ", parts.Skip(1)).Trim();
            if (string.IsNullOrWhiteSpace(instruction))
            {
                Console.WriteLine("Uso: agent \"instrução\" — agente de arquivos num workspace");
                return;
            }

            OpenRouterClient client = EnsureAiClient();
            string workspace = Path.Combine(UserAuraDir(), "workspace");
            Directory.CreateDirectory(workspace);

            var tools = new System.Collections.Generic.List<AgentTool>
            {
                new ListDirTool(workspace),
                new ReadFileTool(workspace),
                new WriteFileTool(workspace),
                new EditFileTool(workspace),
                new ShellAgentTool(workspace, Shell),
                new WebFetchTool()
            };

            string systemPrompt =
                "Você é o agente de arquivos da AURA. Workspace: " + workspace +
                ". Responda em português.";

            var session = new AgentSession(client, tools, systemPrompt, _logger, _memory);
            session.Step += step =>
            {
                Console.WriteLine();
                Console.WriteLine("  ◆ " + step.ToolName + " " + step.Arguments);
                if (!string.IsNullOrWhiteSpace(step.Result))
                    Console.WriteLine("    " + step.Result.Replace("\n", "\n    "));
            };

            Console.WriteLine("Modelo: " + client.Options.Model + " · workspace: " + workspace);
            Console.WriteLine("Executando agente...");

            try
            {
                string answer = session.RunAsync(instruction, SharedHttpClient).GetAwaiter().GetResult();
                Console.WriteLine();
                Console.WriteLine("=== RESPOSTA DO AGENTE ===");
                Console.WriteLine(answer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
                _logger.Error(ex.ToString());
            }
        }

        private static void EnsinarCommand(string[] parts)
        {
            string task = string.Join(" ", parts.Skip(1)).Trim();
            if (string.IsNullOrWhiteSpace(task))
            {
                Console.WriteLine("Uso: ensinar <descrição da tarefa>");
                return;
            }

            string workspace = Path.Combine(UserAuraDir(), "workspace");
            Directory.CreateDirectory(workspace);

            var webSearch = new WebSearchService();
            OpenRouterClient? client = null;
            if (!string.IsNullOrWhiteSpace(ReadAiKey()))
            {
                try { client = EnsureAiClient(); } catch { client = null; }
            }

            var tools = new System.Collections.Generic.List<AgentTool>
            {
                new InterpretCommandTool(),
                new SearchMemoryTool(new SolutionStore(_logger)),
                new WebSearchTool(webSearch),
                new CodeExtractorTool(webSearch, client),
                new CodeExecutorTool(Shell, workspace),
                new ShellAgentTool(workspace, Shell),
                new WebFetchTool()
            };

            string systemPrompt =
                "Você é a AURA Professora. Workspace: " + workspace;

            var session = new AgentSession(
                client ?? new OpenRouterClient(new OpenRouterOptions(), _logger),
                tools, systemPrompt, _logger, _memory);

            session.Step += step =>
            {
                Console.WriteLine();
                Console.WriteLine("  ◆ " + step.ToolName + " " + step.Arguments);
            };

            Console.WriteLine("AURA está aprendendo: " + task);

            try
            {
                string answer = session.RunAsync("Ensinar: " + task,
                    client != null ? SharedHttpClient : null).GetAwaiter().GetResult();
                _memory.Append(MemoryEntry.Answer(answer));
                Console.WriteLine();
                Console.WriteLine("=== RESULTADO ===");
                Console.WriteLine(answer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
                _logger.Error(ex.ToString());
            }
        }

        private static void AiKeyCommand(string[] parts)
        {
            if (parts.Length < 2)
            {
                Console.WriteLine("Uso: aichave <sk-or-...> — salva a chave em ~/.aura/ai_key.txt");
                return;
            }

            string key = parts[1].Trim();
            if (key.Length > 200 || key.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0)
            {
                Console.WriteLine("Chave inválida.");
                return;
            }

            string file = Path.Combine(UserAuraDir(), "ai_key.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, key);
            _aiClient = null;
            Console.WriteLine("Chave salva em " + file);
            Console.WriteLine("Cliente LLM recarregado.");
        }

        private static string UserAuraDir()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aura");
        }

        private static string ReadAiKey()
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                string keyFile = Path.Combine(UserAuraDir(), "ai_key.txt");
                if (File.Exists(keyFile))
                    apiKey = File.ReadAllText(keyFile).Trim();
            }
            return apiKey;
        }

        /// <summary>
        /// Provider/modelo/URL vêm de AURA_PROVIDER, AURA_MODEL, AURA_BASE_URL.
        /// Nenhum modelo padrão é imposto.
        /// </summary>
        private static OpenRouterClient EnsureAiClient(string? model = null)
        {
            string provider = Environment.GetEnvironmentVariable("AURA_PROVIDER") ?? string.Empty;
            string configuredModel = Environment.GetEnvironmentVariable("AURA_MODEL") ?? string.Empty;
            string baseUrl = Environment.GetEnvironmentVariable("AURA_BASE_URL") ?? string.Empty;
            string selectedModel = !string.IsNullOrWhiteSpace(model) ? model : configuredModel;

            if (string.IsNullOrWhiteSpace(provider))
                throw new InvalidOperationException("Provider LLM não configurado. Defina AURA_PROVIDER (ex.: openrouter).");
            if (string.IsNullOrWhiteSpace(selectedModel))
                throw new InvalidOperationException("Modelo LLM não configurado. Defina AURA_MODEL ou use --model.");
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Endpoint LLM não configurado. Defina AURA_BASE_URL.");

            string apiKey = ReadAiKey();

            if (_aiClient != null)
            {
                _aiClient.Options.Provider = provider;
                _aiClient.Options.BaseUrl = baseUrl;
                _aiClient.Options.Model = selectedModel;
                if (!string.IsNullOrWhiteSpace(apiKey)) _aiClient.Options.ApiKey = apiKey;
                return _aiClient;
            }

            _aiClient = new OpenRouterClient(new OpenRouterOptions
            {
                Provider = provider,
                ApiKey = apiKey,
                BaseUrl = baseUrl,
                Model = selectedModel,
                AppReference = "AURA CLI"
            }, _logger);

            return _aiClient;
        }

        private static void Ask(string[] parts)
        {
            if (parts.Length < 2)
            {
                Console.WriteLine("Uso: ask \"sua pergunta\" [--assistente aichat] [--cell <id>]");
                return;
            }

            var args = new System.Collections.Generic.List<string>();
            string assistant = "aichat";
            string cellId = null;

            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i] == "--assistente" && i + 1 < parts.Length)
                    assistant = parts[++i];
                else if (parts[i] == "--cell" && i + 1 < parts.Length)
                    cellId = parts[++i];
                else
                    args.Add(parts[i]);
            }

            string question = string.Join(" ", args);
            if (string.IsNullOrWhiteSpace(question))
            {
                Console.WriteLine("A pergunta não pode ser vazia.");
                return;
            }

            Console.WriteLine("Assistente '" + assistant + "' respondendo...");
            string answer = _agentManager.AskAsync(_runtime, question, assistant, cellId).GetAwaiter().GetResult();
            Console.WriteLine(answer);
        }

        private static bool TryParseLimit(string[] parts, ref int i, ResourceLimits limits)
        {
            string token = parts[i].ToLowerInvariant();
            if (token == "--mem" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long mem))
            { limits.MemoryLimitMb = mem; i++; return true; }
            if (token == "--cpu" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long cpu))
            { limits.CpuLimitSeconds = cpu; i++; return true; }
            if (token == "--files" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long files))
            { limits.MaxFiles = files; i++; return true; }
            if (token == "--procs" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long procs))
            { limits.MaxProcesses = procs; i++; return true; }
            return false;
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
                    cell.Id, cell.State,
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
                case "start": _runtime.StartCellAsync(id).GetAwaiter().GetResult(); Console.WriteLine("Célula iniciada: " + id); break;
                case "stop": _runtime.StopCell(id); break;
                case "pause": _runtime.PauseCell(id); break;
                case "resume": _runtime.ResumeCell(id); break;
                case "delete": _runtime.DeleteCell(id); break;
                case "log": Console.WriteLine(_runtime.ReadCellLog(id)); break;
                case "limits": SetLimits(id, parts); break;
                default: Console.WriteLine("Ação desconhecida: " + action); break;
            }
        }

        private static void SetLimits(string id, string[] parts)
        {
            var limits = new ResourceLimits();
            for (int i = 3; i < parts.Length; i++)
            {
                int idx = i;
                if (TryParseLimit(parts, ref idx, limits)) i = idx;
            }
            _runtime.SetCellLimits(id, limits);
            Console.WriteLine("Limites aplicados na célula '" + id + "'.");
        }

        private static void PrintLaunchers()
        {
            Console.WriteLine("Launchers registrados:");
            foreach (ILauncher launcher in _runner.Launchers)
                Console.WriteLine("  " + launcher.GetType().Name + " -> " + string.Join(", ", launcher.SupportedExtensions));
        }

        private static void PrintPlugins()
        {
            Console.WriteLine("Plugins (" + _pluginWatcher.PluginsRoot + "):");
            string[] paths = _pluginWatcher.PluginPaths.ToArray();
            if (paths.Length == 0) { Console.WriteLine("  (nenhum)"); return; }
            foreach (string path in paths)
                Console.WriteLine("  " + System.IO.Path.GetFileName(path));
        }

        private static void PrintDiagnostics()
        {
            SystemDiagnosticsResult result = new SystemAnalyzer().Analyze();
            Console.WriteLine("SO: " + result.OperatingSystem);
            Console.WriteLine("Arch: " + result.Architecture);
            Console.WriteLine("CPU: " + result.ProcessorCount);
            Console.WriteLine("RAM: " + result.AvailableMemoryGb + " / " + result.TotalMemoryGb + " GB");
        }

        private static void PrintNetwork()
        {
            NetworkStatus status = new NetworkManager().CheckConnection();
            Console.WriteLine("Rede: " + (status.IsConnected ? "Sim" : "Não"));
            Console.WriteLine("Internet: " + (status.HasInternetAccess ? "Sim" : "Não"));
            Console.WriteLine("IP: " + status.LocalIpAddress);
        }

        private static void PrintWelcome()
        {
            Console.WriteLine("Bem-vindo ao AURA Orchestrator!");
            Console.WriteLine();
        }

        private static void PrintConfig()
        {
            Console.WriteLine("Config (" + _bootstrap.SettingsPath + "):");
            Console.WriteLine("  Internet: " + _bootstrap.Settings.Internet);
            Console.WriteLine("  Theme: " + _bootstrap.Settings.Theme);
        }

        private static void PrintModules()
        {
            foreach (ModuleInfo module in ModuleCatalog.GetAll())
                Console.WriteLine(module.Icon + " " + module.DisplayName + " [" + module.Status + "]");
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Comandos:");
            Console.WriteLine("  run <arquivo> [args]   Executa programa em célula");
            Console.WriteLine("  cells / cell ...       Células");
            Console.WriteLine("  chat / agent / ensinar IA (requer AURA_PROVIDER/MODEL/BASE_URL)");
            Console.WriteLine("  modelos                Mostra AURA_PROVIDER / AURA_MODEL / AURA_BASE_URL");
            Console.WriteLine("  aichave <sk-or-...>    Salva chave");
            Console.WriteLine("  plugins / agents / ajuda");
            Console.WriteLine("  exit");
            Console.WriteLine();
        }
    }
}
