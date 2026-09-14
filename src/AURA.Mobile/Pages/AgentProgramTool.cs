using System;
using System.Linq;
using System.Collections.Generic;
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

    public AgentListProgramsTool(CellProgramRegistry registry) => _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    public override AgentToolDefinition Definition => new AgentToolDefinition
    {
        Name = "list_programs",
        Description = "Lista todos os programas (Cell Programs) disponíveis no app.",
        Parameters = { }
    };

    public override Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
    {
        var programs = _registry.All.ToList();
        if (programs.Count == 0) return Task.FromResult("Nenhum programa registrado.");
        var sb = new StringBuilder();
        sb.AppendLine($"Programas ({programs.Count}):");
        foreach (var p in programs) sb.AppendLine($"- {p.Name}: {string.Join(", ", p.RequiredCapabilities)}");
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
        Description = "Executa um Cell Program pela AURA. Envie os argumentos no mesmo objeto: browser-open usa url; browser-read/click/type usam selector; browser-type também usa text; browser-scroll usa pixels; browser-wait usa milliseconds. Não execute programas sem os argumentos exigidos.",
        Parameters =
        {
            ["name"] = new AgentToolParameter { Type = "string", Description = "Nome registrado do programa." },
            ["url"] = new AgentToolParameter { Type = "string", Description = "URL http/https para browser-open." },
            ["selector"] = new AgentToolParameter { Type = "string", Description = "Seletor CSS para browser-read, browser-click ou browser-type." },
            ["text"] = new AgentToolParameter { Type = "string", Description = "Texto para browser-type." },
            ["pixels"] = new AgentToolParameter { Type = "integer", Description = "Pixels para browser-scroll." },
            ["milliseconds"] = new AgentToolParameter { Type = "integer", Description = "Milissegundos para browser-wait." }
        },
        Required = { "name" }
    };

    public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
    {
        string name;
        var arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
        {
            name = ReadString(doc.RootElement, "name") ?? string.Empty;
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.NameEquals("name")) continue;
                if (property.Value.ValueKind == JsonValueKind.String)
                    arguments[property.Name] = property.Value.GetString() ?? string.Empty;
                else if (property.Value.ValueKind == JsonValueKind.Number
                         && property.Value.TryGetInt32(out int number))
                    arguments[property.Name] = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        if (string.IsNullOrWhiteSpace(name)) return "ERRO: nome do programa vazio.";
        var program = _registry.Resolve(name);
        if (program == null)
        {
            var available = string.Join(", ", _registry.All.Select(p => p.Name));
            return $"ERRO: programa '{name}' não encontrado. Disponíveis: {available}";
        }

        string? correlationId = _coordinator?.BeginManual("Programa: " + program.Name, "run_program");
        try
        {
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
            IAuraCellContext context = contextFactory.Create(cellId, ct, arguments);
            CellProgramResult result = await runner.RunAsync(program, context, ct).ConfigureAwait(false);

            var sb = new StringBuilder();
            sb.Append($"Programa '{name}': ");
            sb.AppendLine(result.IsSuccess ? "sucesso" : "falha");
            if (result.Error != null) sb.AppendLine("Erro: " + result.Error);
            if (result.Data != null) sb.AppendLine("Dados: " + JsonSerializer.Serialize(result.Data));
            if (correlationId != null) _coordinator!.CompleteManual(correlationId, result.IsSuccess, result.IsSuccess ? "concluído" : (result.Error ?? "falhou"));
            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            if (correlationId != null) _coordinator!.CompleteManual(correlationId, false, ex.Message);
            return $"ERRO ao executar '{name}': {ex.Message}";
        }
    }
}
