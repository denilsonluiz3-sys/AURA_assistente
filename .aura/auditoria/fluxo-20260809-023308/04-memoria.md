# FLUXO DA MEMÓRIA

## MemoryStore
```
src/AURA.AI/AiAssistant.cs:12:    /// persists the conversation turn in MemoryStore so context survives across
src/AURA.AI/AiAssistant.cs:18:        private readonly MemoryStore _memory;
src/AURA.AI/AiAssistant.cs:21:        public AiAssistant(OpenRouterClient client, MemoryStore memory, ILogger? logger = null)
src/AURA.AI/AiAssistantService.cs:15:    /// <br/>Persists conversation history in MemoryStore for cross-session continuity.
src/AURA.AI/AiAssistantService.cs:28:        public static async Task<string> AskAsync(string question, MemoryStore? memory = null, ILogger? logger = null, OpenRouterOptions? options = null, HttpClient? http = null)
src/AURA.Memory/MemoryStore.cs:19:    public sealed class MemoryStore
src/AURA.Memory/MemoryStore.cs:32:        public MemoryStore(ILogger logger, string path = null)
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:8:    private readonly MemoryStore _memoryStore;
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:11:    public MemoryPage(MemoryStore memoryStore)
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:14:        _memoryStore = memoryStore;
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:33:            var entries = await Task.Run(() => _memoryStore.Read(64));
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:60:        await Task.Run(() => _memoryStore.Clear());
src/AURA.Mobile/Pages/ChatPage.xaml.cs:9:    private readonly AURA.Memory.MemoryStore _memory;
src/AURA.Mobile/Pages/ChatPage.xaml.cs:11:    public ChatPage(OpenRouterClient client, AURA.Memory.MemoryStore memory)
src/AURA.Mobile/MauiProgram.cs:55:        builder.Services.AddSingleton(sp => new MemoryStore(
src/AURA.Mobile/MauiProgram.cs:117:            var memory = app.Services.GetRequiredService<MemoryStore>();
src/AURA.Modules/ModuleCatalog.cs:113:                Includes = new List<string> { "MemoryStore" },
```

## SolutionStore
```
src/AURA.AI/AgentSession.cs:28:        private readonly SolutionStore _solutionStore;
src/AURA.AI/AgentSession.cs:37:            _solutionStore = new SolutionStore();
src/AURA.AI/AgentSession.cs:117:            return _solutionStore.Find(
src/AURA.Memory/SolutionStore.cs:17:    public sealed class SolutionStore
src/AURA.Memory/SolutionStore.cs:31:        public SolutionStore(
```

## SolutionRule
```
src/AURA.AI/AgentSession.cs:109:        private SolutionRule? TryGetKnownSolution(
src/AURA.Memory/SolutionRule.cs:12:    public sealed class SolutionRule
src/AURA.Memory/SolutionStore.cs:44:        public IReadOnlyList<SolutionRule> ReadAll()
src/AURA.Memory/SolutionStore.cs:54:        public SolutionRule? Find(
src/AURA.Memory/SolutionStore.cs:71:        public void SaveValidated(SolutionRule rule)
src/AURA.Memory/SolutionStore.cs:86:                List<SolutionRule> all = LoadLocked();
src/AURA.Memory/SolutionStore.cs:88:                SolutionRule? existing =
src/AURA.Memory/SolutionStore.cs:115:                List<SolutionRule> all = LoadLocked();
src/AURA.Memory/SolutionStore.cs:117:                SolutionRule? rule =
src/AURA.Memory/SolutionStore.cs:136:        private List<SolutionRule> LoadLocked()
src/AURA.Memory/SolutionStore.cs:141:                    return new List<SolutionRule>();
src/AURA.Memory/SolutionStore.cs:146:                    List<SolutionRule>>(json, Options)
src/AURA.Memory/SolutionStore.cs:147:                    ?? new List<SolutionRule>();
src/AURA.Memory/SolutionStore.cs:156:                return new List<SolutionRule>();
src/AURA.Memory/SolutionStore.cs:161:            List<SolutionRule> rules)
```

## RequestContext
```
src/AURA.AI/AgentSession.cs:110:            RequestContext request)
src/AURA.Memory/RequestContext.cs:11:    public sealed class RequestContext
```

