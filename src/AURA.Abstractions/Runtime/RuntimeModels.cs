using System;
using System.Collections.Generic;
using AURA.Abstractions.Execution;

namespace AURA.Abstractions.Runtime;

/// <summary>
/// Resultado da identificação de um arquivo (passo 1 do pipeline).
/// Equivalente a <c>Detection</c> do protótipo Python.
/// </summary>
public sealed class Detection
{
    public string Language { get; set; } = "desconhecido";
    public string Extension { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<string> Hints { get; set; } = new();
    public string DetectedBy { get; set; } = string.Empty;

    public bool Known => Language != "desconhecido";
}

public sealed class RuntimeResolution
{
    public string Language { get; set; } = string.Empty;
    public string? Binary { get; set; }
    public string Version { get; set; } = string.Empty;
    public bool Available { get; set; }
    public List<string> Missing { get; set; } = new();
    public string MinVersionRequired { get; set; } = string.Empty;
    public bool VersionSatisfied { get; set; } = true;
    public string InstallHint { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public sealed class Dependency
{
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = "import";
    public string RequiredBy { get; set; } = string.Empty;
    public string InstallCommand { get; set; } = string.Empty;
    public bool IsRuntime { get; set; }

    public override string ToString() => $"{Kind}:{Name}";
}

public sealed class DependencyReport
{
    public string Language { get; set; } = string.Empty;
    public List<Dependency> Dependencies { get; set; } = new();

    public IReadOnlyList<Dependency> Runtimes => Dependencies.FindAll(d => d.IsRuntime);
    public IReadOnlyList<Dependency> Packages => Dependencies.FindAll(d => !d.IsRuntime);
}

public sealed class SyntaxResult
{
    public bool Valid { get; set; }
    public string Tool { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public string Detail { get; set; } = string.Empty;
}

public sealed class CompatReport
{
    public bool RuntimeOk { get; set; }
    public bool DependenciesOk { get; set; }
    public bool AuxiliaryOk { get; set; } = true;
    public List<string> Messages { get; set; } = new();

    public bool Ok => RuntimeOk && DependenciesOk && AuxiliaryOk;
}

public sealed record InstallStep(string What, string Command, bool IsRuntime);

public sealed class InstallPlan
{
    public List<InstallStep> Steps { get; set; } = new();
    public bool Empty => Steps.Count == 0;
}

public sealed class ExecutionOutcome
{
    public bool Success { get; set; }
    public int ExitCode { get; set; } = -1;
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public bool TimedOut { get; set; }
    public string Command { get; set; } = string.Empty;

    public static ExecutionOutcome From(ExecutionResult result, bool timedOut = false, string command = "")
    {
        return new ExecutionOutcome
        {
            Success = result.Success,
            ExitCode = result.ExitCode,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError,
            DurationSeconds = result.Duration.TotalSeconds,
            TimedOut = timedOut,
            Command = command,
        };
    }
}

/// <summary>
/// Resultado do pipeline completo. Mantém etapas, mensagens e timestamps para
/// que o orquestrador possa publicar progresso e a UI possa revisar o resultado.
/// </summary>
public sealed class PipelineReport
{
    public string File { get; set; } = string.Empty;
    public bool Ok { get; set; }
    public Detection? Detection { get; set; }
    public RuntimeResolution? Runtime { get; set; }
    public DependencyReport? Deps { get; set; }
    public SyntaxResult? Syntax { get; set; }
    public CompatReport? Compat { get; set; }
    public InstallPlan? Plan { get; set; }
    public ExecutionOutcome? Outcome { get; set; }
    public bool Executed { get; set; }
    public bool Installed { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAtUtc { get; set; }
    public List<string> Steps { get; set; } = new();
    public List<string> Messages { get; set; } = new();

    public void Log(string message) => Messages.Add(message);
    public void Finish() => FinishedAtUtc = DateTime.UtcNow;
}
