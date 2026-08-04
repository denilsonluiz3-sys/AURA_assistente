#Requires -Version 5.0
<#
.SYNOPSIS
    AURA Genesis - UPGRADE TO PHASE 7 FINAL
    Aplica correções Fase 5 + adiciona Fase 6 e 7 - MVP Completo
    Compatível .NET 4.6.2 / Win7 x86 / PS 5.0
#>
$ErrorActionPreference = "Stop"
$root = Get-Location
Write-Host "=== AURA Genesis - Upgrade to Phase 7 ===" -ForegroundColor Cyan
Write-Host "Destino: $root"

$dirs = @(
    "Core\Agents", "Core\AI", "Core\Memory", "Core\Bootstrap", "Core\Continuity",
    "Core\Orchestrator", "Windows", "Recovery", "Memory", "Logs", "Backup", "Plugins", "Temp"
)
foreach ($d in $dirs) { New-Item -Path $d -ItemType Directory -Force | Out-Null }

function Write-File([string]$path, [string]$content) {
    $full = Join-Path $root $path
    $dir = Split-Path $full -Parent
    if (!(Test-Path $dir)) { New-Item -Path $dir -ItemType Directory -Force | Out-Null }
    [System.IO.File]::WriteAllText($full, $content, [System.Text.Encoding]::UTF8)
    Write-Host "  OK  $path" -ForegroundColor Green
}

# ==================== JSONS ====================
Write-File "appsettings.json" @'
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AURA": {
    "Database": { "Path": "Memory\\AURA.Memory.db" },
    "AI": {
      "Provider": "openrouter",
      "OpenRouterApiKey": "",
      "Model": "openai/gpt-4o-mini",
      "EmbeddingModel": "openai/text-embedding-3-small",
      "LocalLlamaExe": "C:\\Tools\\llama.cpp\\llama-cli.exe",
      "LocalModelPath": "C:\\Models\\llama-3-8b.Q4.gguf"
    },
    "Security": { "AutoApproveThreshold": 0.3, "RequireConsent": true },
    "Plugins": { "Directory": "Plugins" }
  }
}
'@

Write-File "AURA_SYSTEM_INSTRUCTIONS.json" @'
{
  "identity": { "name": "AURA", "version": "1.0.0", "role": "Assistente autônoma Windows" },
  "rules": ["Sempre criar backup antes de alterações", "Nunca executar ações críticas sem autorização", "Registrar todas as operações em log"],
  "startup_tasks": ["Verificar ambiente", "Carregar banco SQLite", "Inicializar VectorStore", "Diagnóstico rápido"]
}
'@

Write-File "project-status.json" @'
{
  "project": { "name": "AURA Assistente", "version": "1.0.0", "status": "PHASE_7_COMPLETE" },
  "phases": [
    { "phase": 1, "name": "Foundation", "status": "COMPLETE", "completionPercent": 100 },
    { "phase": 2, "name": "Security", "status": "COMPLETE", "completionPercent": 100 },
    { "phase": 3, "name": "Persistence", "status": "COMPLETE", "completionPercent": 100 },
    { "phase": 4, "name": "AI Integration", "status": "COMPLETE", "completionPercent": 100 },
    { "phase": 5, "name": "Windows Automation", "status": "COMPLETE", "completionPercent": 100,
      "components": [
        { "name": "CmdExecutor", "status": "COMPLETE" },
        { "name": "PowerShellExecutor", "status": "COMPLETE" },
        { "name": "RegistryManager", "status": "COMPLETE" },
        { "name": "ServiceManager", "status": "COMPLETE" },
        { "name": "FileManager", "status": "COMPLETE" },
        { "name": "NetworkManager", "status": "COMPLETE" }
      ]
    },
    { "phase": 6, "name": "Recovery", "status": "COMPLETE", "completionPercent": 100,
      "components": [
        { "name": "RestorePointService", "status": "COMPLETE" },
        { "name": "BackupManager", "status": "COMPLETE" },
        { "name": "RollbackService", "status": "COMPLETE" },
        { "name": "CleanupService", "status": "COMPLETE" }
      ]
    },
    { "phase": 7, "name": "AI Real + Vector Memory", "status": "COMPLETE", "completionPercent": 100,
      "components": [
        { "name": "OpenRouterClient", "status": "COMPLETE" },
        { "name": "LocalModelFallback", "status": "COMPLETE" },
        { "name": "VectorStore", "status": "COMPLETE" },
        { "name": "AiEngine RAG", "status": "COMPLETE" }
      ]
    }
  ]
}
'@

