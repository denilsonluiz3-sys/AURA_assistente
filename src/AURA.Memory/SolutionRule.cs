using System;
using System.Collections.Generic;

namespace AURA.Memory
{
    /// <summary>
    /// Procedimento conhecido e validado pela AURA.
    ///
    /// Uma regra só deve ser marcada como validada depois que sua execução
    /// produzir o resultado esperado.
    /// </summary>
    public sealed class SolutionRule
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Intent { get; set; } = string.Empty;

        public string Target { get; set; } = string.Empty;

        public string Goal { get; set; } = string.Empty;

        public List<string> Steps { get; set; } =
            new List<string>();

        public List<string> ValidationSteps { get; set; } =
            new List<string>();

        public bool Validated { get; set; }

        public int SuccessCount { get; set; }

        public DateTime? LastValidatedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public Dictionary<string, string> Parameters { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
