using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions;
using AURA.Abstractions.Execution;
using AURA.Core.Abstractions;

namespace AURA.Agents;

/// <summary>Ferramenta Android do Kernel. Não depende de APIs Android.</summary>
public sealed class AndroidKernelTool : ITool
{
    private readonly IAndroidCapabilityService _service;
    public string Intent => "android";

    public AndroidKernelTool(IAndroidCapabilityService service)
        => _service = service ?? throw new ArgumentNullException(nameof(service));

    public Task<ToolResult> ExecuteAsync(string command, Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        string action = parameters.TryGetValue("action", out var value) ? value : "all";
        string text = parameters.TryGetValue("text", out var textValue) ? textValue : string.Empty;
        int ms = parameters.TryGetValue("milliseconds", out var msValue) && int.TryParse(msValue, out var parsed) ? parsed : 500;
        try
        {
            string result = action.ToLowerInvariant() switch
            {
                "battery" => _service.GetBattery(),
                "light" => _service.GetLight(),
                "accelerometer" => _service.GetAccelerometer(),
                "gyroscope" => _service.GetGyroscope(),
                "magnetometer" => _service.GetMagnetometer(),
                "location" => _service.GetLocation(),
                "camera" => _service.GetCameras(),
                "audio" => _service.GetAudio(),
                "bluetooth" => _service.GetBluetooth(),
                "clipboard" => _service.GetClipboard(),
                "clipboard_set" => _service.SetClipboard(text),
                "notification" => _service.Notify("AURA", text),
                "vibrate" => _service.Vibrate(ms),
                "network" => _service.GetNetwork(),
                "device" => _service.GetDevice(),
                "apps" => _service.GetApps(),
                "properties" => _service.GetProperties(),
                "memory" => _service.GetMemory(),
                "storage" => _service.GetStorage(),
                _ => _service.GetAll()
            };
            return Task.FromResult(new ToolResult(true, result));
        }
        catch (Exception ex) { return Task.FromResult(new ToolResult(false, "Erro Android: " + ex.Message)); }
    }
}

public sealed class KernelSearchTool : ITool
{
    private readonly IWebSearch _search;
    public string Intent => "search";
    public KernelSearchTool(IWebSearch search) => _search = search ?? throw new ArgumentNullException(nameof(search));

    public async Task<ToolResult> ExecuteAsync(string command, Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        string query = parameters.TryGetValue("query", out var value) && !string.IsNullOrWhiteSpace(value) ? value : command;
        try { return new ToolResult(true, await _search.SearchAsync(query, ct)); }
        catch (Exception ex) { return new ToolResult(false, "Erro na busca: " + ex.Message); }
    }
}

public sealed class KernelShellTool : ITool
{
    private readonly IToolExecutor _shell;
    public string Intent => "shell";
    public KernelShellTool(IToolExecutor shell) => _shell = shell ?? throw new ArgumentNullException(nameof(shell));

    public async Task<ToolResult> ExecuteAsync(string command, Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        try
        {
            var request = new ExecutionRequest
            {
                Command = command,
                WorkingDirectory = AgentWorkspace.ActiveRoot,
                Timeout = TimeSpan.FromSeconds(60)
            };
            ExecutionResult result = await _shell.ExecuteAsync(request, ct);
            return new ToolResult(result.Success, result.CombineOutput());
        }
        catch (Exception ex) { return new ToolResult(false, "Erro no shell: " + ex.Message); }
    }
}

public sealed class KernelFileTool : ITool
{
    private readonly string _root;
    public string Intent => "file";
    public KernelFileTool(string root) => _root = root ?? throw new ArgumentNullException(nameof(root));

    public Task<ToolResult> ExecuteAsync(string command, Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        string action = parameters.TryGetValue("action", out var a) ? a : "list";
        string relative = parameters.TryGetValue("path", out var p) ? p : string.Empty;
        try
        {
            string path = Resolve(relative);
            switch (action.ToLowerInvariant())
            {
                case "read":
                    return Task.FromResult(new ToolResult(true, File.ReadAllText(path)));
                case "write":
                    string content = parameters.TryGetValue("content", out var c) ? c : string.Empty;
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, content);
                    return Task.FromResult(new ToolResult(true, "Arquivo criado: " + relative));
                default:
                    if (!Directory.Exists(path)) return Task.FromResult(new ToolResult(false, "Diretório não encontrado: " + relative));
                    return Task.FromResult(new ToolResult(true, string.Join(Environment.NewLine, Directory.GetFileSystemEntries(path))));
            }
        }
        catch (Exception ex) { return Task.FromResult(new ToolResult(false, "Erro de arquivo: " + ex.Message)); }
    }

    private string Resolve(string relative)
    {
        string fullRoot = Path.GetFullPath(_root);
        string candidate = Path.GetFullPath(Path.Combine(fullRoot, relative ?? string.Empty));
        if (!candidate.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(candidate, fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Caminho fora do workspace.");
        return candidate;
    }
}

public sealed class KernelConversationTool : ITool
{
    public string Intent => "conversar";
    public Task<ToolResult> ExecuteAsync(string command, Dictionary<string, string> parameters, CancellationToken ct = default)
        => Task.FromResult(new ToolResult(true, "AURA: não encontrei uma ação local determinística para esse comando."));
}
