namespace AURA.Core.Configuration
{
    /// <summary>
    /// Tracks which optional capability modules the user has chosen to
    /// prepare/enable, persisted to config/modules.json.
    /// </summary>
    public class ModulesConfiguration
    {
        public ModuleFlags Modules { get; set; }

        public ModulesConfiguration()
        {
            Modules = new ModuleFlags();
        }
    }

    public class ModuleFlags
    {
        public bool Windows { get; set; }

        public bool AI { get; set; }

        public bool Automation { get; set; }

        public bool Memory { get; set; }

        public bool Plugins { get; set; }
    }
}