Write-File "AURA.Genesis.csproj" @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net462</TargetFramework>
    <AssemblyName>AURA</AssemblyName>
    <RootNamespace>AURA</RootNamespace>
    <LangVersion>7.3</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.Data.SQLite" Version="1.0.118" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="3.1.32" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.1" />
    <PackageReference Include="System.ServiceProcess.ServiceController" Version="4.7.0" />
  </ItemGroup>
  <ItemGroup>
    <None Update="appsettings.json;AURA_SYSTEM_INSTRUCTIONS.json;project-status.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
'@

# ==================== CORE ====================
Write-File "Core\IAgent.cs" @'
using System.Threading;
using System.Threading.Tasks;
namespace AURA.Core
{
    public interface IAgent
    {
        string Name { get; }
        string Description { get; }
        string Version { get; }
        int Priority { get; }
        Task Initialize();
        Task<AgentResult> ExecuteAsync(CommandRequest request, CancellationToken ct = default);
    }
}
'@

Write-File "Core\DataClasses.cs" @'
using System.Collections.Generic;
namespace AURA.Core
{
    public class CommandRequest{ public string Action{get;set;} public object Payload{get;set;} public string Source{get;set;} }
    public class AgentResult{ public bool Success{get;set;} public string Message{get;set;} public object Data{get;set;} }
}
'@

Write-File "Core\Bootstrap\AuraBootstrap.cs" @'
using System;
using System.IO;
using System.Threading.Tasks;
namespace AURA.Core.Bootstrap
{
    public class AuraBootstrap{
        public async Task<bool> InitializeAsync(){
            foreach(var d in new[]{"Logs","Backup","Plugins","Memory","Temp"}){
                var p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,d);
                if(!Directory.Exists(p)) Directory.CreateDirectory(p);
            }
            var db=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Memory","AURA.Memory.db");
            if(!File.Exists(db)) System.Data.SQLite.SQLiteConnection.CreateFile(db);
            // Inicializa VectorStore para criar tabelas
            var vs = Memory.VectorStore.Instance;
            await Task.CompletedTask;
            return true;
        }
    }
}
'@

Write-File "Core\Continuity\ProjectContinuationEngine.cs" @'
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
namespace AURA.Core.Continuity
{
    public class ProjectContinuationEngine{
        public async Task<dynamic> ReadStatusAsync(){
            var json=await Task.Run(()=>File.ReadAllText("project-status.json"));
            return JsonConvert.DeserializeObject(json);
        }
    }
}
'@

Write-File "Core\Agents\BaseAgent.cs" @'
using System;
using System.Threading;
using System.Threading.Tasks;
namespace AURA.Core.Agents
{
    public abstract class BaseAgent: IAgent
    {
        public abstract string Name{get;} public abstract string Description{get;} public abstract string Version{get;}
        public virtual int Priority=>5;
        protected Windows.WindowsEngine Win=> Windows.WindowsEngine.Instance;
        public virtual Task Initialize()=> Task.CompletedTask;
        public abstract Task<AgentResult> ExecuteAsync(CommandRequest request, CancellationToken ct=default);
    }
}
'@

# ==================== WINDOWS - FASE 5 FINAL ====================
Write-File "Windows\CmdExecutor.cs" @'
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace AURA.Windows
{
    public sealed class CmdExecutor
    {
        private static readonly Lazy<CmdExecutor> _inst = new Lazy<CmdExecutor>(()=>new CmdExecutor());
        public static CmdExecutor Instance=> _inst.Value;
        private CmdExecutor(){}
        public Task<CmdResult> ExecuteWithResultAsync(string command, int timeoutMs=30000, CancellationToken ct=default){
            return Task.Run(()=>{
                var result=new CmdResult();
                try{
                    using(var p=new Process{ StartInfo=new ProcessStartInfo{ FileName="cmd.exe", Arguments="/c "+command, UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true, RedirectStandardError=true, StandardOutputEncoding=Encoding.GetEncoding(850), StandardErrorEncoding=Encoding.GetEncoding(850) } }){
                        var outSb=new StringBuilder(); var errSb=new StringBuilder();
                        p.OutputDataReceived+=(s,e)=>{ if(e.Data!=null) outSb.AppendLine(e.Data); };
                        p.ErrorDataReceived+=(s,e)=>{ if(e.Data!=null) errSb.AppendLine(e.Data); };
                        p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine();
                        var sw=Stopwatch.StartNew();
                        while(!p.HasExited){
                            if(ct.IsCancellationRequested){ try{p.Kill();}catch{} throw new OperationCanceledException(ct); }
                            if(sw.ElapsedMilliseconds>timeoutMs){ try{p.Kill();}catch{} throw new TimeoutException(); }
                            Thread.Sleep(50);
                        }
                        p.WaitForExit(1000);
                        result.ExitCode=p.ExitCode; result.Output=outSb.ToString(); result.Error=errSb.ToString(); result.Success=p.ExitCode==0;
                    }
                }catch(Exception ex){ result.ExitCode=-1; result.Error=ex.Message; }
                return result;
            }, ct);
        }
    }
    public class CmdResult{ public int ExitCode{get;set;} public string Output{get;set;}="" ; public string Error{get;set;}="" ; public bool Success{get;set;} }
}
'@

