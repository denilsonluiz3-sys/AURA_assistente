using System;
using System.IO;
using System.Linq;
using AURA.Agents;
using Xunit;

namespace AURA.Tests
{
    public class AgentManagerTests
    {
        private sealed class FakeLogger : AURA.Core.Logging.ILogger
        {
            public void Info(string message) { }
            public void Warning(string message) { }
            public void Error(string message) { }
        }

        [Fact]
        public void Resolve_KnownAssistant_ReturnsInfo()
        {
            var manager = new AgentManager(new FakeLogger(),
                new[] {
                    new AgentInfo { Name = "aichat", Executable = "/usr/bin/aichat" }
                });

            Assert.NotNull(manager.Resolve("aichat"));
            Assert.Null(manager.Resolve("nao-existe"));
        }

        [Fact]
        public void Resolve_IsCaseInsensitive()
        {
            var manager = new AgentManager(new FakeLogger(),
                new[] {
                    new AgentInfo { Name = "aichat", Executable = "/usr/bin/aichat" }
                });

            Assert.NotNull(manager.Resolve("AICHAT"));
            Assert.NotNull(manager.Resolve("aIChat"));
        }

        [Fact]
        public void AvailableAssistants_OnlyReturnsThoseWhoseExecutableExists()
        {
            string temp = Path.Combine(Path.GetTempPath(), "aura-agt-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            string exe = Path.Combine(temp, "aichat");
            Directory.CreateDirectory(temp);
            File.WriteAllText(exe, "#!/bin/sh\necho hi\n");
            File.SetUnixFileMode(exe, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            try
            {
                var manager = new AgentManager(new FakeLogger(),
                    new[] {
                        new AgentInfo { Name = "aichat", Executable = exe },
                        new AgentInfo { Name = "termux-ai", Executable = "/caminho/que/nao/existe" }
                    });

                AgentInfo[] available = manager.AvailableAssistants().ToArray();
                Assert.Single(available);
                Assert.Equal("aichat", available[0].Name);
            }
            finally
            {
                Directory.Delete(temp, true);
            }
        }

        [Fact]
        public void ResolveExecutable_ReturnsNullWhenNotFound()
        {
            Assert.Null(AgentManager.ResolveExecutable("binario_que_nao_existe_aichat_xyz"));
        }
    }
}
