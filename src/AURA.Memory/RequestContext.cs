using System;
using System.Collections.Generic;

namespace AURA.Memory
{
    /// <summary>
    /// Representa a solicitação do usuário em formato estruturado.
    /// A intenção é separar a solicitação textual dos procedimentos
    /// que a AURA já conhece e consegue executar.
    /// </summary>
    public sealed class RequestContext
    {
        public string Intent { get; set; } = string.Empty;

        public string Target { get; set; } = string.Empty;

        public string Goal { get; set; } = string.Empty;

        public string Workspace { get; set; } = string.Empty;

        public List<string> Files { get; set; } = new List<string>();

        public List<string> Constraints { get; set; } = new List<string>();

        public List<string> Validation { get; set; } = new List<string>();

        public Dictionary<string, string> Parameters { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool RequiresAiFallback { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
