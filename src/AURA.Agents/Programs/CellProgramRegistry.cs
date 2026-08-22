using System;
using System.Collections.Generic;
using AURA.Abstractions;

namespace AURA.Agents.Programs;

public sealed class CellProgramRegistry
{
    private readonly Dictionary<string, IAuraCellProgram> _programs = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IAuraCellProgram program)
    {
        if (program is null) throw new ArgumentNullException(nameof(program));
        if (string.IsNullOrWhiteSpace(program.Name)) throw new ArgumentException("Program name cannot be empty.", nameof(program));
        _programs[program.Name.Trim()] = program;
    }

    public IAuraCellProgram? Resolve(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return _programs.TryGetValue(name.Trim(), out var program) ? program : null;
    }

    public IReadOnlyCollection<IAuraCellProgram> All => _programs.Values;
}
