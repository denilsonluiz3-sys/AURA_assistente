using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.AI;
using AURA.Abstractions.Execution;
using AURA.Core;
using AURA.Core.Abstractions;
using AURA.Core.Logging;
using AURA.Memory;
using Xunit;

namespace AURA.Tests;

public class ProfessorToolsTests
{
    private class FakeWebSearch : IWebSearch
    {
        public string Response { get; set; } = "Resultado web";
        public int Calls { get; private set; }

        public Task<string> SearchAsync(string query, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(Response);
        }

        public Task<string> SearchWithRefinementAsync(string query, CancellationToken ct = default)
        {
            Calls++;
            return Task.FromResult(Response);
        }
    }

    private class FakeExecutor : IToolExecutor
    {
        public string Name => "fake";
        public ExecutionResult Result { get; set; } = new ExecutionResult { Success = true, StandardOutput = "ok" };
        public int Calls { get; private set; }

        public bool IsAvailable() => true;

        public Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }

    private static string CreateTempWorkspace()
    {
        string dir = Path.Combine(Path.GetTempPath(), "aura-prof-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public async Task WebSearchTool_ReturnsResult()
    {
        var tool = new WebSearchTool(new FakeWebSearch { Response = "Python tutorial aqui" });
        string result = await tool.ExecuteAsync(JsonSerializer.Serialize(new { query = "como usar python" }));

        Assert.Contains("Python tutorial", result);
    }

    [Fact]
    public async Task WebSearchTool_EmptyQuery_ReturnsError()
    {
        var tool = new WebSearchTool(new FakeWebSearch());
        string result = await tool.ExecuteAsync(JsonSerializer.Serialize(new { query = "" }));

        Assert.StartsWith("ERRO", result);
    }

    [Fact]
    public async Task CodeExtractorTool_Heuristic_ExtractsBlock()
    {
        var web = new FakeWebSearch
        {
            Response = "```python\nprint('oi')\n```"
        };
        var tool = new CodeExtractorTool(web);
        string result = await tool.ExecuteAsync(JsonSerializer.Serialize(new { task = "imprimir oi", language = "python" }));

        Assert.Contains("print('oi')", result);
        Assert.Contains("python3", result);
    }

    [Fact]
    public async Task CodeExecutorTool_WritesAndExecutes()
    {
        string root = CreateTempWorkspace();
        try
        {
            var executor = new FakeExecutor();
            var tool = new CodeExecutorTool(executor, root);

            string result = await tool.ExecuteAsync(
                JsonSerializer.Serialize(new { code = "print('x')", language = "python" }));

            Assert.Contains("Código executado com sucesso", result);
            Assert.Contains("ok", result);
            Assert.Equal(1, executor.Calls);
            Assert.Empty(Directory.GetFiles(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task CodeExecutorTool_Failure_ReportsExit()
    {
        string root = CreateTempWorkspace();
        try
        {
            var executor = new FakeExecutor
            {
                Result = new ExecutionResult { Success = false, ExitCode = 1, StandardError = "erro" }
            };
            var tool = new CodeExecutorTool(executor, root);

            string result = await tool.ExecuteAsync(
                JsonSerializer.Serialize(new { code = "print('x')", language = "python" }));

            Assert.Contains("Falha na execução", result);
            Assert.Contains("exit 1", result);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task InterpretCommandTool_DetectsIntent()
    {
        var tool = new InterpretCommandTool();

        string research = await tool.ExecuteAsync(JsonSerializer.Serialize(new { command = "pesquise como usar git" }));
        using (var doc = JsonDocument.Parse(research))
        {
            Assert.Equal("pesquisar", doc.RootElement.GetProperty("intent").GetString());
        }

        string run = await tool.ExecuteAsync(JsonSerializer.Serialize(new { command = "execute um script" }));
        using (var doc = JsonDocument.Parse(run))
        {
            Assert.Equal("executar", doc.RootElement.GetProperty("intent").GetString());
        }
    }

    [Fact]
    public async Task SearchMemoryTool_EmptyMemory_ReturnsNone()
    {
        var store = new SolutionStore(new ConsoleLogger(), maxAgeDays: 30, maxEntries: 50);
        var tool = new SearchMemoryTool(store);

        string result = await tool.ExecuteAsync(JsonSerializer.Serialize(new { query = "qualquer coisa" }));

        Assert.Contains("Nenhuma memória", result);
    }

    [Fact]
    public void WebSearchService_Refinement_SkipsFailure()
    {
        var service = new WebSearchService();
        string result = service.SearchWithRefinementAsync("").GetAwaiter().GetResult();

        Assert.Contains("Digite uma pergunta", result);
    }
}