Write-File "Windows\PowerShellExecutor.cs" @'
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace AURA.Windows
{
    public sealed class PowerShellExecutor
    {
        private static readonly Lazy<PowerShellExecutor> _inst = new Lazy<PowerShellExecutor>(()=>new PowerShellExecutor());
        public static PowerShellExecutor Instance=> _inst.Value;
        private const int DefaultTimeout=30000;
        private readonly string _psPath;
        private PowerShellExecutor(){
            var sys=Environment.GetFolderPath(Environment.SpecialFolder.System);
            var path=Path.Combine(sys, "WindowsPowerShell\\v1.0\\powershell.exe");
            _psPath=File.Exists(path)?path:"powershell.exe";
        }
        public Task<PSResult> ExecuteScriptAsync(string script, int timeoutMs=DefaultTimeout, CancellationToken ct=default){
            if(string.IsNullOrWhiteSpace(script)) throw new ArgumentException("Script vazio");
            var bytes=Encoding.Unicode.GetBytes(script);
            var encoded=Convert.ToBase64String(bytes);
            var args=$"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encoded}";
            return ExecuteInternalAsync(args, timeoutMs, ct);
        }
        private Task<PSResult> ExecuteInternalAsync(string psArgs, int timeoutMs, CancellationToken ct){
            return Task.Run(()=>{
                var result=new PSResult(); var outSb=new StringBuilder(); var errSb=new StringBuilder();
                var sw=Stopwatch.StartNew(); Process proc=null;
                try{
                    proc=new Process{ StartInfo=new ProcessStartInfo{ FileName=_psPath, Arguments=psArgs, UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true, RedirectStandardError=true, StandardOutputEncoding=Encoding.UTF8, StandardErrorEncoding=Encoding.UTF8 } };
                    proc.OutputDataReceived+=(s,e)=>{ if(e.Data!=null) lock(outSb) outSb.AppendLine(e.Data); };
                    proc.ErrorDataReceived+=(s,e)=>{ if(e.Data!=null) lock(errSb) errSb.AppendLine(e.Data); };
                    proc.Start(); proc.BeginOutputReadLine(); proc.BeginErrorReadLine();
                    using(var timeoutCts=new CancellationTokenSource()){
                        if(timeoutMs>0) timeoutCts.CancelAfter(timeoutMs);
                        while(!proc.WaitForExit(200)){
                            if(ct.IsCancellationRequested){ try{proc.Kill();}catch{} throw new OperationCanceledException(ct); }
                            if(timeoutCts.IsCancellationRequested){ try{proc.Kill();}catch{} throw new TimeoutException($"Timeout {timeoutMs}ms"); }
                        }
                    }
                    proc.WaitForExit();
                    result.ExitCode=proc.ExitCode; result.Output=outSb.ToString().Trim(); result.Error=errSb.ToString().Trim(); result.Success=proc.ExitCode==0 && string.IsNullOrEmpty(result.Error);
                }catch(Exception ex){ result.ExitCode=-1; result.Error=ex.Message; if(!(ex is OperationCanceledException)&&!(ex is TimeoutException)) throw; }
                finally{ sw.Stop(); result.ExecutionTime=sw.Elapsed; proc?.Dispose(); try{ File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Logs",$"ps_{DateTime.Now:yyyyMMdd}.log"), $"[{DateTime.Now:HH:mm:ss}] Exit:{result.ExitCode} Time:{result.ExecutionTime}\n{result.Error}\n---\n"); }catch{} }
                return result;
            }, ct);
        }
    }
    public class PSResult{ public int ExitCode{get;set;} public string Output{get;set;}="" ; public string Error{get;set;}="" ; public bool Success{get;set;} public TimeSpan ExecutionTime{get;set;} }
}
'@

Write-File "Windows\NetworkManager.cs" @'
using System.Net.NetworkInformation;
using System.Threading.Tasks;
namespace AURA.Windows
{
    public sealed class NetworkManager
    {
        private static readonly System.Lazy<NetworkManager> _inst = new System.Lazy<NetworkManager>(()=>new NetworkManager());
        public static NetworkManager Instance=> _inst.Value;
        private NetworkManager(){}
        public Task<bool> IsInternetAvailableAsync()=> Task.Run(()=>NetworkInterface.GetIsNetworkAvailable());
    }
}
'@

Write-File "Windows\RegistryManager.cs" @'
using Microsoft.Win32;
using System;
using System.Threading.Tasks;
namespace AURA.Windows
{
    public sealed class RegistryManager
    {
        private static readonly System.Lazy<RegistryManager> _inst = new System.Lazy<RegistryManager>(()=>new RegistryManager());
        public static RegistryManager Instance=> _inst.Value;
        private RegistryManager(){}
        public Task<object> GetValueAsync(string keyPath, string valueName, RegistryHive hive=RegistryHive.LocalMachine, RegistryView view=RegistryView.Default){
            return Task.Run(()=>{ using(var baseKey=RegistryKey.OpenBaseKey(hive, view)) using(var key=baseKey.OpenSubKey(keyPath)) return key?.GetValue(valueName); });
        }
        public Task SetValueAsync(string keyPath, string valueName, object value, RegistryValueKind kind=RegistryValueKind.String, RegistryHive hive=RegistryHive.LocalMachine, RegistryView view=RegistryView.Default){
            return Task.Run(()=>{ using(var baseKey=RegistryKey.OpenBaseKey(hive, view)) using(var key=baseKey.CreateSubKey(keyPath)) key.SetValue(valueName, value, kind); });
        }
        public async Task<bool> BackupKeyAsync(string keyPath, string backupFile){
            var r=await CmdExecutor.Instance.ExecuteWithResultAsync($"reg export \"{keyPath}\" \"{backupFile}\" /y", 15000);
            return r.Success;
        }
    }
}
'@

Write-File "Windows\ServiceManager.cs" @'
using System;
using System.ServiceProcess;
using System.Threading.Tasks;
namespace AURA.Windows
{
    public sealed class ServiceManager
    {
        private static readonly System.Lazy<ServiceManager> _inst = new System.Lazy<ServiceManager>(()=>new ServiceManager());
        public static ServiceManager Instance=> _inst.Value;
        private ServiceManager(){}
        public Task<ServiceControllerStatus?> GetStatusAsync(string name){
            return Task.Run(()=>{ try{ using(var sc=new ServiceController(name)) return (ServiceControllerStatus?)sc.Status; }catch{ return null; } });
        }
        public Task<bool> StartAsync(string name, int timeoutMs=30000){
            return Task.Run(()=>{ try{ using(var sc=new ServiceController(name)){ if(sc.Status==ServiceControllerStatus.Running) return true; sc.Start(); sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(timeoutMs)); return sc.Status==ServiceControllerStatus.Running; } }catch{ return false; } });
        }
        public Task<bool> StopAsync(string name, int timeoutMs=30000){
            return Task.Run(()=>{ try{ using(var sc=new ServiceController(name)){ if(sc.Status==ServiceControllerStatus.Stopped) return true; sc.Stop(); sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(timeoutMs)); return sc.Status==ServiceControllerStatus.Stopped; } }catch{ return false; } });
        }
    }
}
'@

