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

    private const int DefaultMaxRetainedTerminalRuns = 20;
    private static readonly TimeSpan DefaultRetention = TimeSpan.FromDays(14);

    private readonly ILogger _logger;
    private readonly string _directory;
    private readonly TimeSpan _retention;
    private readonly int _maxRetainedTerminalRuns;
    private readonly object _sync = new();

    public AgentRunStore(ILogger? logger = null, string? directory = null,
        TimeSpan? retention = null, int maxRetainedTerminalRuns = DefaultMaxRetainedTerminalRuns)
    {
        _logger = logger ?? new ConsoleLogger();
        _directory = directory ?? SimulationRuntime.ExpandUserHome("~/AURA/runs");
        _retention = retention is { } configuredRetention && configuredRetention >= TimeSpan.Zero
            ? configuredRetention
            : DefaultRetention;
        _maxRetainedTerminalRuns = Math.Max(1, maxRetainedTerminalRuns);
        Cleanup();
    }

    public string DirectoryPath => _directory;

    /// <summary>
    /// Remove checkpoints terminais antigos, arquivos temporários abandonados e
    /// JSONs corrompidos que já ultrapassaram a retenção. Runs pausados ou em
    /// execução nunca são removidos automaticamente.
    /// </summary>
    public int Cleanup()
    {
        lock (_sync)
        {
            return CleanupNoLock();
        }
    }

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
                CleanupNoLock();
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

    private int CleanupNoLock()
    {
        if (!System.IO.Directory.Exists(_directory)) return 0;

        int removed = 0;
        DateTime cutoffUtc = DateTime.UtcNow - _retention;
        var terminal = new List<(string Path, DateTime LastWriteUtc)>();

        foreach (string path in System.IO.Directory.EnumerateFiles(_directory))
        {
            DateTime lastWriteUtc;
            try { lastWriteUtc = File.GetLastWriteTimeUtc(path); }
            catch { continue; }

            if (path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
            {
                if (lastWriteUtc < cutoffUtc)
                    removed += TryDelete(path);
                continue;
            }

            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            AgentRunState? state;
            try
            {
                state = JsonSerializer.Deserialize<AgentRunState>(File.ReadAllText(path), Options);
            }
            catch
            {
                if (lastWriteUtc < cutoffUtc)
                    removed += TryDelete(path);
                else
                    _logger.Warning("Checkpoint inválido preservado até a retenção: " + path);
                continue;
            }

            if (state == null)
            {
                if (lastWriteUtc < cutoffUtc)
                    removed += TryDelete(path);
                continue;
            }

            if (IsTerminal(state.Status))
            {
                if (lastWriteUtc < cutoffUtc)
                    removed += TryDelete(path);
                else
                    terminal.Add((path, lastWriteUtc));
            }
        }

        foreach (var item in terminal.OrderBy(x => x.LastWriteUtc).Take(Math.Max(0, terminal.Count - _maxRetainedTerminalRuns)))
            removed += TryDelete(item.Path);

        return removed;
    }

    private static bool IsTerminal(string? status)
        => status is AgentRunStatus.Completed or AgentRunStatus.Failed or AgentRunStatus.Cancelled;

    private int TryDelete(string path)
    {
        try
        {
            File.Delete(path);
            return 1;
        }
        catch (Exception ex)
        {
            _logger.Warning("Não foi possível limpar checkpoint '" + path + "': " + ex.Message);
            return 0;
        }
    }

    private string GetPath(string runId)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            runId = runId.Replace(c, '_');
        return Path.Combine(_directory, runId + ".json");
    }
}
