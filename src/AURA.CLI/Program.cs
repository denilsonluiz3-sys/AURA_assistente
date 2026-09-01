using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using AURA.AI;
using AURA.AI.UniversalAI;
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
    /// <summary>
    /// A text-mode front-end for AURA. The user picks a program; AURA decides
    /// how to run it (launcher resolution) and inside which cell (isolation).
    /// </summary>
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
            _runner = new Runner(_pluginWatcher.Launchers.Concat(new ILauncher[] { new PythonLauncher(), new JarLauncher(), new DllLauncher(), new NodeLauncher(), new GoLauncher() }));
            _memory = new MemoryStore(_logger);
            _agentManager = new AgentManager(_logger);
            _agentManager.Events = bootstrap.Events;
            bootstrap.Events.Subscribe<CellStateChangedEvent>(evt => _logger.Info("[evento] célula " + evt.CellId + ": " + evt.From + " -> " + evt.To));
            bootstrap.Events.Subscribe<AssistantRespondedEvent>(evt => _logger.Info("[evento] assistente " + evt.Assistant + " respondeu (célula " + evt.CellId + ")"));
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
            if (args.Length > 0) { RunCommand(string.Join(" ", args)); return; }
            while (true)
            {
                Console.Write("AURA> ");
                string input = Console.ReadLine();
                if (input == null) break;
                if (string.IsNullOrWhiteSpace(input)) continue;
                string cmd = input.Trim();
                if (cmd.Equals("exit", StringComparison.OrdinalIgnoreCase) || cmd.Equals("quit", StringComparison.OrdinalIgnoreCase)) break;
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
                    case "diagnostico": case "diag": PrintDiagnostics(); break;
                    case "internet": PrintNetwork(); break;
                    case "modulos": PrintModules(); break;
                    case "config": PrintConfig(); break;
                    case "launchers": PrintLaunchers(); break;
                    case "plugins": PrintPlugins(); break;
                    case "agents": PrintAgents(); break;
                    case "ask": Ask(parts); break;
                    case "chat": ChatCommand(parts); break;
                    case "agent": AgentCommand(parts); break;
                    case "ensinar": case "aprender": case "professora": EnsinarCommand(parts); break;
                    case "aichave": AiKeyCommand(parts); break;
                    case "exec": ExecCommand(parts); break;
                    case "run": RunFile(parts); break;
                    case "cells": PrintCells(); break;
                    case "persist": case "save": Console.WriteLine("Células persistidas em: " + _runtime.PersistNow()); break;
                    case "cell": CellCommand(parts); break;
                    case "ajuda": case "help": PrintHelp(); break;
                    default: Console.WriteLine("Comando desconhecido: " + verb); Console.WriteLine("Digite 'ajuda' para ver os comandos."); break;
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
            if (parts.Length < 2) { Console.WriteLine("Uso: run <arquivo> [argumentos...] [--cell <id>] [--wait]"); return; }
            string filePath = parts[1];
            if (_agentManager.Resolve(filePath) != null) { RunAssistant(parts); return; }
            string cellId = null;
            var appArgs = new System.Collections.Generic.List<string>();
            var limits = new ResourceLimits();
            bool wait = false;
            for (int i = 2; i < parts.Length; i++)
            {
                if (parts[i] == "--cell" && i + 1 < parts.Length) cellId = parts[++i];
                else if (parts[i] == "--wait") wait = true;
                else if (TryParseLimit(parts, ref i, limits)) continue;
                else appArgs.Add(parts[i]);
            }
            string arguments = appArgs.Count > 0 ? string.Join(" ", appArgs) : null;
            if (!File.Exists(filePath)) { Console.WriteLine("Arquivo não encontrado: " + filePath); return; }
            Cell cell = _runner.RunAsync(_runtime, cellId, filePath, arguments, null, limits.IsEmpty ? null : limits).GetAwaiter().GetResult();
            Console.WriteLine("Célula criada e iniciada:");
            Console.WriteLine("  id     : " + cell.Id);
            Console.WriteLine("  comando: " + cell.AppPath + " " + cell.Args);
            Console.WriteLine("  pid    : " + cell.ProcessId);
            Console.WriteLine("  log    : " + cell.LogFile);
            if (wait)
            {
                _runtime.WaitCellAsync(cell.Id).GetAwaiter().GetResult();
                Console.WriteLine(); Console.WriteLine("--- saída da célula ---"); Console.WriteLine(_runtime.ReadCellLog(cell.Id));
            }
        }

        private static void RunAssistant(string[] parts)
        {
            string assistant = parts[1]; string cellId = null;
            for (int i = 2; i < parts.Length; i++) if (parts[i] == "--cell" && i + 1 < parts.Length) cellId = parts[++i];
            if (string.IsNullOrWhiteSpace(cellId)) cellId = assistant;
            Cell cell = _agentManager.StartAssistantCell(_runtime, cellId, assistant);
            Console.WriteLine("Célula assistente criada (iniciar com 'cell start " + cell.Id + "'):");
            Console.WriteLine("  id     : " + cell.Id); Console.WriteLine("  app    : " + cell.AppPath); Console.WriteLine("  log    : " + cell.LogFile);
        }

        private static void PrintAgents()
        {
            Console.WriteLine("Assistentes configurados:");
            AgentInfo[] available = _agentManager.AvailableAssistants().ToArray();
            foreach (AgentInfo agent in _agentManager.Assistants)
            {
                bool ok = agent.Executable != null && File.Exists(agent.Executable);
                Console.WriteLine("  " + (ok ? "[ok]   " : "[ausente] ") + agent);
            }
            if (available.Length == 0) Console.WriteLine("Nenhum assistente disponível. Rode: bash scripts/migrar-ferramentas.sh");
        }

        private static void ExecCommand(string[] parts)
        {
            if (parts.Length < 3) { Console.WriteLine("Uso: exec <shell|git|python|node> <comando> [argumentos...]"); return; }
            IToolExecutor executor = parts[1].ToLowerInvariant() switch { "shell" => Shell, "git" => Git, "python" or "python3" or "py" => Python, "node" => Node, _ => null };
            if (executor == null) { Console.WriteLine("Executor desconhecido: " + parts[1] + " (use shell, git, python ou node)"); return; }
            if (!executor.IsAvailable()) { Console.WriteLine("Executor '" + executor.Name + "' não está disponível neste ambiente."); return; }
            var request = new ExecutionRequest { Command = parts[2], Arguments = parts.Skip(3).ToList(), Timeout = TimeSpan.FromSeconds(60) };
            Console.WriteLine("Executando via " + executor.Name + ": " + request.Command + (request.Arguments.Count > 0 ? " " + string.Join(" ", request.Arguments) : string.Empty));
            Console.WriteLine();
            ExecutionResult result = executor.ExecuteAsync(request).GetAwaiter().GetResult();
            string output = result.CombineOutput();
            Console.WriteLine(string.IsNullOrWhiteSpace(output) ? "(sem saída)" : output);
            Console.WriteLine();
            Console.WriteLine("== exit " + result.ExitCode + " (" + (result.Success ? "OK" : "FALHOU") + ") em " + result.Duration.TotalSeconds.ToString("0.0") + "s ==");
        }

        private static void ChatCommand(string[] parts)
        {
            string question = string.Join(" ", parts.Skip(1)).Trim();
            if (string.IsNullOrWhiteSpace(question)) { Console.WriteLine("Uso: chat \"sua pergunta\" [--model <modelo>]"); return; }
            string? model = null;
            int modelIdx = Array.IndexOf(parts, "--model");
            if (modelIdx >= 0 && modelIdx + 1 < parts.Length) model = parts[modelIdx + 1];
            OpenRouterClient client = EnsureAiClient(model);
            Console.WriteLine("Modelo: " + client.Options.Model + " · " + client.Options.BaseUrl);
            Console.WriteLine("Pensando...");
            try { Console.WriteLine(); Console.WriteLine(client.ChatAsync(question, SharedHttpClient).GetAwaiter().GetResult()); }
            catch (Exception ex) { Console.WriteLine("Erro: " + ex.Message); _logger.Error(ex.ToString()); }
        }

        private static void AgentCommand(string[] parts)
        {
            string instruction = string.Join(" ", parts.Skip(1)).Trim();
            if (string.IsNullOrWhiteSpace(instruction)) { Console.WriteLine("Uso: agent \"instrução\" — agente de arquivos num workspace (listar/ler/editar/rodar comandos)"); return; }
            OpenRouterClient client = EnsureAiClient();
            string workspace = Path.Combine(UserAuraDir(), "workspace"); Directory.CreateDirectory(workspace);
            var tools = new System.Collections.Generic.List<AgentTool> { new ListDirTool(workspace), new ReadFileTool(workspace), new WriteFileTool(workspace), new EditFileTool(workspace), new ShellAgentTool(workspace, Shell), new WebFetchTool() };
            string systemPrompt = "Você é o agente de arquivos da AURA, um assistente que trabalha dentro de um workspace no dispositivo. Você PODE listar, ler, criar, editar e sobrescrever arquivos do workspace e rodar comandos de shell para concluir a tarefa. Sempre responda em português. Workspace: " + workspace;
            var session = new AgentSession(client, tools, systemPrompt, _logger, _memory);
            session.Step += step => { Console.WriteLine(); Console.WriteLine("  ◆ " + step.ToolName + " " + step.Arguments); if (!string.IsNullOrWhiteSpace(step.Result)) Console.WriteLine("    " + step.Result.Replace("\n", "\n    ")); };
            Console.WriteLine("Modelo: " + client.Options.Model + " · workspace: " + workspace); Console.WriteLine("Executando agente...");
            try { string answer = session.RunAsync(instruction, SharedHttpClient).GetAwaiter().GetResult(); Console.WriteLine(); Console.WriteLine("=== RESPOSTA DO AGENTE ==="); Console.WriteLine(answer); }
            catch (Exception ex) { Console.WriteLine("Erro: " + ex.Message); _logger.Error(ex.ToString()); }
        }

        private static void EnsinarCommand(string[] parts)
        {
            string task = string.Join(" ", parts.Skip(1)).Trim();
            if (string.IsNullOrWhiteSpace(task)) { Console.WriteLine("Uso: ensinar <descrição da tarefa>"); Console.WriteLine("Exemplo: ensinar como baixar um arquivo com Python"); return; }
            string workspace = Path.Combine(UserAuraDir(), "workspace"); Directory.CreateDirectory(workspace);
            var webSearch = new WebSearchService();
            OpenRouterClient client = null;
            try { client = EnsureAiClient(); } catch (Exception ex) { _logger.Warning("AI não configurada: " + ex.Message); }
            var tools = new System.Collections.Generic.List<AgentTool> { new InterpretCommandTool(), new SearchMemoryTool(new SolutionStore(_logger)), new WebSearchTool(webSearch), new CodeExtractorTool(webSearch, client), new CodeExecutorTool(Shell, workspace), new ShellAgentTool(workspace, Shell), new WebFetchTool() };
            string systemPrompt = "Você é a AURA Professora. Dada uma tarefa de aprendizado, execute o fluxo: 1) busque na memória por tarefas similares; 2) pesquise na web exemplos; 3) extraia o código; 4) execute o código e valide o resultado. Se o código falhar, tente novamente com ajustes. Responda em português com o que aprendeu e o resultado final. Workspace: " + workspace;
            var session = new AgentSession(client ?? EnsureAiClient(), tools, systemPrompt, _logger, _memory);
            session.Step += step => { Console.WriteLine(); Console.WriteLine("  ◆ " + step.ToolName + " " + step.Arguments); if (!string.IsNullOrWhiteSpace(step.Result)) Console.WriteLine("    " + step.Result.Replace("\n", "\n    ")); };
            Console.WriteLine("AURA está aprendendo..."); Console.WriteLine("Tarefa: " + task); Console.WriteLine("Modelo: " + client?.Options.Model + " · workspace: " + workspace); Console.WriteLine();
            try { string answer = session.RunAsync("Ensinar: " + task, SharedHttpClient).GetAwaiter().GetResult(); _memory.Append(MemoryEntry.Answer(answer)); Console.WriteLine(); Console.WriteLine("=== RESULTADO DO APRENDIZADO ==="); Console.WriteLine(answer); }
            catch (Exception ex) { Console.WriteLine("Erro: " + ex.Message); _logger.Error(ex.ToString()); }
        }

        private static void AiKeyCommand(string[] parts)
        {
            if (parts.Length < 2) { Console.WriteLine("Uso: aichave <chave> — salva a credencial em ~/.aura/ai_key.txt"); Console.WriteLine("O provider, modelo e endpoints são configurados separadamente pelo usuário."); return; }
            string key = parts[1].Trim();
            if (key.Length > 200 || key.IndexOfAny(new[] { ' ', '\t', '\r', '\n' }) >= 0) { Console.WriteLine("Chave inválida."); return; }
            string file = Path.Combine(UserAuraDir(), "ai_key.txt"); Directory.CreateDirectory(Path.GetDirectoryName(file)); File.WriteAllText(file, key); Console.WriteLine("Chave salva em " + file);
        }

        private static string UserAuraDir() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aura");

        private static string ReadAiKey()
        {
            string key = Environment.GetEnvironmentVariable("AURA_AI_API_KEY") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key)) key = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                string keyFile = Path.Combine(UserAuraDir(), "ai_key.txt");
                if (File.Exists(keyFile)) key = File.ReadAllText(keyFile).Trim();
            }
            return key;
        }

        private static string ReadAiSetting(string name)
        {
            string value = Environment.GetEnvironmentVariable("AURA_AI_" + name.ToUpperInvariant()) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            string file = Path.Combine(UserAuraDir(), "ai_" + name.ToLowerInvariant() + ".txt");
            return File.Exists(file) ? File.ReadAllText(file).Trim() : string.Empty;
        }

        private static OpenRouterClient EnsureAiClient(string? model = null)
        {
            string providerId = ReadAiSetting("PROVIDER");
            string selectedModel = string.IsNullOrWhiteSpace(model) ? ReadAiSetting("MODEL") : model.Trim();
            string apiKey = ReadAiKey();
            string baseUrl = ReadAiSetting("BASE_URL");
            string modelsUrl = ReadAiSetting("MODELS_URL");
            if (string.IsNullOrWhiteSpace(providerId)) throw new InvalidOperationException("Provider de IA não configurado. Defina AURA_AI_PROVIDER ou ~/.aura/ai_provider.txt.");
            if (string.IsNullOrWhiteSpace(selectedModel)) throw new InvalidOperationException("Modelo de IA não configurado. Defina AURA_AI_MODEL ou ~/.aura/ai_model.txt.");
            if (string.IsNullOrWhiteSpace(baseUrl)) throw new InvalidOperationException("Endpoint de IA não configurado. Defina AURA_AI_BASE_URL ou ~/.aura/ai_base_url.txt.");
            UniversalConnection connection = UniversalRuntimeAdapter.CreateConnection(providerId, apiKey, selectedModel, baseUrl, modelsUrl);
            _aiClient = UniversalAiClientFactory.Create(connection);
            return _aiClient;
        }

        private static void Ask(string[] parts)
        {
            if (parts.Length < 2) { Console.WriteLine("Uso: ask \"sua pergunta\" [--assistente aichat] [--cell <id>]"); return; }
            var args = new System.Collections.Generic.List<string>(); string assistant = "aichat"; string cellId = null;
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i] == "--assistente" && i + 1 < parts.Length) assistant = parts[++i];
                else if (parts[i] == "--cell" && i + 1 < parts.Length) cellId = parts[++i];
                else args.Add(parts[i]);
            }
            string question = string.Join(" ", args);
            if (string.IsNullOrWhiteSpace(question)) { Console.WriteLine("A pergunta não pode ser vazia."); return; }
            Console.WriteLine("Assistente '" + assistant + "' respondendo...");
            Console.WriteLine(_agentManager.AskAsync(_runtime, question, assistant, cellId).GetAwaiter().GetResult());
        }

        private static bool TryParseLimit(string[] parts, ref int i, ResourceLimits limits)
        {
            string token = parts[i].ToLowerInvariant();
            if (token == "--mem" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long mem)) { limits.MemoryLimitMb = mem; i++; return true; }
            if (token == "--cpu" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long cpu)) { limits.CpuLimitSeconds = cpu; i++; return true; }
            if (token == "--files" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long files)) { limits.MaxFiles = files; i++; return true; }
            if (token == "--procs" && i + 1 < parts.Length && long.TryParse(parts[i + 1], out long procs)) { limits.MaxProcesses = procs; i++; return true; }
            return false;
        }

        private static void PrintCells()
        {
            Cell[] cells = _runtime.Cells.ToArray();
            if (cells.Length == 0) { Console.WriteLine("Nenhuma célula. Use 'run <arquivo>' para criar uma."); return; }
            Console.WriteLine("Células (" + _runtime.CellsRoot + "):"); Console.WriteLine("{0,-24} {1,-10} {2,-8} {3}", "ID", "ESTADO", "PID", "APLICATIVO");
            foreach (Cell cell in cells) Console.WriteLine("{0,-24} {1,-10} {2,-8} {3}", cell.Id, cell.State, cell.ProcessId.HasValue ? cell.ProcessId.Value.ToString() : "-", cell.AppPath);
        }

        private static void CellCommand(string[] parts)
        {
            if (parts.Length < 3) { Console.WriteLine("Uso: cell <start|stop|pause|resume|delete|log|limits> <id>"); return; }
            string action = parts[1].ToLowerInvariant(); string id = parts[2];
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
            for (int i = 3; i < parts.Length; i++) { int idx = i; if (TryParseLimit(parts, ref idx, limits)) i = idx; }
            _runtime.SetCellLimits(id, limits); Console.WriteLine("Limites aplicados na célula '" + id + "'.");
        }

        private static void PrintLaunchers()
        {
            Console.WriteLine("Launchers registrados (AURA decide como rodar):");
            foreach (ILauncher launcher in _runner.Launchers) Console.WriteLine("  " + launcher.GetType().Name + " -> " + string.Join(", ", launcher.SupportedExtensions));
        }

        private static void PrintPlugins()
        {
            Console.WriteLine("Plugins (" + _pluginWatcher.PluginsRoot + "):");
            string[] paths = _pluginWatcher.PluginPaths.ToArray();
            if (paths.Length == 0) { Console.WriteLine("  (nenhum plugin .dll encontrado)"); return; }
            foreach (string path in paths) Console.WriteLine("  " + Path.GetFileName(path));
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

        private static void PrintWelcome()
        {
            Console.WriteLine("Bem-vindo ao AURA Orchestrator!");
            Console.WriteLine("Comandos básicos: 'ajuda' para ajuda, 'run <arquivo>' para executar,");
            Console.WriteLine("'agents' para listar assistentes, 'config' para ver a configuração.");
            Console.WriteLine();
        }

        private static void PrintConfig()
        {
            Console.WriteLine("Configuração (" + _bootstrap.SettingsPath + "):");
            Console.WriteLine("  Internet           : " + _bootstrap.Settings.Internet);
            Console.WriteLine("  FirstRunCompleted  : " + _bootstrap.Settings.FirstRunCompleted);
            Console.WriteLine("  Theme              : " + _bootstrap.Settings.Theme);
            Console.WriteLine();
            Console.WriteLine("Módulos (" + _bootstrap.ModulesPath + "):");
            foreach (ModuleInfo m in ModuleCatalog.GetAll())
            {
                string state = m.IsCore ? "núcleo" : _bootstrap.Modules.Modules.IsEnabled(m.Id) ? "aplicado" : "não aplicado";
                Console.WriteLine("  " + m.DisplayName.PadRight(24) + ": " + state);
            }
        }

        private static void PrintModules()
        {
            foreach (ModuleInfo module in ModuleCatalog.GetAll())
            {
                string kind = module.IsCore ? "núcleo" : string.IsNullOrWhiteSpace(module.PackageUrl) ? "planejado" : "baixável";
                Console.WriteLine(module.Icon + " " + module.DisplayName + " [" + module.Status + ", " + kind + "] - " + module.ShortDescription);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Comandos:");
            Console.WriteLine("  run <arquivo> [args]   Escolhe um programa; AURA decide como rodar");
            Console.WriteLine("  run --wait app.go      Roda em primeiro plano e mostra a saída");
            Console.WriteLine("  run --mem 256 --cpu 30 app.py   Aplica limites (prlimit) à célula");
            Console.WriteLine("  cells                   Lista as células");
            Console.WriteLine("  cell start|stop|pause|resume|delete|log|limits <id>");
            Console.WriteLine("  persist                 Salva o índice de células em disco");
            Console.WriteLine("  diagnostico             Diagnóstico do sistema");
            Console.WriteLine("  internet                Verifica conexão");
            Console.WriteLine("  agents                  Lista assistentes");
            Console.WriteLine("  ask \"pergunta\"          Pergunta via assistente, logada em célula");
            Console.WriteLine("  chat \"pergunta\"         Pergunta direta à IA [--model x]");
            Console.WriteLine("  agent \"instrução\"       Agente de arquivos num workspace (IA + ferramentas)");
            Console.WriteLine("  ensinar \"tarefa\"        AURA Professora: pesquisa, extrai e executa código");
            Console.WriteLine("  aichave <chave>          Salva a credencial da IA em ~/.aura/ai_key.txt");
            Console.WriteLine("  exec <shell|git|python|node> <cmd> [args]   Executa via executor");
            Console.WriteLine("  run aichat --cell chat  Inicia assistente como célula");
            Console.WriteLine("  modulos                 Lista módulos disponíveis");
            Console.WriteLine("  config                  Mostra configuração (settings + módulos)");
            Console.WriteLine("  launchers               Lista resolutores de extensão");
            Console.WriteLine("  plugins                 Lista plugins carregados");
            Console.WriteLine("  ajuda                   Mostra esta ajuda");
            Console.WriteLine("  exit                    Sai");
            Console.WriteLine();
        }
    }
}
