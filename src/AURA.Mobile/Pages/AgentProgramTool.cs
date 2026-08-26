using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;
using AURA.AI;
using AURA.Agents.Programs;
using AURA.Core.Launchers;
using AURA.Core.Runtime;

namespace AURA.Mobile.Pages;

public sealed class AgentListProgramsTool : AgentTool
{
    private readonly CellProgramRegistry _registry;

    public AgentListProgramsTool(CellProgramRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public override AgentToolDefinition Definition => new AgentToolDefinition
    {
        Name = "list_programs",
        Description = "Lista todos os programas (Cell Programs) disponíveis no app.",
        Parameters = { }
    };

    public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
    {
        var programs = _registry.All.ToList();
        if (programs.Count == 0)
            return Task.FromResult("Nenhum programa registrado.");

        var sb = new StringBuilder();
        sb.AppendLine($"Programas ({programs.Count}):");
        foreach (var p in programs)
            sb.AppendLine($"- {p.Name}: {string.Join(", ", p.RequiredCapabilities)}");

        return Task.FromResult(sb.ToString().TrimEnd());
    }
}

public sealed class AgentRunProgramTool : AgentTool
{
    private readonly CellProgramRegistry _registry;
    private readonly SimulationRuntime _runtime;

    public AgentRunProgramTool(CellProgramRegistry registry, SimulationRuntime runtime)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public override AgentToolDefinition Definition => new AgentToolDefinition
    {
        Name = "run_program",
        Description = "Executa um programa (Cell Program) registrado no app pelo nome.",
        Parameters =
        {
            ["name"] = new AgentToolParameter
            {
                Type = "string",
                Description = "Nome do programa a executar."
            }
        },
        Required = { "name" }
    };

    public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
    {
        string name;
        using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            name = ReadString(doc.RootElement, "name") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
            return "ERRO: nome do programa vazio.";

        var program = _registry.Resolve(name);
        if (program == null)
        {
            var available = string.Join(", ", _registry.All.Select(p => p.Name));
            return $"ERRO: programa '{name}' não encontrado. Disponíveis: {available}";
        }

        try
        {
            string cellId = "prog-" + Guid.NewGuid().ToString("N")[..8];
            var context = new AgentCellContext(cellId, ct);
            CellProgramResult result = await program.ExecuteAsync(context, ct).ConfigureAwait(false);

            var sb = new StringBuilder();
            sb.Append($"Programa '{name}': ");
            sb.AppendLine(result.IsSuccess ? "sucesso" : "falha");
            if (result.Error != null) sb.AppendLine("Erro: " + result.Error);
            if (result.Data != null) sb.AppendLine("Dados: " + JsonSerializer.Serialize(result.Data));
            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"ERRO ao executar '{name}': {ex.Message}";
        }
    }
}

file sealed class AgentCellContext : IAuraCellContext
{
    public AgentCellContext(string cellId, CancellationToken ct)
    {
        CellId = cellId;
        CancellationToken = ct;
        Device = new NoOpDiagnostic();
    }

    public string CellId { get; }
    public CancellationToken CancellationToken { get; }
    public IDeviceDiagnosticCapability Device { get; }

    private sealed class NoOpDiagnostic : IDeviceDiagnosticCapability
    {
        public string GetDevice() => "unknown";
        public string GetProperties() => "unknown";
        public string GetBattery() => "unknown";
        public string GetNetwork() => "unknown";
    }
}
