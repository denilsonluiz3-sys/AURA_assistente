# FERRAMENTAS

## Definições
```
src/AURA.AI/AgentSession.cs:128:                t => t.Definition.Name == call.Name);
src/AURA.AI/AgentSession.cs:167:            string toolName,
src/AURA.AI/AgentSession.cs:172:                return DefaultToolArguments(toolName);
src/AURA.AI/AgentSession.cs:216:                    return DefaultToolArguments(toolName);
src/AURA.AI/AgentSession.cs:304:                                string typeName =
src/AURA.AI/AgentSession.cs:316:                                        toolName.Equals(
src/AURA.AI/AgentSession.cs:355:                if (toolName.Equals(
src/AURA.AI/AgentSession.cs:370:                if (toolName.Equals(
src/AURA.AI/AgentSession.cs:381:                if (toolName.Equals(
src/AURA.AI/AgentSession.cs:399:                if (toolName.Equals(
src/AURA.AI/AgentSession.cs:415:                if (toolName.Equals(
src/AURA.AI/AgentSession.cs:455:                        toolName,
src/AURA.AI/AgentSession.cs:459:                return DefaultToolArguments(toolName);
src/AURA.AI/AgentSession.cs:464:            string toolName)
src/AURA.AI/AgentSession.cs:466:            if (toolName.Equals(
src/AURA.AI/AgentSession.cs:473:            if (toolName.Equals(
src/AURA.AI/AgentSession.cs:480:            if (toolName.Equals(
src/AURA.AI/AgentSession.cs:487:            if (toolName.Equals(
src/AURA.AI/AgentSession.cs:494:            if (toolName.Equals(
src/AURA.AI/OpenRouterClient.cs:287:                                string name = string.Empty;
src/AURA.AI/OpenRouterClient.cs:291:                                    name = GetProp(fn, "name") ?? string.Empty;
src/AURA.AI/OpenRouterClient.cs:298:                                    Name = name,
src/AURA.AI/OpenRouterClient.cs:405:                string? name = nameEl.GetString();
src/AURA.AI/OpenRouterClient.cs:431:                        Name = name,
src/AURA.AI/AgentChat.cs:22:    public sealed class AgentToolCall
src/AURA.AI/AgentChat.cs:47:        public AgentStep(string toolName, string arguments, string result)
src/AURA.AI/AgentChat.cs:49:            ToolName = toolName;
src/AURA.AI/AgentChat.cs:54:        public string ToolName { get; }
src/AURA.AI/ProviderCatalog.cs:37:                    Name = "OpenRouter",
src/AURA.AI/ProviderCatalog.cs:55:                    Name = "Groq (grátis)",
src/AURA.AI/ProviderCatalog.cs:69:                    Name = "Cerebras (grátis)",
src/AURA.AI/ProviderCatalog.cs:81:                    Name = "Google Gemini",
src/AURA.AI/ProviderCatalog.cs:93:                    Name = "Ollama (local)",
src/AURA.AI/AgentTool.cs:9:    public sealed class AgentToolParameter
src/AURA.AI/AgentTool.cs:17:    public sealed class AgentToolDefinition
src/AURA.AI/AgentTool.cs:32:    public abstract class AgentTool
src/AURA.AI/AgentTools/ShellAgentTool.cs:15:    public sealed class ShellAgentTool : AgentTool
src/AURA.AI/AgentTools/ShellAgentTool.cs:23:        public ShellAgentTool(string workspaceRoot)
src/AURA.AI/AgentTools/ShellAgentTool.cs:31:            Name = "run_shell",
src/AURA.AI/AgentTools/ShellAgentTool.cs:32:            Description = "Executa um comando shell (sh -c) no diretório do workspace. " +
src/AURA.AI/AgentTools/ShellAgentTool.cs:39:                    Description = "Comando shell completo (ex.: 'git status --short')."
src/AURA.AI/AgentTools/ShellAgentTool.cs:65:                FileName = _shellPath,
src/AURA.AI/AgentTools/FileTools.cs:11:    public sealed class ListDirTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:19:            Name = "list_dir",
src/AURA.AI/AgentTools/FileTools.cs:20:            Description = "Lista o conteúdo de um diretório do workspace (pastas e arquivos com tamanho).",
src/AURA.AI/AgentTools/FileTools.cs:26:                    Description = "Caminho relativo ao workspace (vazio ou '.' = raiz)."
src/AURA.AI/AgentTools/FileTools.cs:50:                string name = Path.GetFileName(entry);
src/AURA.AI/AgentTools/FileTools.cs:71:    public sealed class ReadFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:79:            Name = "read_file",
src/AURA.AI/AgentTools/FileTools.cs:80:            Description = "Lê o conteúdo textual de um arquivo do workspace (máx. 40.000 caracteres).",
src/AURA.AI/AgentTools/FileTools.cs:86:                    Description = "Caminho relativo ao workspace."
src/AURA.AI/AgentTools/FileTools.cs:112:    public sealed class WriteFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:120:            Name = "write_file",
src/AURA.AI/AgentTools/FileTools.cs:121:            Description = "Cria ou sobrescreve um arquivo do workspace com o conteúdo informado.",
src/AURA.AI/AgentTools/FileTools.cs:127:                    Description = "Caminho relativo ao workspace."
src/AURA.AI/AgentTools/FileTools.cs:132:                    Description = "Conteúdo completo a gravar no arquivo."
src/AURA.AI/AgentTools/FileTools.cs:157:    public sealed class EditFileTool : WorkspaceAgentTool
src/AURA.AI/AgentTools/FileTools.cs:165:            Name = "edit_file",
src/AURA.AI/AgentTools/FileTools.cs:166:            Description = "Substitui a primeira ocorrência de um trecho exato em um arquivo do workspace.",
src/AURA.AI/AgentTools/FileTools.cs:172:                    Description = "Caminho relativo ao workspace."
src/AURA.AI/AgentTools/FileTools.cs:177:                    Description = "Trecho exato a ser substituído."
src/AURA.AI/AgentTools/FileTools.cs:182:                    Description = "Novo trecho que substitui old_text."
src/AURA.AI/AgentTools/WorkspaceAgentTool.cs:11:    public abstract class WorkspaceAgentTool : AgentTool
src/AURA.AI/AgentTools/WorkspaceAgentTool.cs:13:        protected WorkspaceAgentTool(string workspaceRoot)
```