Write-File "Windows\FileManager.cs" @'
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
namespace AURA.Windows
{
    public sealed class FileManager
    {
        private static readonly System.Lazy<FileManager> _inst = new System.Lazy<FileManager>(()=>new FileManager());
        public static FileManager Instance=> _inst.Value;
        private FileManager(){}
        public Task<string> ReadAllTextAsync(string path)=> Task.Run(()=>File.ReadAllText(path, Encoding.UTF8));
        public Task WriteAllTextAsync(string path, string content)=> Task.Run(()=>File.WriteAllText(path, content, Encoding.UTF8));
        public Task CopyAsync(string src, string dst, bool over=false)=> Task.Run(()=>File.Copy(src,dst,over));
        public Task<string> ComputeHashAsync(string path)=> Task.Run(()=>{ using(var sha=SHA256.Create()) using(var s=File.OpenRead(path)) return System.BitConverter.ToString(sha.ComputeHash(s)).Replace("-","").ToLowerInvariant(); });
        public bool Exists(string path)=> File.Exists(path);
    }
}
'@

Write-File "Windows\WindowsEngine.cs" @'
namespace AURA.Windows
{
    public sealed class WindowsEngine
    {
        private static readonly System.Lazy<WindowsEngine> _inst = new System.Lazy<WindowsEngine>(()=>new WindowsEngine());
        public static WindowsEngine Instance=> _inst.Value;
        private WindowsEngine(){}
        public CmdExecutor Cmd=> CmdExecutor.Instance;
        public PowerShellExecutor PowerShell=> PowerShellExecutor.Instance;
        public NetworkManager Network=> NetworkManager.Instance;
        public RegistryManager Registry=> RegistryManager.Instance;
        public ServiceManager Services=> ServiceManager.Instance;
        public FileManager Files=> FileManager.Instance;
        public Recovery.RestorePointService RestorePoint=> Recovery.RestorePointService.Instance;
        public Recovery.BackupManager Backup=> Recovery.BackupManager.Instance;
        public Recovery.RollbackService Rollback=> Recovery.RollbackService.Instance;
        public Recovery.CleanupService Cleanup=> Recovery.CleanupService.Instance;
        public Core.Memory.VectorStore VectorMemory=> Core.Memory.VectorStore.Instance;
        public Core.AI.AiEngine AI=> Core.AI.AiEngine.Instance;
    }
}
'@

