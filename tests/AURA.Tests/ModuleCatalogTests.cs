using System.Linq;
using AURA.Modules;
using Xunit;

namespace AURA.Tests
{
    public class ModuleCatalogTests
    {
        [Fact]
        public void GetAll_ReturnsFiveModules()
        {
            var modules = ModuleCatalog.GetAll();

            Assert.Equal(5, modules.Count);
        }

        [Theory]
        [InlineData("Windows")]
        [InlineData("AI")]
        [InlineData("Automation")]
        [InlineData("Memory")]
        [InlineData("Plugins")]
        public void GetById_FindsEachExpectedModule(string id)
        {
            ModuleInfo module = ModuleCatalog.GetById(id);

            Assert.NotNull(module);
            Assert.Equal(id, module.Id);
        }

        [Fact]
        public void GetById_ReturnsNullForUnknownId()
        {
            Assert.Null(ModuleCatalog.GetById("DoesNotExist"));
        }

        [Fact]
        public void AllModules_HaveAtLeastOneImplementationStep()
        {
            Assert.All(ModuleCatalog.GetAll(), m => Assert.True(m.ImplementationSteps.Any()));
        }
    }
}
