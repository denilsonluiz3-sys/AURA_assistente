using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AURA.Installer;
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
    }
}
