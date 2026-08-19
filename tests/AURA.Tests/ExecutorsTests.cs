using System.Linq;
using System.Threading.Tasks;
using AURA.Abstractions.Execution;
using AURA.Modules.Executors;
using Xunit;

namespace AURA.Tests
{
    public class ExecutorsTests
    {
        private static IToolExecutor[] AllExecutors()
        {
            return new IToolExecutor[]
            {
                new ShellExecutor(),
                new GitExecutor(),
                new PythonExecutor(),
                new NodeExecutor()
            };
        }

        [Fact]
        public void AllExecutors_HaveNonEmptyName()
        {
            Assert.All(AllExecutors(), e => Assert.False(string.IsNullOrWhiteSpace(e.Name)));
        }

        [Fact]
        public void ShellExecutor_IsAvailableOnUnix()
        {
            var executor = new ShellExecutor();

            Assert.True(executor.IsAvailable());
        }

        [Fact]
        public async Task ShellExecutor_RunsCommandAndCapturesOutput()
        {
            var executor = new ShellExecutor();

            var result = await executor.ExecuteAsync(new ExecutionRequest
            {
                Command = "printf 'ola-do-shell'"
            });

            Assert.True(result.Success);
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("ola-do-shell", result.StandardOutput);
        }

        [Fact]
        public async Task GitExecutor_RunsVersionAndSucceeds()
        {
            var executor = new GitExecutor();
            if (!executor.IsAvailable())
            {
                return; // git ausente no ambiente: não falha o teste.
            }

            var result = await executor.ExecuteAsync(new ExecutionRequest
            {
                Command = "version"
            });

            Assert.True(result.Success);
            Assert.Contains("git", result.StandardOutput, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PythonExecutor_RunsInlineScript()
        {
            var executor = new PythonExecutor();
            if (!executor.IsAvailable())
            {
                return; // python ausente no ambiente: não falha o teste.
            }

            var result = await executor.ExecuteAsync(new ExecutionRequest
            {
                Command = "-c",
                Arguments = { "print('ola-do-python')" }
            });

            Assert.True(result.Success);
            Assert.Contains("ola-do-python", result.StandardOutput);
        }

        [Fact]
        public async Task NodeExecutor_RunsInlineScript()
        {
            var executor = new NodeExecutor();
            if (!executor.IsAvailable())
            {
                return; // node ausente no ambiente: não falha o teste.
            }

            var result = await executor.ExecuteAsync(new ExecutionRequest
            {
                Command = "-e",
                Arguments = { "console.log('ola-do-node')" }
            });

            Assert.True(result.Success);
            Assert.Contains("ola-do-node", result.StandardOutput);
        }

        [Fact]
        public async Task ShellExecutor_WithEnvironmentVariables_AppliesThem()
        {
            var executor = new ShellExecutor();

            var result = await executor.ExecuteAsync(new ExecutionRequest
            {
                Command = "printf '%s' \"$AURA_SMOKE\"",
                EnvironmentVariables = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "AURA_SMOKE", "valor-ok" }
                }
            });

            Assert.True(result.Success);
            Assert.Contains("valor-ok", result.StandardOutput);
        }

        [Fact]
        public async Task ShellExecutor_Timeout_KillsProcessAndReportsFailure()
        {
            var executor = new ShellExecutor();

            var result = await executor.ExecuteAsync(new ExecutionRequest
            {
                Command = "sleep 30",
                Timeout = System.TimeSpan.FromMilliseconds(300)
            });

            Assert.False(result.Success);
            Assert.Contains("cancelada", result.StandardError);
        }

        [Fact]
        public async Task ShellExecutor_Stderr_IsCapturedSeparately()
        {
            var executor = new ShellExecutor();

            var result = await executor.ExecuteAsync(new ExecutionRequest
            {
                Command = "printf 'aviso-no-stderr' 1>&2"
            });

            Assert.True(result.Success);
            Assert.Contains("aviso-no-stderr", result.StandardError);
            Assert.DoesNotContain("aviso-no-stderr", result.StandardOutput);
        }

        [Fact]
        public async Task ShellExecutor_NonexistentCommand_ReportsFailure()
        {
            var executor = new ShellExecutor();

            var result = await executor.ExecuteAsync(new ExecutionRequest
            {
                Command = "comando-inexistente-xyz-123"
            });

            Assert.False(result.Success);
            Assert.NotEqual(0, result.ExitCode);
            Assert.False(string.IsNullOrWhiteSpace(result.StandardError));
        }

        [Fact]
        public async Task GitExecutor_WithWorkingDirectory_RunsGitStatus()
        {
            var executor = new GitExecutor();
            if (!executor.IsAvailable())
            {
                return;
            }

            string repo = FindRepoRoot();
            var result = await executor.ExecuteAsync(new ExecutionRequest
            {
                Command = "status",
                WorkingDirectory = repo
            });

            Assert.True(result.Success);
        }

        private static string FindRepoRoot()
        {
            string current = System.IO.Directory.GetCurrentDirectory();
            while (true)
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(current, "AURA.sln")))
                {
                    return current;
                }

                string? parent = System.IO.Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(parent) || parent == current)
                {
                    return current;
                }

                current = parent;
            }
        }
    }
}
