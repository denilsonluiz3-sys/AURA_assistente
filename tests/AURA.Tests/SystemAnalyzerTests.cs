using AURA.SystemInfo;
using Xunit;

namespace AURA.Tests
{
    public class SystemAnalyzerTests
    {
        [Fact]
        public void Analyze_ReturnsProcessorCountGreaterThanZero()
        {
            var analyzer = new SystemAnalyzer();

            SystemDiagnosticsResult result = analyzer.Analyze();

            Assert.True(result.ProcessorCount > 0);
        }

        [Fact]
        public void Analyze_ReturnsNonEmptyOperatingSystemDescription()
        {
            var analyzer = new SystemAnalyzer();

            SystemDiagnosticsResult result = analyzer.Analyze();

            Assert.False(string.IsNullOrWhiteSpace(result.OperatingSystem));
        }
    }
}
