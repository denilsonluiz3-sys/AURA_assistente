using System.IO;
using AURA.Core.Configuration;
using AURA.Core.Logging;
using AURA.Modules;
using Xunit;

namespace AURA.Tests
{
    public class ModuleManagerTests
    {
        private static ModuleManager CreateManager(out string root)
        {
            root = Path.Combine(Path.GetTempPath(), "aura_mod_" + Path.GetRandomFileName());
            string packagesDir = Path.Combine(root, "modules");
            string modulesPath = Path.Combine(root, "config", "modules.json");

            return new ModuleManager(new ConsoleLogger(), packagesDir, modulesPath);
        }

        [Fact]
        public void DownloadableModules_StartNotDownloadedAndNotApplied()
        {
            ModuleManager manager = CreateManager(out string root);
            try
            {
                Assert.False(manager.IsDownloaded("ai"));
                Assert.False(manager.IsApplied("ai"));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void Apply_RequiresDownloadFirst()
        {
            ModuleManager manager = CreateManager(out string root);
            try
            {
                Assert.Throws<System.InvalidOperationException>(() => manager.Apply("ai"));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void Download_SavesPackageAndAllowsApplyThenRemove()
        {
            ModuleManager manager = CreateManager(out string root);
            try
            {
                string packagePath = manager.GetPackagePath("ai");
                Directory.CreateDirectory(Path.GetDirectoryName(packagePath));
                File.WriteAllText(packagePath, "{\"id\":\"ai\",\"name\":\"IA\"}");

                Assert.True(manager.IsDownloaded("ai"));
                Assert.False(manager.IsApplied("ai"));

                manager.Apply("ai");
                Assert.True(manager.IsApplied("ai"));

                ModulesConfiguration persisted = new ConfigLoader(new ConsoleLogger())
                    .LoadModules(Path.Combine(root, "config", "modules.json"));
                Assert.True(persisted.Modules.IsEnabled("ai"));

                manager.Remove("ai");
                Assert.False(manager.IsApplied("ai"));
                Assert.False(File.Exists(packagePath));
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Fact]
        public void CoreModule_CannotBeRemovedOrDownloaded()
        {
            ModuleManager manager = CreateManager(out string root);
            try
            {
                Assert.Throws<System.InvalidOperationException>(() => manager.Remove("browser"));
                Assert.ThrowsAsync<System.InvalidOperationException>(async () => await manager.DownloadAsync("browser")).Wait();
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
