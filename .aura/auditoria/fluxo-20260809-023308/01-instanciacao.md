# INSTANCIAÇÃO REAL

## AgentSession
```
src/AURA.CLI/Program.cs:432:            var session = new AgentSession(client, tools, systemPrompt);
src/AURA.Mobile/Pages/AgentPage.xaml.cs:60:        _session = new AgentSession(_client, tools, systemPrompt);
```

## MemoryStore
```
src/AURA.Mobile/MauiProgram.cs:55:        builder.Services.AddSingleton(sp => new MemoryStore(
```

## SolutionStore
```
src/AURA.AI/AgentSession.cs:37:            _solutionStore = new SolutionStore();
```

## RequestContext
```
```

## AgentTool
```
src/AURA.AI/OpenRouterClient.cs:295:                                calls.Add(new AgentToolCall
src/AURA.AI/OpenRouterClient.cs:428:                    new AgentToolCall
src/AURA.AI/AgentTools/ShellAgentTool.cs:29:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/ShellAgentTool.cs:36:                ["command"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:17:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:23:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:77:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:83:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:118:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:124:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:129:                ["content"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:163:        public override AgentToolDefinition Definition => new AgentToolDefinition
src/AURA.AI/AgentTools/FileTools.cs:169:                ["path"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:174:                ["old_text"] = new AgentToolParameter
src/AURA.AI/AgentTools/FileTools.cs:179:                ["new_text"] = new AgentToolParameter
```

## AgentManager
```
src/AURA.CLI/Program.cs:64:            _agentManager = new AgentManager(_logger);
src/AURA.Mobile/MauiProgram.cs:69:        builder.Services.AddSingleton(sp => new AgentManager(sp.GetRequiredService<ILogger>())
```

## OpenRouterClient
```
src/AURA.CLI/Program.cs:381:            OpenRouterClient client = EnsureAiClient(model);
src/AURA.CLI/Program.cs:417:            OpenRouterClient client = EnsureAiClient();
src/AURA.CLI/Program.cs:507:                _aiClient = new OpenRouterClient(
src/AURA.CLI/Program.cs:537:            _aiClient = new OpenRouterClient(
src/AURA.Mobile/MauiProgram.cs:60:        builder.Services.AddSingleton(sp => new OpenRouterClient(new OpenRouterOptions
```

## ModuleManager
```
src/AURA.Mobile/MauiProgram.cs:48:        builder.Services.AddSingleton(sp => new ModuleManager(
```

## RuntimeManager
```
```

## ShellExecutor
```
src/AURA.CLI/Program.cs:32:        private static readonly ShellExecutor Shell = new();
```

## PythonExecutor
```
src/AURA.Installer/PythonInstaller.cs:16:    public PythonInstaller() : this(new PythonExecutor()) { }
src/AURA.Installer/PythonEnvironmentSelector.cs:25:        : this(new PythonExecutor(), () => new SystemAnalyzer().Analyze())
src/AURA.CLI/Program.cs:34:        private static readonly PythonExecutor Python = new();
```

## NodeExecutor
```
src/AURA.CLI/Program.cs:35:        private static readonly NodeExecutor Node = new();
```

## GitExecutor
```
src/AURA.CLI/Program.cs:33:        private static readonly GitExecutor Git = new();
```