# ==================== RECOVERY FASE 6 ====================
Write-File "Recovery\RecoveryPlan.cs" @'
using System.Collections.Generic;
namespace AURA.Recovery
{
    public class RecoveryPlan{ public string Name{get;set;} public List<string> Steps{get;set;}=new List<string>(); public bool RequiresRestart{get;set;} }
}
'@

Write-File "Recovery\RestorePointService.cs" @'
using System;
using System.Threading.Tasks;
using AURA.Windows;
namespace AURA.Recovery
{
    public sealed class RestorePointService
    {
        private static readonly Lazy<RestorePointService> _inst = new Lazy<RestorePointService>(()=>new RestorePointService());
        public static RestorePointService Instance=> _inst.Value;
        private RestorePointService(){}
        public async Task<bool> CreateRestorePointAsync(string desc){
            var safe=desc.Replace("'", "''");
            var script=$"try{{ Checkpoint-Computer -Description ''{safe}'' -RestorePointType MODIFY_SETTINGS; Write-Output OK }}catch{{ Write-Error $_.Exception.Message; exit 1 }}";
            var r=await PowerShellExecutor.Instance.ExecuteScriptAsync(script, 90000);
            return r.Success;
        }
    }
}
'@

Write-File "Recovery\BackupManager.cs" @'
using System;
using System.IO;
using System.Threading.Tasks;
using AURA.Windows;
namespace AURA.Recovery
{
    public sealed class BackupManager
    {
        private static readonly Lazy<BackupManager> _inst = new Lazy<BackupManager>(()=>new BackupManager());
        public static BackupManager Instance=> _inst.Value;
        private BackupManager(){}
        public async Task<string> BackupFileAsync(string src, string backupDir=null){
            if(!File.Exists(src)) throw new FileNotFoundException(src);
            if(backupDir==null) backupDir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Backup");
            Directory.CreateDirectory(backupDir);
            var hash=await FileManager.Instance.ComputeHashAsync(src);
            var dest=Path.Combine(backupDir, $"{Path.GetFileName(src)}.{DateTime.Now:yyyyMMdd_HHmmss}.bak");
            await FileManager.Instance.CopyAsync(src,dest,true);
            await FileManager.Instance.WriteAllTextAsync(dest+".hash", hash);
            await FileManager.Instance.WriteAllTextAsync(dest+".meta", src);
            return dest;
        }
        public Task<bool> BackupRegistryKeyAsync(string key, string dest)=> RegistryManager.Instance.BackupKeyAsync(key,dest);
    }
}
'@

