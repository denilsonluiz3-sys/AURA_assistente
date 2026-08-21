using System;
using System.Collections.Generic;
using AURA.Abstractions;

namespace AURA.Agents.Programs;

public sealed class CellProgramRegistry
{
    private readonly Dictionary<string, IAuraCellProgram> _programs = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAuraCellProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        if (string.IsNullOrWhiteSpace(program.Name))
            throw new ArgumentException("Programa sem nome.", nameof(program));

        _programs[program.Name.Trim()] = program;
    }

    public IAuraCellProgram? Resolve(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null :
        _programs.TryGetValue(name.Trim(), out var program) ? program : null;

    public IReadOnlyCollection<IAuraCellProgram> All => _programs.Values;
}
