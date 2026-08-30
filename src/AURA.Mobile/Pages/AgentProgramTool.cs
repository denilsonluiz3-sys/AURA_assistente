using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;
using AURA.AI;
using AURA.Agents.Programs;
using AURA.Mobile.Services;
using Microsoft.Maui.Controls;

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
    private readonly AgentExecutionCoordinator? _coordinator;

    public AgentRunProgramTool(CellProgramRegistry registry, AURA.Core.Runtime.SimulationRuntime runtime, AgentExecutionCoordinator? coordinator = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _ = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _coordinator = coordinator;
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

        // Registra a execução no Coordinator ANTES de rodar, para que uma bolha de
        // capacidade ("Programa: <nome>") apareça no chat assim que ela começa,
        // e não só depois que o resultado inteiro já estiver pronto.
        string? correlationId = _coordinator?.BeginManual("Programa: " + program.Name, "run_program");

        try
        {
            // Usa o mesmo contexto real e o mesmo CellProgramRunner da tela Programas.
            // Isso mantém as capabilities Android reais e passa pelo PolicyGuard.
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var contextFactory = services?.GetService(typeof(IAuraCellContextFactory)) as IAuraCellContextFactory;
            var runner = services?.GetService(typeof(CellProgramRunner)) as CellProgramRunner;

            if (contextFactory == null)
            {
                if (correlationId != null) _coordinator!.CompleteManual(correlationId, false, "contexto indisponível");
                return "ERRO: contexto de execução de programas indisponível neste dispositivo.";
            }
            if (runner == null)
            {
                if (correlationId != null) _coordinator!.CompleteManual(correlationId, false, "executor indisponível");
                return "ERRO: executor de programas indisponível neste dispositivo.";
            }

            string cellId = correlationId ?? "prog-" + Guid.NewGuid().ToString("N")[..8];
            IAuraCellContext context = contextFactory.Create(cellId, ct);
            CellProgramResult result = await runner.RunAsync(program, context, ct).ConfigureAwait(false);

            var sb = new StringBuilder();
            sb.Append($"Programa '{name}': ");
            sb.AppendLine(result.IsSuccess ? "sucesso" : "falha");
            if (result.Error != null) sb.AppendLine("Erro: " + result.Error);
            if (result.Data != null) sb.AppendLine("Dados: " + JsonSerializer.Serialize(result.Data));

            if (correlationId != null)
                _coordinator!.CompleteManual(correlationId, result.IsSuccess, result.IsSuccess ? "concluído" : (result.Error ?? "falhou"));

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            if (correlationId != null) _coordinator!.CompleteManual(correlationId, false, ex.Message);
            return $"ERRO ao executar '{name}': {ex.Message}";
        }
    }
}