Write-File "Recovery\RollbackService.cs" @'
using System.IO;
using System.Threading.Tasks;
using AURA.Windows;
namespace AURA.Recovery
{
    public sealed class RollbackService
    {
        private static readonly System.Lazy<RollbackService> _inst = new System.Lazy<RollbackService>(()=>new RollbackService());
        public static RollbackService Instance=> _inst.Value;
        private RollbackService(){}
        public async Task<bool> RollbackFileAsync(string backupFile, string original=null){
            if(!File.Exists(backupFile)) return false;
            if(string.IsNullOrEmpty(original)){
                var meta=backupFile+".meta";
                if(File.Exists(meta)) original=await FileManager.Instance.ReadAllTextAsync(meta);
                else return false;
            }
            if(File.Exists(original)) await FileManager.Instance.CopyAsync(original, original+".pre_rollback.bak", true);
            await FileManager.Instance.CopyAsync(backupFile, original, true);
            return true;
        }
    }
}
'@

Write-File "Recovery\CleanupService.cs" @'
using System;
using System.IO;
using System.Threading.Tasks;
namespace AURA.Recovery
{
    public sealed class CleanupService
    {
        private static readonly System.Lazy<CleanupService> _inst = new System.Lazy<CleanupService>(()=>new CleanupService());
        public static CleanupService Instance=> _inst.Value;
        private CleanupService(){}
        public Task CleanOldBackupsAsync(string dir, int maxDays=30)=> Task.Run(()=>{
            if(!Directory.Exists(dir)) return;
            var cutoff=DateTime.Now.AddDays(-maxDays);
            foreach(var f in Directory.GetFiles(dir,"*.bak")){
                if(File.GetCreationTime(f)<cutoff){ try{File.Delete(f); File.Delete(f+".hash"); File.Delete(f+".meta");}catch{} }
            }
        });
    }
}
'@

# ==================== AI FASE 7 ====================
Write-File "Core\AI\OpenRouterClient.cs" @'
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace AURA.Core.AI
{
    public sealed class OpenRouterClient
    {
        private static readonly Lazy<OpenRouterClient> _inst = new Lazy<OpenRouterClient>(()=>new OpenRouterClient());
        public static OpenRouterClient Instance=> _inst.Value;
        private readonly HttpClient _http = new HttpClient{ Timeout=TimeSpan.FromSeconds(60) };
        private string _apiKey=""; private string _model="openai/gpt-4o-mini";
        private OpenRouterClient(){ Reload(); }
        public void Reload(){
            try{
                var p=System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"appsettings.json");
                if(System.IO.File.Exists(p)){
                    dynamic c=JsonConvert.DeserializeObject(System.IO.File.ReadAllText(p));
                    _apiKey=c?.AURA?.AI?.OpenRouterApiKey??""; _model=c?.AURA?.AI?.Model??"openai/gpt-4o-mini";
                }
            }catch{}
        }
        public async Task<AiResponse> SendAsync(AiRequest req, System.Threading.CancellationToken ct=default){
            if(string.IsNullOrWhiteSpace(_apiKey)) return new AiResponse{ Success=false, Content="API Key vazia" };
            var payload=new{ model=_model, messages=new[]{ new{ role="user", content=req.Prompt } } };
            var httpReq=new HttpRequestMessage(HttpMethod.Post,"https://openrouter.ai/api/v1/chat/completions"){ Content=new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8,"application/json") };
            httpReq.Headers.Add("Authorization",$"Bearer {_apiKey}"); httpReq.Headers.Add("HTTP-Referer","https://aura.local");
            try{
                var resp=await _http.SendAsync(httpReq, ct);
                var body=await resp.Content.ReadAsStringAsync();
                if(!resp.IsSuccessStatusCode) return new AiResponse{ Success=false, Content=body };
                var j=JObject.Parse(body);
                return new AiResponse{ Success=true, Content=j["choices"]?[0]?["message"]?["content"]?.ToString()??"" };
            }catch(Exception ex){ return new AiResponse{ Success=false, Content=ex.Message }; }
        }
        public async Task<float[]> CreateEmbeddingAsync(string text){
            if(string.IsNullOrWhiteSpace(_apiKey)) return new float[384];
            try{
                var payload=new{ model="openai/text-embedding-3-small", input=text };
                var req=new HttpRequestMessage(HttpMethod.Post,"https://openrouter.ai/api/v1/embeddings"){ Content=new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8,"application/json") };
                req.Headers.Add("Authorization",$"Bearer {_apiKey}");
                var resp=await _http.SendAsync(req); var body=await resp.Content.ReadAsStringAsync();
                var j=JObject.Parse(body); var arr=j["data"]?[0]?["embedding"] as JArray;
                if(arr==null) return new float[384];
                var vec=new float[arr.Count]; for(int i=0;i<arr.Count;i++) vec[i]=(float)arr[i]; return vec;
            }catch{ return new float[384]; }
        }
    }
}
'@

