using System;
using System.IO;
using System.Text.Json;
using AURA.Core.Logging;
using AURA.Modules;
using AURA.Modules.Loja;
using Xunit;

namespace AURA.Tests
{
    public class LojaLocalResolverTests : IDisposable
    {
        private readonly string _tempRoot;
        private readonly string _lojaRoot;
        private readonly string _pluginsRoot;
        private readonly string _packagesDir;

        public LojaLocalResolverTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "aura_loja_tests_" + Guid.NewGuid().ToString("N"));
            _lojaRoot = Path.Combine(_tempRoot, "loja");
            _pluginsRoot = Path.Combine(_tempRoot, "plugins");
            _packagesDir = Path.Combine(_tempRoot, "packages");

            Directory.CreateDirectory(_lojaRoot);
            Directory.CreateDirectory(_pluginsRoot);
            Directory.CreateDirectory(_packagesDir);
        }

        [Fact]
        public void InstallFromLoja_IdNotInCatalog_ThrowsBeforeCopyingAnything()
        {
            // arrange: create loja entry for fake id
            string id = "modulo-fake";
            string entry = Path.Combine(_lojaRoot, id);
            Directory.CreateDirectory(entry);
            Directory.CreateDirectory(Path.Combine(entry, "payload"));
            File.WriteAllText(Path.Combine(entry, "payload", "Fake.dll"), "dll");
            var manifest = new LojaEntry { Id = id, PayloadFiles = { "Fake.dll" } };
            File.WriteAllText(Path.Combine(entry, "manifest.json"), JsonSerializer.Serialize(manifest));

            var logger = new ConsoleLogger();
            var resolver = new LojaLocalResolver(logger, _lojaRoot, _packagesDir, _pluginsRoot, getById: (x) => null);

            // act/assert
            Assert.Throws<InvalidOperationException>(() => resolver.InstallFromLoja(id));

            // ensure nothing copied
            Assert.False(File.Exists(Path.Combine(_pluginsRoot, "Fake.dll")));
        }

        [Fact]
        public void InstallFromLoja_CopiesPayloadAndWritesModuleJson()
        {
            // pick a real id from ModuleCatalog that exists
            string id = "executors";

            // arrange loja entry
            string entry = Path.Combine(_lojaRoot, id);
            Directory.CreateDirectory(entry);
            string payloadDir = Path.Combine(entry, "payload");
            Directory.CreateDirectory(payloadDir);
            File.WriteAllText(Path.Combine(payloadDir, "Executors.dll"), "dummy");
            var manifest = new LojaEntry { Id = id, PayloadFiles = { "Executors.dll" } };
            File.WriteAllText(Path.Combine(entry, "manifest.json"), JsonSerializer.Serialize(manifest));

            var logger = new ConsoleLogger();
            var resolver = new LojaLocalResolver(logger, _lojaRoot, _packagesDir, _pluginsRoot);

            resolver.InstallFromLoja(id);

            // assert file copied
            Assert.True(File.Exists(Path.Combine(_pluginsRoot, "Executors.dll")));

            // assert module.json exists in packages dir
            string moduleJson = Path.Combine(_packagesDir, id, "module.json");
            Assert.True(File.Exists(moduleJson));
            string json = File.ReadAllText(moduleJson);
            Assert.Contains("id", json);
            Assert.Contains(id, json);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true);
            }
            catch { }
        }
    }
}
