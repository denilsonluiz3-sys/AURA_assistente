using AURA.Core.DependencyInjection;
using Xunit;

namespace AURA.Tests
{
    public class ServiceContainerTests
    {
        private interface ISample
        {
            string Value { get; }
        }

        private sealed class Sample : ISample
        {
            public string Value { get; set; }
        }

        [Fact]
        public void RegisterInstance_ThenResolve_ReturnsSameInstance()
        {
            var container = new ServiceContainer();
            var instance = new Sample { Value = "hello" };

            container.RegisterInstance<ISample>(instance);

            Assert.Same(instance, container.Resolve<ISample>());
        }

        [Fact]
        public void RegisterFactory_ResolvesLazilyAndCaches()
        {
            var container = new ServiceContainer();
            int callCount = 0;

            container.RegisterFactory<ISample>(() =>
            {
                callCount++;
                return new Sample { Value = "lazy" };
            });

            ISample first = container.Resolve<ISample>();
            ISample second = container.Resolve<ISample>();

            Assert.Equal(1, callCount);
            Assert.Same(first, second);
        }

        [Fact]
        public void Resolve_WithoutRegistration_Throws()
        {
            var container = new ServiceContainer();

            Assert.Throws<System.InvalidOperationException>(() => container.Resolve<ISample>());
        }
    }
}