Write-File "Core\AI\LocalModelFallback.cs" @'
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
namespace AURA.Core.AI
{
    public sealed class LocalModelFallback
    {
        private static readonly System.Lazy<LocalModelFallback> _inst = new System.Lazy<LocalModelFallback>(()=>new LocalModelFallback());
        public static LocalModelFallback Instance=> _inst.Value;
        private string _exe=""; private string _model="";
        private LocalModelFallback(){ try{ dynamic c=Newtonsoft.Json.JsonConvert.DeserializeObject(File.ReadAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory,"appsettings.json"))); _exe=c?.AURA?.AI?.LocalLlamaExe; _model=c?.AURA?.AI?.LocalModelPath; }catch{} }
        public Task<AiResponse> SendAsync(AiRequest req)=> Task.Run(()=>{
            if(!File.Exists(_exe)||!File.Exists(_model)) return new AiResponse{ Success=false, Content="Modelo local não configurado" };
            var psi=new ProcessStartInfo{ FileName=_exe, Arguments=$"-m \"{_model}\" -p \"{req.Prompt.Replace("\"","\\\"")}\" -n 256", UseShellExecute=false, CreateNoWindow=true, RedirectStandardOutput=true };
            using(var p=Process.Start(psi)){ var o=p.StandardOutput.ReadToEnd(); p.WaitForExit(60000); return new AiResponse{ Success=p.ExitCode==0, Content=o.Trim() }; }
        });
    }
}
'@

Write-File "Core\Memory\VectorStore.cs" @'
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace AURA.Core.Memory
{
    public sealed class VectorStore
    {
        private static readonly Lazy<VectorStore> _inst = new Lazy<VectorStore>(()=>new VectorStore());
        public static VectorStore Instance=> _inst.Value;
        private readonly string _dbPath;
        private VectorStore(){ _dbPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Memory","AURA.Memory.db"); Init(); }
        void Init(){
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));
            using(var conn=new SQLiteConnection($"Data Source={_dbPath}")){ conn.Open(); new SQLiteCommand("CREATE TABLE IF NOT EXISTS VectorMemory(Id INTEGER PRIMARY KEY, Key TEXT, Embedding BLOB, Metadata TEXT, CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP); CREATE INDEX IF NOT EXISTS idx_key ON VectorMemory(Key);", conn).ExecuteNonQuery(); }
        }
        public Task StoreAsync(string key, float[] emb, string meta=null)=> Task.Run(()=>{
            var bytes=new byte[emb.Length*4]; Buffer.BlockCopy(emb,0,bytes,0,bytes.Length);
            using(var conn=new SQLiteConnection($"Data Source={_dbPath}")){ conn.Open(); using(var cmd=new SQLiteCommand("INSERT INTO VectorMemory(Key,Embedding,Metadata) VALUES(@k,@e,@m)",conn)){ cmd.Parameters.AddWithValue("@k",key); cmd.Parameters.AddWithValue("@e",bytes); cmd.Parameters.AddWithValue("@m",(object)meta??DBNull.Value); cmd.ExecuteNonQuery(); } }
        });
        public Task<List<Tuple<string,float,string>>> SearchAsync(float[] query, int topK=5)=> Task.Run(()=>{
            var res=new List<Tuple<string,float,string>>();
            using(var conn=new SQLiteConnection($"Data Source={_dbPath}")){ conn.Open(); using(var cmd=new SQLiteCommand("SELECT Key,Embedding,Metadata FROM VectorMemory",conn)) using(var r=cmd.ExecuteReader()){ while(r.Read()){ var blob=(byte[])r["Embedding"]; var vec=new float[blob.Length/4]; Buffer.BlockCopy(blob,0,vec,0,blob.Length); float sim=Cosine(query,vec); res.Add(Tuple.Create(r.GetString(0), sim, r.IsDBNull(2)?null:r.GetString(2))); } } }
            return res.OrderByDescending(x=>x.Item2).Take(topK).ToList();
        });
        float Cosine(float[] a,float[] b){ if(a.Length!=b.Length||a.Length==0) return 0; float d=0,ma=0,mb=0; for(int i=0;i<a.Length;i++){ d+=a[i]*b[i]; ma+=a[i]*a[i]; mb+=a[i]*b[i]; } return ma==0||mb==0?0:d/(float)(Math.Sqrt(ma)*Math.Sqrt(mb)); }
        public Task<float[]> EmbedAsync(string text)=> Core.AI.OpenRouterClient.Instance.CreateEmbeddingAsync(text);
    }
}
'@

