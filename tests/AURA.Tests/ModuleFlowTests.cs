using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Configuration;
using AURA.Core.Events;
using AURA.Core.Logging;
using AURA.Modules;
using Xunit;

namespace AURA.Tests
{
    /// <summary>
    /// Fluxo ponta a ponta da Fase 1 (roadmap): manifesto remoto -> baixar ->
    /// aplicar -> persistir em modules.json -> função desbloqueada (gating) ->
    /// remover. Usa o manifest real do repositório servido por um handler fake,
    /// então valida o conteúdo real sem depender de rede na execução dos testes.
    /// </summary>
    public class ModuleFlowTests
    {
        private static string? RepoRoot()
        {
            string? dir = AppContext.BaseDirectory;
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "modules", "packages", "executors", "module.json");
                if (File.Exists(candidate))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        /// <summary>Handler que serve o manifest real do repo para a URL esperada do catálogo.</summary>
        private sealed class PackageHandler : HttpMessageHandler
        {
            private readonly byte[] _package;
            private readonly string _url;
            public int Calls;

            public PackageHandler(string packagePath, string url)
            {
                _package = File.ReadAllBytes(packagePath);
                _url = url;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                HttpResponseMessage response;
                if (request.RequestUri != null && string.Equals(request.RequestUri.AbsoluteUri, _url, System.StringComparison.OrdinalIgnoreCase))
                {
                    response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(_package)
                    };
                }
                else
                {
                    response = new HttpResponseMessage(HttpStatusCode.NotFound);
                }
                return Task.FromResult(response);
            }
        }

        [Fact]
        public async Task ExecutorsModule_FullFlow_FromRemoteManifestToGatingToRemove()
        {
            string repo = Assert.IsType<string>(RepoRoot());
            ModuleInfo info = ModuleCatalog.GetById("executors");
            Assert.NotNull(info);
            string packagePath = Path.Combine(repo, "modules", "packages", "executors", "module.json");
            Assert.True(File.Exists(packagePath));

            string root = Path.Combine(Path.GetTempPath(), "aura_flow_" + Path.GetRandomFileName());
            string packagesDir = Path.Combine(root, "modules");
            string modulesPath = Path.Combine(root, "config", "modules.json");
            var events = new EventBus();
            var handler = new PackageHandler(packagePath, info.PackageUrl);
            var manager = new ModuleManager(new ConsoleLogger(), packagesDir, modulesPath, events, handler);

            try
            {
                // Estado inicial: nada baixado/aplicado.
                Assert.False(manager.IsDownloaded("executors"));
                Assert.False(manager.IsApplied("executors"));

                // Gating inicial: aba de Executores NÃO deve aparecer.
                Assert.False(IsTabVisible(manager, "executors"));

                // Download do manifesto remoto (handler servindo o arquivo real).
                await manager.DownloadAsync("executors");
                Assert.True(handler.Calls == 1);
                Assert.True(manager.IsDownloaded("executors"));
                Assert.False(manager.IsApplied("executors"));

                // Manifest real validado: id confere com o catálogo.
                string saved = File.ReadAllText(manager.GetPackagePath("executors"));
                Assert.Contains("\"id\": \"executors\"", saved);

                // Aplicar -> persiste em modules.json -> função desbloqueada (gating).
                manager.Apply("executors");
                Assert.True(manager.IsApplied("executors"));
                Assert.True(IsTabVisible(manager, "executors"));

                ModulesConfiguration persisted = new ConfigLoader(new ConsoleLogger())
                    .LoadModules(modulesPath);
                Assert.True(persisted.Modules.IsEnabled("executors"));

                // Evento público de mudança de estado foi emitido.
                ModuleStateChangedEvent? last = null;
                events.Subscribe<ModuleStateChangedEvent>(e => last = e);
                manager.Remove("executors");
                Assert.NotNull(last);
                Assert.False(last!.Applied);

                // Remover -> desabilita + limpa pacote -> gating volta a esconder.
                Assert.False(manager.IsApplied("executors"));
                Assert.False(manager.IsDownloaded("executors"));
                Assert.False(IsTabVisible(manager, "executors"));
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
        }

        private static bool IsTabVisible(ModuleManager manager, string moduleId)
        {
            return manager.IsApplied(moduleId);
        }
    }
}