## MemoryEntry
```
src/AURA.AI/AiAssistant.cs:31:            _memory.Append(MemoryEntry.Question(question));
src/AURA.AI/AiAssistant.cs:34:            _memory.Append(MemoryEntry.Answer(answer));
src/AURA.AI/AiAssistantService.cs:40:                memory.Append(MemoryEntry.Question(question));
src/AURA.AI/AiAssistantService.cs:71:                memory.Append(MemoryEntry.Answer(answer));
src/AURA.Memory/MemoryEntry.cs:12:    public sealed class MemoryEntry
src/AURA.Memory/MemoryEntry.cs:29:        public MemoryEntry()
src/AURA.Memory/MemoryEntry.cs:33:        public static MemoryEntry Question(string question)
src/AURA.Memory/MemoryEntry.cs:35:            return new MemoryEntry { Kind = MemoryKind.Turn, Role = "user", Text = question };
src/AURA.Memory/MemoryEntry.cs:38:        public static MemoryEntry Answer(string answer)
src/AURA.Memory/MemoryEntry.cs:40:            return new MemoryEntry { Kind = MemoryKind.Turn, Role = "assistant", Text = answer };
src/AURA.Memory/MemoryEntry.cs:43:        public static MemoryEntry CellStateChange(string cellId, string state)
src/AURA.Memory/MemoryEntry.cs:45:            return new MemoryEntry { Kind = MemoryKind.CellEvent, CellId = cellId, Detail = state };
src/AURA.Memory/MemoryStore.cs:40:        public void Append(MemoryEntry entry)
src/AURA.Memory/MemoryStore.cs:64:        public IReadOnlyList<MemoryEntry> Read(int tail = 64)
src/AURA.Memory/MemoryStore.cs:70:                var slice = new List<MemoryEntry>();
src/AURA.Memory/MemoryStore.cs:146:            public List<MemoryEntry> Entries { get; set; } = new List<MemoryEntry>();
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:9:    public ObservableCollection<MemoryEntry> Entries { get; } = new();
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:42:                Entries.Add(new MemoryEntry { Role = "AURA", Text = "Nenhuma memória registrada ainda." });
src/AURA.Mobile/Pages/MemoryPage.xaml.cs:48:            Entries.Add(new MemoryEntry { Role = "Erro", Text = ex.Message });
src/AURA.Mobile/MauiProgram.cs:119:                memory.Append(MemoryEntry.CellStateChange(evt.CellId, evt.To)));
```

## TryGetKnownSolution
```
src/AURA.AI/AgentSession.cs:109:        private SolutionRule? TryGetKnownSolution(
```