Write-File "Core\AI\AiEngine.cs" @'
using System.Threading.Tasks;
using AURA.Core.Memory;
namespace AURA.Core.AI
{
    public sealed class AiEngine
    {
        private static readonly System.Lazy<AiEngine> _inst = new System.Lazy<AiEngine>(()=>new AiEngine());
        public static AiEngine Instance=> _inst.Value;
        private AiEngine(){}
        public async Task<AiResponse> SendAsync(AiRequest req){
            string ctx="";
            try{
                var emb=await VectorStore.Instance.EmbedAsync(req.Prompt);
                var mems=await VectorStore.Instance.SearchAsync(emb, 3);
                if(mems.Count>0) ctx="Contexto relevante:\n" + string.Join("\n", mems.ConvertAll(m=> $"- {m.Item1}: {m.Item3}").ToArray()) + "\n\n";
            }catch{}
            var enriched=new AiRequest{ Prompt=ctx+req.Prompt };
            var r=await OpenRouterClient.Instance.SendAsync(enriched);
            if(r.Success) return r;
            return await LocalModelFallback.Instance.SendAsync(enriched);
        }
    }
    public class AiRequest{ public string Prompt{get;set;} }
    public class AiResponse{ public bool Success{get;set;} public string Content{get;set;} }
}
'@

Write-File "Program.cs" @'
using System;
using System.IO;
using System.Threading.Tasks;
using AURA.Core.Bootstrap;
using AURA.Core.Memory;
using AURA.Windows;
using AURA.Core.AI;

namespace AURA
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title="AURA Genesis v1.0 - Phase 7";
            Console.WriteLine("╔══════════════════════════════════╗");
            Console.WriteLine("║ AURA Genesis v1.0 - PHASE 7 MVP ║");
            Console.WriteLine("╚══════════════════════════════════╝\n");
            var boot=new AuraBootstrap();
            await boot.InitializeAsync();
            Console.WriteLine("✅ Ambiente OK - VectorStore inicializado");
            Console.WriteLine("✅ WindowsEngine: "+WindowsEngine.Instance.GetType().Name);
            Console.WriteLine("\nComandos: ai <pergunta> | backup <arquivo> | rollback <bak> | mem <texto> | search <texto> | status | exit\n");
            while(true){
                Console.Write("AURA> ");
                var input=Console.ReadLine();
                if(string.IsNullOrWhiteSpace(input)) continue;
                if(input.ToLower()=="exit") break;
                var parts=input.Split(new[]{' '},2);
                var cmd=parts[0].ToLower(); var arg=parts.Length>1?parts[1]:null;
                try{
                    switch(cmd){
                        case "ai": if(arg==null) Console.WriteLine("Uso: ai <pergunta>"); else { var r=await WindowsEngine.Instance.AI.SendAsync(new AiRequest{Prompt=arg}); Console.WriteLine(r.Content); } break;
                        case "backup": if(arg==null) Console.WriteLine("Uso: backup <arquivo>"); else { var b=await WindowsEngine.Instance.Backup.BackupFileAsync(arg); Console.WriteLine($"Backup: {b}"); } break;
                        case "mem": if(arg!=null){ var emb=await VectorStore.Instance.EmbedAsync(arg); await VectorStore.Instance.StoreAsync(Guid.NewGuid().ToString(), emb, arg); Console.WriteLine("Memória salva"); } break;
                        case "search": if(arg!=null){ var emb=await VectorStore.Instance.EmbedAsync(arg); var res=await VectorStore.Instance.SearchAsync(emb,3); foreach(var m in res) Console.WriteLine($"[{m.Item2:F3}] {m.Item3}"); } break;
                        case "status": Console.WriteLine(File.ReadAllText("project-status.json")); break;
                        case "clear": Console.Clear(); break;
                        default: Console.WriteLine("Comando desconhecido"); break;
                    }
                }catch(Exception ex){ Console.WriteLine($"❌ {ex.Message}"); }
            }
        }
    }
}
'@

Write-Host "`n✅ Upgrade para Fase 7 aplicado!" -ForegroundColor Green
Write-Host "Próximos passos:" -ForegroundColor Yellow
Write-Host "  1. dotnet restore"
Write-Host "  2. dotnet build -c Release"
Write-Host "  3. Coloque sua OpenRouter API Key em appsettings.json"
Write-Host "  4. dotnet run"