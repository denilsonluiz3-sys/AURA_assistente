using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.Agents
{
    /// <summary>
    /// Adaptador neutro de capacidades Android. A camada Mobile fornece a função concreta via DI.
    /// Isso evita que AURA.Agents dependa de APIs Android.
    /// </summary>
    public sealed class AndroidTool : ITool
    {
        private readonly Func<string, Dictionary<string, string>, CancellationToken, Task<ToolResult>> _handler;

        public AndroidTool(Func<string, Dictionary<string, string>, CancellationToken, Task<ToolResult>> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public string Intent => "android";

        public Task<ToolResult> ExecuteAsync(
            string command,
            Dictionary<string, string> parameters,
            CancellationToken ct = default) =>
            _handler(command, parameters, ct);
    }
}
