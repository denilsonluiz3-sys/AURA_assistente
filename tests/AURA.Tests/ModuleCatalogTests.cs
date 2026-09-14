using System.Linq;
using AURA.Modules;
using Xunit;

namespace AURA.Tests
{
    public class ModuleCatalogTests
    {
        [Fact]
        public void GetAll_ReturnsExpectedNumberOfModules()
        {
            Assert.Equal(12, ModuleCatalog.GetAll().Count);
        }

        [Theory]
        [InlineData("browser")]
        [InlineData("modules")]
        [InlineData("system")]
        [InlineData("ai")]
        [InlineData("memory")]
        [InlineData("executors")]
        [InlineData("terminal")]
        [InlineData("cells")]
        [InlineData("logs")]
        [InlineData("windows")]
        [InlineData("automation")]
        [InlineData("plugins")]
        public void GetById_FindsEachExpectedModule(string id)
        {
            ModuleInfo module = ModuleCatalog.GetById(id);

            Assert.NotNull(module);
            Assert.Equal(id, module.Id, ignoreCase: true);
        }

        [Fact]
        public void GetById_ReturnsNullForUnknownId()
        {
            Assert.Null(ModuleCatalog.GetById("DoesNotExist"));
        }

        [Fact]
        public void CoreModules_AreExactlyBrowserAndModules()
        {
            Assert.Equal(2, ModuleCatalog.GetCore().Count);
            Assert.All(ModuleCatalog.GetCore(), m =>
            {
                Assert.True(m.IsCore);
                Assert.True(string.IsNullOrWhiteSpace(m.PackageUrl));
            });
        }

        [Fact]
        public void DownloadableModules_HavePackageUrl()
        {
            Assert.Equal(7, ModuleCatalog.GetDownloadable().Count);
            Assert.All(ModuleCatalog.GetDownloadable(), m =>
            {
                Assert.False(m.IsCore);
                Assert.False(string.IsNullOrWhiteSpace(m.PackageUrl));
                Assert.False(string.IsNullOrWhiteSpace(m.PackageVersion));
                Assert.True(m.Features != null && m.Features.Count > 0);
            });
        }

        [Fact]
        public void NonCoreModules_HaveAtLeastOneImplementationStep()
        {
            Assert.All(ModuleCatalog.GetAll().Where(m => !m.IsCore),
                m => Assert.True(m.ImplementationSteps != null && m.ImplementationSteps.Any()));
        }

        [Fact]
        public void PlannedModules_HaveNoPackageUrl()
        {
            Assert.All(ModuleCatalog.GetAll().Where(m => m.Status == ModuleStatus.Planejado),
                m => Assert.True(string.IsNullOrWhiteSpace(m.PackageUrl)));
        }
    }
}
