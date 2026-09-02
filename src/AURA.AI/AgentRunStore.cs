using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AURA.Core.Logging;
using AURA.Core.Runtime;

namespace AURA.AI;

/// <summary>Estado durável de uma execução do agente.</summary>
public sealed class AgentRunState
{
    public string RunId { get; set; } = string.Empty;
    public string Status { get; set; } = AgentRunStatus.Running;
    public string Goal { get; set; } = string.Empty;
    public int Round { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? LastError { get; set; }
    public List<AgentMessage> Messages { get; set; } = new();
}

public static class AgentRunStatus
{
    public const string Running = "running";
    public const string Paused = "paused";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}

/// <summary>Persistência simples e atômica dos checkpoints do agente.</summary>
public sealed class AgentRunStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    private readonly ILogger _logger;
    private readonly string _directory;
    private readonly object _sync = new();

    public AgentRunStore(ILogger? logger = null, string? directory = null)
    {
        _logger = logger ?? new ConsoleLogger();
        _directory = directory ?? SimulationRuntime.ExpandUserHome("~/AURA/runs");
    }

    public string DirectoryPath => _directory;

    public void Save(AgentRunState state)
    {
        if (state == null) throw new ArgumentNullException(nameof(state));
        if (string.IsNullOrWhiteSpace(state.RunId)) throw new ArgumentException("RunId obrigatório.", nameof(state));

        lock (_sync)
        {
            try
            {
                System.IO.Directory.CreateDirectory(_directory);
                state.UpdatedAtUtc = DateTime.UtcNow;
                string path = GetPath(state.RunId);
                string tmp = path + ".tmp";
                string json = JsonSerializer.Serialize(state, Options);
                File.WriteAllText(tmp, json);
                File.Move(tmp, path, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger.Error("Falha ao persistir run '" + state.RunId + "': " + ex.Message);
            }
        }
    }

    public AgentRunState? Load(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId)) return null;
        lock (_sync)
        {
            try
            {
                string path = GetPath(runId);
                if (!File.Exists(path)) return null;
                return JsonSerializer.Deserialize<AgentRunState>(File.ReadAllText(path), Options);
            }
            catch (Exception ex)
            {
                _logger.Warning("Run '" + runId + "' inválido: " + ex.Message);
                return null;
            }
        }
    }

    public AgentRunState? LoadLatestResumable()
    {
        lock (_sync)
        {
            try
            {
                if (!System.IO.Directory.Exists(_directory)) return null;
                AgentRunState? latest = null;
                foreach (string path in System.IO.Directory.EnumerateFiles(_directory, "*.json"))
                {
                    AgentRunState? state = null;
                    try { state = JsonSerializer.Deserialize<AgentRunState>(File.ReadAllText(path), Options); }
                    catch { /* arquivo isolado não impede os demais */ }
                    if (state == null || state.Status != AgentRunStatus.Paused) continue;
                    if (latest == null || state.UpdatedAtUtc > latest.UpdatedAtUtc) latest = state;
                }
                return latest;
            }
            catch (Exception ex)
            {
                _logger.Warning("Não foi possível localizar runs retomáveis: " + ex.Message);
                return null;
            }
        }
    }

    private string GetPath(string runId)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            runId = runId.Replace(c, '_');
        return Path.Combine(_directory, runId + ".json");
    }
}