## Métodos de memória
```
src/AURA.Memory/SolutionRule.cs:4:namespace AURA.Memory
src/AURA.Memory/SolutionRule.cs:12:    public sealed class SolutionRule
src/AURA.Memory/RequestContext.cs:4:namespace AURA.Memory
src/AURA.Memory/MemoryEntry.cs:4:namespace AURA.Memory
src/AURA.Memory/MemoryEntry.cs:6:    public enum MemoryKind
src/AURA.Memory/MemoryEntry.cs:12:    public sealed class MemoryEntry
src/AURA.Memory/MemoryEntry.cs:14:        public MemoryKind Kind { get; set; }
src/AURA.Memory/MemoryEntry.cs:29:        public MemoryEntry()
src/AURA.Memory/MemoryEntry.cs:33:        public static MemoryEntry Question(string question)
src/AURA.Memory/MemoryEntry.cs:35:            return new MemoryEntry { Kind = MemoryKind.Turn, Role = "user", Text = question };
src/AURA.Memory/MemoryEntry.cs:38:        public static MemoryEntry Answer(string answer)
src/AURA.Memory/MemoryEntry.cs:40:            return new MemoryEntry { Kind = MemoryKind.Turn, Role = "assistant", Text = answer };
src/AURA.Memory/MemoryEntry.cs:43:        public static MemoryEntry CellStateChange(string cellId, string state)
src/AURA.Memory/MemoryEntry.cs:45:            return new MemoryEntry { Kind = MemoryKind.CellEvent, CellId = cellId, Detail = state };
src/AURA.Memory/MemoryStore.cs:8:namespace AURA.Memory
src/AURA.Memory/MemoryStore.cs:11:    /// F3/F5 backend: short-term working memory for the assistant. Mirrors the
src/AURA.Memory/MemoryStore.cs:12:    /// memory store exposed by the mobile app (AURA.Memory) - an append-only
src/AURA.Memory/MemoryStore.cs:13:    /// journal of conversation turns and cell lifecycle events, persisted to
src/AURA.Memory/MemoryStore.cs:14:    /// ~/AURA/memory.json so the assistant keeps context across restarts.
src/AURA.Memory/MemoryStore.cs:16:    /// This is the backend the mobile app's MemoryService/MemoryManager consume;
src/AURA.Memory/MemoryStore.cs:19:    public sealed class MemoryStore
src/AURA.Memory/MemoryStore.cs:32:        public MemoryStore(ILogger logger, string path = null)
src/AURA.Memory/MemoryStore.cs:35:            _path = path ?? SimulationRuntime.ExpandUserHome("~/AURA/memory.json");
src/AURA.Memory/MemoryStore.cs:40:        public void Append(MemoryEntry entry)
src/AURA.Memory/MemoryStore.cs:51:                    MemoryDocument document = LoadLocked();
src/AURA.Memory/MemoryStore.cs:52:                    document.Entries.Add(entry);
src/AURA.Memory/MemoryStore.cs:53:                    document.SavedAtUtc = DateTime.UtcNow;
src/AURA.Memory/MemoryStore.cs:55:                    PersistLocked(document);
src/AURA.Memory/MemoryStore.cs:64:        public IReadOnlyList<MemoryEntry> Read(int tail = 64)
src/AURA.Memory/MemoryStore.cs:68:                MemoryDocument document = LoadLocked();
src/AURA.Memory/MemoryStore.cs:70:                var slice = new List<MemoryEntry>();
src/AURA.Memory/MemoryStore.cs:73:                    slice.Add(document.Entries[i]);
src/AURA.Memory/MemoryStore.cs:98:        private MemoryDocument LoadLocked()
src/AURA.Memory/MemoryStore.cs:104:                    return new MemoryDocument();
src/AURA.Memory/MemoryStore.cs:108:                MemoryDocument document = JsonSerializer.Deserialize<MemoryDocument>(json, Options);
src/AURA.Memory/MemoryStore.cs:109:                return document ?? new MemoryDocument();
src/AURA.Memory/MemoryStore.cs:114:                return new MemoryDocument();
src/AURA.Memory/MemoryStore.cs:118:        private void PersistLocked(MemoryDocument document)
src/AURA.Memory/MemoryStore.cs:144:        private sealed class MemoryDocument
src/AURA.Memory/MemoryStore.cs:146:            public List<MemoryEntry> Entries { get; set; } = new List<MemoryEntry>();
src/AURA.Memory/MemoryStore.cs:148:            public DateTime? SavedAtUtc { get; set; }
src/AURA.Memory/SolutionStore.cs:9:namespace AURA.Memory
src/AURA.Memory/SolutionStore.cs:17:    public sealed class SolutionStore
src/AURA.Memory/SolutionStore.cs:31:        public SolutionStore(
src/AURA.Memory/SolutionStore.cs:39:                    "~/.aura/solutions.json");
src/AURA.Memory/SolutionStore.cs:44:        public IReadOnlyList<SolutionRule> ReadAll()
src/AURA.Memory/SolutionStore.cs:48:                return LoadLocked()
src/AURA.Memory/SolutionStore.cs:54:        public SolutionRule? Find(
src/AURA.Memory/SolutionStore.cs:61:                return LoadLocked()
src/AURA.Memory/SolutionStore.cs:71:        public void SaveValidated(SolutionRule rule)
src/AURA.Memory/SolutionStore.cs:86:                List<SolutionRule> all = LoadLocked();
src/AURA.Memory/SolutionStore.cs:88:                SolutionRule? existing =
src/AURA.Memory/SolutionStore.cs:97:                    all.Add(rule);
src/AURA.Memory/SolutionStore.cs:105:                PersistLocked(all);
src/AURA.Memory/SolutionStore.cs:115:                List<SolutionRule> all = LoadLocked();
src/AURA.Memory/SolutionStore.cs:117:                SolutionRule? rule =
src/AURA.Memory/SolutionStore.cs:132:                PersistLocked(all);
src/AURA.Memory/SolutionStore.cs:136:        private List<SolutionRule> LoadLocked()
src/AURA.Memory/SolutionStore.cs:141:                    return new List<SolutionRule>();
src/AURA.Memory/SolutionStore.cs:146:                    List<SolutionRule>>(json, Options)
src/AURA.Memory/SolutionStore.cs:147:                    ?? new List<SolutionRule>();
src/AURA.Memory/SolutionStore.cs:156:                return new List<SolutionRule>();
src/AURA.Memory/SolutionStore.cs:160:        private void PersistLocked(
src/AURA.Memory/SolutionStore.cs:161:            List<SolutionRule> rules)
src/AURA.AI/AgentSession.cs:9:using AURA.Memory;
src/AURA.AI/AgentSession.cs:28:        private readonly SolutionStore _solutionStore;
src/AURA.AI/AgentSession.cs:37:            _solutionStore = new SolutionStore();
src/AURA.AI/AgentSession.cs:53:            _messages.Add(new AgentMessage { Role = "user", Content = userText });
src/AURA.AI/AgentSession.cs:72:                    _messages.Add(new AgentMessage
src/AURA.AI/AgentSession.cs:83:                        _messages.Add(new AgentMessage
src/AURA.AI/AgentSession.cs:97:                _messages.Add(new AgentMessage { Role = "assistant", Content = final });
src/AURA.AI/AgentSession.cs:109:        private SolutionRule? TryGetKnownSolution(
src/AURA.AI/AgentSession.cs:117:            return _solutionStore.Find(
src/AURA.AI/AiAssistant.cs:6:using AURA.Memory;
src/AURA.AI/AiAssistant.cs:12:    /// persists the conversation turn in MemoryStore so context survives across
src/AURA.AI/AiAssistant.cs:13:    /// restarts (mirror of the mobile app's AURA.AI / MemoryService).
src/AURA.AI/AiAssistant.cs:18:        private readonly MemoryStore _memory;
src/AURA.AI/AiAssistant.cs:21:        public AiAssistant(OpenRouterClient client, MemoryStore memory, ILogger? logger = null)
src/AURA.AI/AiAssistant.cs:24:            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
src/AURA.AI/AiAssistant.cs:31:            _memory.Append(MemoryEntry.Question(question));
src/AURA.AI/AiAssistant.cs:34:            _memory.Append(MemoryEntry.Answer(answer));
src/AURA.AI/OpenRouterClient.cs:11:using AURA.Memory;
src/AURA.AI/OpenRouterClient.cs:17:    /// provedor via MemoryService; aqui o cliente HTTP direto. Defaults seguem
src/AURA.AI/OpenRouterClient.cs:57:                messages.Add(new { role = "system", content = systemPrompt });
src/AURA.AI/OpenRouterClient.cs:60:            messages.Add(new { role = "user", content = question });
src/AURA.AI/OpenRouterClient.cs:62:            var payload = new
src/AURA.AI/OpenRouterClient.cs:69:            string json = JsonSerializer.Serialize(payload);
src/AURA.AI/OpenRouterClient.cs:74:                request.Headers.TryAddWithoutValidation(
src/AURA.AI/OpenRouterClient.cs:80:                    request.Headers.TryAddWithoutValidation("X-Title", "AURA");
src/AURA.AI/OpenRouterClient.cs:81:                    request.Headers.TryAddWithoutValidation("X-URL", Options.AppReference);
src/AURA.AI/OpenRouterClient.cs:144:            var payload = new JsonObject
src/AURA.AI/OpenRouterClient.cs:153:                arr.Add(new JsonObject { ["role"] = "system", ["content"] = systemPrompt });
src/AURA.AI/OpenRouterClient.cs:176:                            calls.Add(new JsonObject
src/AURA.AI/OpenRouterClient.cs:191:                    arr.Add(mo);
src/AURA.AI/OpenRouterClient.cs:195:            payload["messages"] = arr;
src/AURA.AI/OpenRouterClient.cs:218:                            required.Add(r);
src/AURA.AI/OpenRouterClient.cs:224:                    toolsArray.Add(new JsonObject
src/AURA.AI/OpenRouterClient.cs:236:                payload["tools"] = toolsArray;
src/AURA.AI/OpenRouterClient.cs:239:            string json = JsonSerializer.Serialize(payload);
src/AURA.AI/OpenRouterClient.cs:242:            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + Options.ApiKey);
src/AURA.AI/OpenRouterClient.cs:245:                request.Headers.TryAddWithoutValidation("X-Title", "AURA");
src/AURA.AI/OpenRouterClient.cs:246:                request.Headers.TryAddWithoutValidation("X-URL", Options.AppReference);
src/AURA.AI/OpenRouterClient.cs:295:                                calls.Add(new AgentToolCall
src/AURA.AI/ProviderCatalog.cs:107:        public static ProviderInfo? Find(string? name)
src/AURA.AI/AiAssistantService.cs:8:using AURA.Memory;
src/AURA.AI/AiAssistantService.cs:15:    /// <br/>Persists conversation history in MemoryStore for cross-session continuity.
src/AURA.AI/AiAssistantService.cs:28:        public static async Task<string> AskAsync(string question, MemoryStore? memory = null, ILogger? logger = null, OpenRouterOptions? options = null, HttpClient? http = null)
src/AURA.AI/AiAssistantService.cs:38:            if (memory != null)
src/AURA.AI/AiAssistantService.cs:40:                memory.Append(MemoryEntry.Question(question));
src/AURA.AI/AiAssistantService.cs:44:            var payload = new
src/AURA.AI/AiAssistantService.cs:51:            string json = JsonSerializer.Serialize(payload);
src/AURA.AI/AiAssistantService.cs:69:            if (memory != null)
src/AURA.AI/AiAssistantService.cs:71:                memory.Append(MemoryEntry.Answer(answer));
src/AURA.AI/AgentTools/ShellAgentTool.cs:33:                "Use para git status/add/commit, dotnet build, grep, ls, etc. Timeout de 30s.",
```

