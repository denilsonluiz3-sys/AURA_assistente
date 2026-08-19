using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AURA.Core.Launchers;
using AURA.Core.Logging;
using AURA.Core.Runtime;
using Xunit;

namespace AURA.Tests
{
    /// <summary>
    /// Fase 3 do roadmap (pergunta.txt): "Uma pessoa consegue resolver alguma
    /// coisa usando a AURA?" — prova o caso de uso real ponta a ponta:
    ///   arquivo/script -> AURA -> Runner (escolhe launcher) -> célula ->
    ///   processo -> resultado legível no log.
    /// O runtime é o mesmo usado no APK (SimulationRuntime + DirectoryCellBackend).
    /// </summary>
    public class EndToEndRunTests
    {
        private const string ScriptText =
            "#!/bin/sh\n" +
            "echo 'resultado-do-caso-de-uso-real'\n" +
            "echo 'erro-simulado' 1>&2\n";

        [Fact]
        public async Task RunScriptFile_EndToEnd_ProducesReadableLog()
        {
            string root = Path.Combine(Path.GetTempPath(), "aura_e2e_" + Path.GetRandomFileName());
            Directory.CreateDirectory(root);

            try
            {
                string script = Path.Combine(root, "meu-script.sh");
                File.WriteAllText(script, ScriptText);

                var runtime = new SimulationRuntime(new ConsoleLogger(),
                    Path.Combine(root, "cells"), new DirectoryCellBackend(), persist: false);

                // AURA decide como rodar: Runner resolve o launcher pelo arquivo.
                var runner = new Runner(new ILauncher[]
                {
                    new ShellScriptLauncher()
                });
                Assert.True(runner.CanRun(script));

                Cell cell = await runner.RunAsync(runtime, id: "caso-real", filePath: script);

                Assert.NotNull(cell);
                Assert.NotEqual(0, cell.Id.Length);
                Assert.True(cell.ProcessId > 0);

                // Aguarda a célula atingir estado terminal (WatchCellAsync roda
                // em background e registra o fim do processo).
                CellState terminal = await WaitTerminalStateAsync(runtime, cell.Id, 5000);
                Assert.True(terminal == CellState.Stopped || terminal == CellState.Crashed,
                    "Célula deveria ter terminado (estado: " + terminal + ")");

                // O resultado aparece no log da célula — legível pelo usuário.
                // A saída assíncrona (stdout/stderr) é drenada um instante após
                // o processo sair; aguarda até ambos os fluxos aparecerem.
                string log = await WaitLogContainsAsync(runtime, cell.Id, 3000,
                    "resultado-do-caso-de-uso-real", "erro-simulado");
                Assert.Contains("resultado-do-caso-de-uso-real", log);
                Assert.Contains("erro-simulado", log);

                runtime.Dispose();
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
        }

        [Fact]
        public void PythonLauncher_ResolvesInterpreter_AndBuildsCommand()
        {
            var launcher = new PythonLauncher();

            Assert.Contains(".py", launcher.SupportedExtensions);
            Assert.True(launcher.Supports("meu-script.py"));

            CellCommand command = launcher.BuildCommand("/tmp/meu-script.py", "--arg 1");
            Assert.False(string.IsNullOrWhiteSpace(command.FileName));
            Assert.Contains("meu-script.py", command.Arguments);
            Assert.Contains("--arg 1", command.Arguments);
        }

        [Fact]
        public void Runner_DoesNotRunUnknownExtensions()
        {
            var runner = new Runner(new ILauncher[] { new PythonLauncher() });

            Assert.False(runner.CanRun("arquivo.xyz"));
            Assert.Null(runner.ResolveLauncher("arquivo.xyz"));
        }

        /// <summary>
        /// Teste de concorrência exigido pelo fix de logging: um processo que
        /// gera stdout e stderr simultaneamente em muitas linhas, e o log da
        /// célula deve conter TODAS elas — nenhuma linha pode desaparecer por
        /// corrida de escrita concorrente no arquivo de log.
        /// </summary>
        [Fact]
        public async Task ConcurrentStdoutStderr_NothingIsLost()
        {
            const int Lines = 300;
            string root = Path.Combine(Path.GetTempPath(), "aura_conc_" + Path.GetRandomFileName());
            Directory.CreateDirectory(root);

            try
            {
                string script = Path.Combine(root, "barulhento.sh");
                File.WriteAllText(script, BuildNoisyScript(Lines));

                var runtime = new SimulationRuntime(new ConsoleLogger(),
                    Path.Combine(root, "cells"), new DirectoryCellBackend(), persist: false);

                var runner = new Runner(new ILauncher[] { new ShellScriptLauncher() });
                Cell cell = await runner.RunAsync(runtime, id: "barulhento", filePath: script);

                CellState terminal = await WaitTerminalStateAsync(runtime, cell.Id, 10000);
                Assert.True(terminal == CellState.Stopped || terminal == CellState.Crashed,
                    "Célula deveria ter terminado (estado: " + terminal + ")");

                string log = runtime.ReadCellLog(cell.Id, tailLines: Lines * 2 + 10);

                for (int i = 0; i < Lines; i++)
                {
                    Assert.Contains("out-" + i, log);
                    Assert.Contains("err-" + i, log);
                }

                runtime.Dispose();
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
        }

        private static string BuildNoisyScript(int lines)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("#!/bin/sh");
            sb.AppendLine("i=0");
            sb.AppendLine("while [ $i -lt " + lines + " ]; do");
            sb.AppendLine("  echo \"out-$i\"");
            sb.AppendLine("  echo \"err-$i\" 1>&2");
            sb.AppendLine("  i=$((i + 1))");
            sb.AppendLine("done");
            return sb.ToString();
        }

        private async Task<string> WaitLogContainsAsync(SimulationRuntime runtime, string id, int timeoutMs, params string[] expected)
        {
            var deadline = System.Diagnostics.Stopwatch.StartNew();
            string log = string.Empty;
            while (deadline.ElapsedMilliseconds < timeoutMs)
            {
                log = runtime.ReadCellLog(id, 50);
                if (expected.All(e => log.Contains(e)))
                {
                    return log;
                }

                await Task.Delay(50);
            }

            return log;
        }

        private async Task<CellState> WaitTerminalStateAsync(SimulationRuntime runtime, string id, int timeoutMs)
        {
            var deadline = System.Diagnostics.Stopwatch.StartNew();
            while (deadline.ElapsedMilliseconds < timeoutMs)
            {
                Cell? cell = runtime.GetCell(id);
                if (cell == null)
                {
                    return CellState.Crashed;
                }

                if (cell.State == CellState.Stopped || cell.State == CellState.Crashed)
                {
                    return cell.State;
                }

                await Task.Delay(50);
            }

            return runtime.GetCell(id)?.State ?? CellState.Crashed;
        }

        private sealed class ShellScriptLauncher : ILauncher
        {
            public string[] SupportedExtensions => new[] { ".sh" };

            public bool Supports(string filePath)
            {
                return filePath != null && Path.GetExtension(filePath) == ".sh";
            }

            public CellCommand BuildCommand(string filePath, string arguments)
            {
                return new CellCommand("/bin/sh", "\"" + filePath + "\" " + arguments);
            }
        }
    }
}
