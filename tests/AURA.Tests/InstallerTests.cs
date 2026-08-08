using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions.Execution;
using AURA.Installer;
using AURA.SystemInfo;
using Xunit;

namespace AURA.Tests
{
    public class InstallerTests
    {
        private static string WriteTempFile(string fileName, byte[] content)
        {
            string path = Path.Combine(Path.GetTempPath(), $"aura-installer-test-{System.Guid.NewGuid():N}-{fileName}");
            File.WriteAllBytes(path, content);
            return path;
        }

        private static string WriteTempPythonFile(string content)
        {
            return WriteTempFile("script.py", System.Text.Encoding.UTF8.GetBytes(content));
        }

        [Fact]
        public async Task FileIdentifier_RecognizesPythonByExtensionAndContent()
        {
            string path = WriteTempPythonFile("import requests\n\ndef main():\n    print('ola')\n");
            try
            {
                var identifier = new FileIdentifier();
                var result = await identifier.IdentifyAsync(path);

                Assert.Equal(ArtifactType.Python, result.Type);
                Assert.Equal(1.0, result.Confidence);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task FileIdentifier_RecognizesDotNetAssemblyByPeSignature()
        {
            // "MZ" + padding — não precisa ser uma DLL válida de verdade pra testar a Etapa 1,
            // só a assinatura PE nos primeiros bytes.
            byte[] fakeDll = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
            string path = WriteTempFile("modulo.dll", fakeDll);
            try
            {
                var identifier = new FileIdentifier();
                var result = await identifier.IdentifyAsync(path);

                Assert.Equal(ArtifactType.DotNetAssembly, result.Type);
                Assert.Equal(1.0, result.Confidence);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task FileIdentifier_RecognizesJarByZipSignatureAndExtension()
        {
            byte[] fakeJar = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x00, 0x00 };
            string path = WriteTempFile("app.jar", fakeJar);
            try
            {
                var identifier = new FileIdentifier();
                var result = await identifier.IdentifyAsync(path);

                Assert.Equal(ArtifactType.JarJava, result.Type);
                Assert.Equal(1.0, result.Confidence);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task FileIdentifier_UnknownExtensionWithoutSignature_ReturnsUnknown()
        {
            string path = WriteTempFile("dados.txt", System.Text.Encoding.UTF8.GetBytes("texto qualquer sem pistas"));
            try
            {
                var identifier = new FileIdentifier();
                var result = await identifier.IdentifyAsync(path);

                Assert.Equal(ArtifactType.Unknown, result.Type);
                Assert.Equal(0.0, result.Confidence);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task PythonDependencyAnalyzer_InfersPackagesFromImports_IgnoringStdlib()
        {
            string path = WriteTempPythonFile(
                "import os\n" +
                "import requests\n" +
                "import cv2\n" +
                "from bs4 import BeautifulSoup\n" +
                "from . import utils\n");
            try
            {
                var analyzer = new PythonDependencyAnalyzer();
                var report = await analyzer.AnalyzeAsync(path);

                Assert.False(report.HasRequirementsFile);
                Assert.Contains("requests", report.Dependencies);
                Assert.Contains("opencv-python", report.Dependencies);   // cv2 -> opencv-python
                Assert.Contains("beautifulsoup4", report.Dependencies);  // bs4 -> beautifulsoup4
                Assert.DoesNotContain("os", report.Dependencies);        // stdlib não entra na lista
                Assert.DoesNotContain("utils", report.Dependencies);     // import relativo não é dependência externa
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task PythonDependencyAnalyzer_PrefersRequirementsFileWhenPresent()
        {
            string dir = Path.Combine(Path.GetTempPath(), $"aura-installer-test-{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);
            string scriptPath = Path.Combine(dir, "script.py");
            string requirementsPath = Path.Combine(dir, "requirements.txt");

            File.WriteAllText(scriptPath, "import cv2\n");
            File.WriteAllText(requirementsPath, "# comentário\nrequests==2.31.0\nnumpy>=1.26\n\n");

            try
            {
                var analyzer = new PythonDependencyAnalyzer();
                var report = await analyzer.AnalyzeAsync(scriptPath);

                Assert.True(report.HasRequirementsFile);
                Assert.Equal(new[] { "numpy", "requests" }, report.Dependencies.OrderBy(d => d));
                // Como usou requirements.txt, não deve ter tentado inferir "opencv-python" do import cv2.
                Assert.DoesNotContain("opencv-python", report.Dependencies);
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        public async Task ArtifactAnalysisService_RunsIdentificationAndAnalysisEndToEnd()
        {
            string path = WriteTempPythonFile("import flask\n\napp = flask.Flask(__name__)\n");
            try
            {
                var service = ArtifactAnalysisService.CreateDefault();
                var result = await service.AnalyzeAsync(path);

                Assert.Equal(ArtifactType.Python, result.Identification.Type);
                Assert.NotNull(result.Dependencies);
                Assert.Contains("flask", result.Dependencies!.Dependencies);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task ArtifactAnalysisService_UnsupportedTypeYet_ReturnsNullDependencies()
        {
            byte[] fakeJar = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
            string path = WriteTempFile("app.jar", fakeJar);
            try
            {
                var service = ArtifactAnalysisService.CreateDefault();
                var result = await service.AnalyzeAsync(path);

                Assert.Equal(ArtifactType.JarJava, result.Identification.Type);
                Assert.Null(result.Dependencies); // analisador de Jar ainda não existe nesta etapa
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>Dublê de IToolExecutor pra controlar em teste se o "runtime" está disponível ou não.</summary>
        private sealed class FakeToolExecutor : IToolExecutor
        {
            private readonly bool _available;

            public FakeToolExecutor(string name, bool available)
            {
                Name = name;
                _available = available;
            }

            public string Name { get; }

            public bool IsAvailable() => _available;

            public Task<ExecutionResult> ExecuteAsync(ExecutionRequest request, CancellationToken cancellationToken = default)
                => throw new System.NotImplementedException("Não usado na Etapa 3.");
        }

        private static SystemDiagnosticsResult FakeDiagnostics(double freeDiskSpaceGb) => new()
        {
            OperatingSystem = "teste",
            Architecture = "teste",
            ProcessorCount = 1,
            TotalMemoryGb = 8,
            AvailableMemoryGb = 4,
            SystemDrive = "/",
            TotalDiskSpaceGb = 100,
            FreeDiskSpaceGb = freeDiskSpaceGb,
            MeetsMinimumRequirements = true
        };

        [Fact]
        public async Task PythonEnvironmentSelector_RuntimeAvailableAndDiskOk_IsReadyToInstall()
        {
            var selector = new PythonEnvironmentSelector(
                new FakeToolExecutor("python3", available: true),
                () => FakeDiagnostics(freeDiskSpaceGb: 10));

            var report = new DependencyReport { Dependencies = { "requests", "flask" } };
            var result = await selector.SelectAsync(report);

            Assert.True(result.RuntimeAvailable);
            Assert.Equal("python3", result.RuntimeBinary);
            Assert.True(result.HasEnoughDiskSpace);
            Assert.True(result.ReadyToInstall);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public async Task PythonEnvironmentSelector_RuntimeMissing_SuggestsInstallAndIsNotReady()
        {
            var selector = new PythonEnvironmentSelector(
                new FakeToolExecutor("python3", available: false),
                () => FakeDiagnostics(freeDiskSpaceGb: 10));

            var report = new DependencyReport();
            var result = await selector.SelectAsync(report);

            Assert.False(result.RuntimeAvailable);
            Assert.Null(result.RuntimeBinary);
            Assert.False(result.ReadyToInstall);
            Assert.NotEmpty(result.InstallRuntimeSuggestions);
            Assert.Contains(result.Warnings, w => w.Contains("Python não encontrado"));
        }

        [Fact]
        public async Task PythonEnvironmentSelector_LowDiskSpace_WarnsAndIsNotReady()
        {
            var selector = new PythonEnvironmentSelector(
                new FakeToolExecutor("python3", available: true),
                () => FakeDiagnostics(freeDiskSpaceGb: 0.001)); // ~1MB livre

            var report = new DependencyReport { Dependencies = { "numpy", "pandas", "scipy" } };
            var result = await selector.SelectAsync(report);

            Assert.True(result.RuntimeAvailable);
            Assert.False(result.HasEnoughDiskSpace);
            Assert.False(result.ReadyToInstall);
            Assert.Contains(result.Warnings, w => w.Contains("Espaço livre em disco"));
        }

        [Fact]
        public async Task ArtifactAnalysisService_AnalyzeWithEnvironment_ChainsAllThreeStagesForPython()
        {
            string path = WriteTempPythonFile("import requests\n");
            try
            {
                var service = ArtifactAnalysisService.CreateDefault();
                var result = await service.AnalyzeWithEnvironmentAsync(path);

                Assert.Equal(ArtifactType.Python, result.Identification.Type);
                Assert.NotNull(result.Dependencies);
                Assert.NotNull(result.Environment);
                Assert.Equal(ArtifactType.Python, result.Environment!.ArtifactType);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task ArtifactAnalysisService_AnalyzeWithEnvironment_UnsupportedType_ReturnsNullEnvironment()
        {
            byte[] fakeJar = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
            string path = WriteTempFile("app.jar", fakeJar);
            try
            {
                var service = ArtifactAnalysisService.CreateDefault();
                var result = await service.AnalyzeWithEnvironmentAsync(path);

                Assert.Null(result.Dependencies);
                Assert.Null(result.Environment);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