## Ferramentas conhecidas
```
src/AURA.AI/AgentSession.cs:311:                                    // list_dir sem valor real =
src/AURA.AI/AgentSession.cs:317:                                            "list_dir",
src/AURA.AI/AgentSession.cs:354:                // list_dir
src/AURA.AI/AgentSession.cs:356:                        "list_dir",
src/AURA.AI/AgentSession.cs:369:                // read_file
src/AURA.AI/AgentSession.cs:371:                        "read_file",
src/AURA.AI/AgentSession.cs:380:                // run_shell
src/AURA.AI/AgentSession.cs:382:                        "run_shell",
src/AURA.AI/AgentSession.cs:398:                // write_file
src/AURA.AI/AgentSession.cs:400:                        "write_file",
src/AURA.AI/AgentSession.cs:414:                // edit_file
src/AURA.AI/AgentSession.cs:416:                        "edit_file",
src/AURA.AI/AgentSession.cs:467:                    "list_dir",
src/AURA.AI/AgentSession.cs:474:                    "read_file",
src/AURA.AI/AgentSession.cs:481:                    "run_shell",
src/AURA.AI/AgentSession.cs:488:                    "write_file",
src/AURA.AI/AgentSession.cs:495:                    "edit_file",
src/AURA.AI/AgentTools/ShellAgentTool.cs:31:            Name = "run_shell",
src/AURA.AI/AgentTools/ShellAgentTool.cs:32:            Description = "Executa um comando shell (sh -c) no diretório do workspace. " +
src/AURA.AI/AgentTools/FileTools.cs:19:            Name = "list_dir",
src/AURA.AI/AgentTools/FileTools.cs:79:            Name = "read_file",
src/AURA.AI/AgentTools/FileTools.cs:120:            Name = "write_file",
src/AURA.AI/AgentTools/FileTools.cs:165:            Name = "edit_file",
src/AURA.Core/Configuration/ModulesConfiguration.cs:51:                case "executors": return Executors;
src/AURA.Core/Configuration/ModulesConfiguration.cs:74:                case "executors": Executors = value; break;
src/AURA.CLI/Program.cs:168:                    case "exec":
src/AURA.CLI/Program.cs:319:                "git" => Git,
src/AURA.CLI/Program.cs:327:                Console.WriteLine("Executor desconhecido: " + parts[1] + " (use shell, git, python ou node)");
src/AURA.CLI/Program.cs:333:                Console.WriteLine("Executor '" + executor.Name + "' não está disponível neste ambiente.");
src/AURA.CLI/Program.cs:344:            Console.WriteLine("Executando via " + executor.Name + ": " + request.Command +
src/AURA.CLI/Program.cs:403:        "Execute a solicitação do usuário de forma objetiva. " +
src/AURA.CLI/Program.cs:444:            Console.WriteLine("Executando Aura...");
src/AURA.Mobile/MainPage.cs:43:                ("executors", "Ferramentas", "Executores", executors),
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:23:        ExecutorPicker.ItemsSource = new[] { "Shell", "Git", "Python", "Node" };
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:44:            "Git" => _git,
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:80:        ResultEditor.Text = "Executando...";
src/AURA.Mobile/Pages/ExecutorsPage.xaml.cs:103:            AuraLog.Exception("ExecutorsPage.Execute", ex);
src/AURA.Abstractions/Runtime/RuntimeModels.cs:102:/// converte um <see cref="ExecutionResult"/> existente da AURA.
src/AURA.Modules/Executors/GitExecutor.cs:13:    public override string Name => "git";
src/AURA.Modules/Executors/GitExecutor.cs:15:    public override bool IsAvailable() => ResolveBinary("git") is not null;
src/AURA.Modules/Executors/GitExecutor.cs:19:        if (ResolveBinary("git") is not { } binary)
src/AURA.Modules/Executors/GitExecutor.cs:20:            return Task.FromResult(ExecutionResult.Failed("git não encontrado no ambiente."));
src/AURA.Modules/ModuleCatalog.cs:31:                Includes = new List<string> { "WebView", "SearchCatalog", "VpnHelper" },
src/AURA.Modules/ModuleCatalog.cs:97:                    "Execução de tarefas em arquivos"
src/AURA.Modules/ModuleCatalog.cs:131:                Id = "executors",
src/AURA.Modules/ModuleCatalog.cs:132:                DisplayName = "Executores",
src/AURA.Modules/ModuleCatalog.cs:134:                ShortDescription = "Executa comandos Shell, Git, Python e Node com saída capturada.",
src/AURA.Modules/ModuleCatalog.cs:138:                Features = new List<string> { "Shell", "Git", "Python", "Node" },
src/AURA.Modules/ModuleCatalog.cs:139:                Includes = new List<string> { "ShellExecutor", "GitExecutor", "PythonExecutor", "NodeExecutor" },
src/AURA.Modules/ModuleCatalog.cs:266:                    "Execução automática de tarefas",
src/AURA.Modules/Runtime/Installer.cs:59:            Console.Write("Executar agora? [s/N] ");
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:60:        "set", "shift", "source", "eval", "exec", "trap", "ulimit", "umask",
src/AURA.Modules/Runtime/DependencyAnalyzer.cs:63:        "gzip", "git", "python", "python3", "pip", "pip3", "node", "npm", "bash",
src/AURA.Modules/Runtime/RuntimeManager.cs:141:        report.Steps.Add("execucao");
src/AURA.Modules/Runtime/RuntimeManager.cs:142:        report.Log($"Execução: {(report.Outcome.Success ? "OK" : "FALHOU")} " +
```

## Registro / construção
```
src/AURA.AI/AgentSession.cs:25:        private readonly List<AgentTool> _tools;
src/AURA.AI/AgentSession.cs:30:        public AgentSession(OpenRouterClient client, IEnumerable<AgentTool> tools,
src/AURA.AI/AgentSession.cs:34:            _tools = (tools ?? Enumerable.Empty<AgentTool>()).ToList();
src/AURA.AI/OpenRouterClient.cs:295:                                calls.Add(new AgentToolCall
src/AURA.CLI/Program.cs:421:            var tools = new System.Collections.Generic.List<AgentTool>
src/AURA.Mobile/Pages/AgentPage.xaml.cs:40:        var tools = new List<AgentTool>
```

