using System.Collections.Generic;
using AURA.Core.Abstractions;

namespace AURA.Modules
{
    /// <summary>
    /// Describes one of AURA's optional capability modules shown in the
    /// "Módulos Disponíveis" catalog.
    /// </summary>
    public sealed class ModuleInfo : IModule
    {
        public string Id { get; set; }

        public string DisplayName { get; set; }

        public string Icon { get; set; }

        public string ShortDescription { get; set; }

        public List<string> Includes { get; set; }

        public List<string> ImplementationSteps { get; set; }

        public List<string> AcquiredCapabilities { get; set; }

        public ModuleDifficulty Difficulty { get; set; }

        public string EstimatedTime { get; set; }

        /// <summary>Estado real: implementado (em uso) ou só planejado.</summary>
        public ModuleStatus Status { get; set; } = ModuleStatus.Planejado;
    }
}
