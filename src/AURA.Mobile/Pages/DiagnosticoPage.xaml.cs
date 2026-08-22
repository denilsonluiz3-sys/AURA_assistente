using AURA.Agents;
using AURA.Agents.Programs;
using AURA.Abstractions;
using AURA.Network;
using AURA.SystemInfo;
using AURA.Core.Logging;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Pages;

public partial class DiagnosticoPage : ContentPage
{
    private readonly SystemAnalyzer _systemAnalyzer;
    private readonly NetworkManager _networkManager;
    private readonly AgentManager _agentManager;
    private readonly AiDiagnosticsService _diagnostics;
    private readonly CellProgramRegistry? _registry;
    private readonly CellProgramRunner? _runner;
    private readonly IAuraCellContextFactory? _contextFactory;
    private readonly ILogger? _logger;
    private string? _lastCellJson;
    private bool _cellRunning;
    private bool _aiRunning;

    public DiagnosticoPage(
        SystemAnalyzer systemAnalyzer,
        NetworkManager networkManager,
        AgentManager agentManager,
        AiDiagnosticsService diagnostics,
        CellProgramRegistry? registry = null,
        CellProgramRunner? runner = null,
        IAuraCellContextFactory? contextFactory = null,
        ILogger? logger = null)
    {
        InitializeComponent();
        _systemAnalyzer = systemAnalyzer;
        _networkManager = networkManager;
        _agentManager = agentManager;
        _diagnostics = diagnostics;
        _registry = registry;
        _runner = runner;
        _contextFactory = contextFactory;
        _logger = logger;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
        UpdateCellButtonState();
    }

    private void UpdateCellButtonState()
    {
        bool available = _registry != null && _runner != null && _contextFactory != null;
        BtnRunCellDiag.IsEnabled = available && !_cellRunning;
        if (!available)
        {
            CellDiagStatus.IsVisible = true;
            CellDiagStatus.Text = "Cell Program disponível apenas em Android.";
            CellDiagStatus.TextColor = Color.FromArgb("#7a7a90");
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private async void OnRunCellDiagnosticClicked(object? sender, EventArgs e)
    {
        if (_registry == null || _runner == null || _contextFactory == null || _cellRunning)
            return;

        _cellRunning = true;
        BtnRunCellDiag.IsEnabled = false;
        BtnRunCellDiag.Text = "Executando…";
        CellDiagStatus.IsVisible = true;
        CellDiagStatus.Text = "PolicyGuard → CellProgramRunner…";
        CellDiagStatus.TextColor = Color.FromArgb("#f0a050");
        CellDiagSummary.IsVisible = false;
        BtnCellDetails.IsVisible = false;
        _lastCellJson = null;

        try
        {
            var program = _registry.Resolve("device-diagnostic");
            if (program == null)
            {
                CellDiagStatus.Text = "Programa device-diagnostic não registrado.";
                CellDiagStatus.TextColor = Color.FromArgb("#e05560");
                return;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var context = _contextFactory.Create($"sys-hub-{Guid.NewGuid():N}", cts.Token);
            var result = await _runner.RunAsync(program, context, cts.Token);

            if (result.IsSuccess)
            {
                _lastCellJson = System.Text.Json.JsonSerializer.Serialize(
                    result.Data,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                _diagnostics.CaptureDiagnosticContext(_lastCellJson);

                CellDiagStatus.Text = "Concluído";
                CellDiagStatus.TextColor = Color.FromArgb("#3ec97a");
                CellDiagSummary.Text = BuildSummary(result.Data);
                CellDiagSummary.IsVisible = true;
                BtnCellDetails.IsVisible = true;
                BtnAnalyzeWithAi.IsEnabled = true;
            }
            else
            {
                CellDiagStatus.Text = $"Erro: {result.Error}";
                CellDiagStatus.TextColor = Color.FromArgb("#e05560");
            }
        }
        catch (OperationCanceledException)
        {
            CellDiagStatus.Text = "Cancelado / timeout";
            CellDiagStatus.TextColor = Color.FromArgb("#f0a050");
        }
        catch (Exception ex)
        {
            CellDiagStatus.Text = $"Falha: {ex.Message}";
            CellDiagStatus.TextColor = Color.FromArgb("#e05560");
            _logger?.Error($"DiagnosticoPage CellDiag: {ex.Message}");
        }
        finally
        {
            _cellRunning = false;
            BtnRunCellDiag.Text = "Executar diagnóstico";
            BtnRunCellDiag.IsEnabled = _registry != null && _runner != null && _contextFactory != null;
        }
    }

    private async void OnAnalyzeWithAiClicked(object? sender, EventArgs e)
    {
        if (_aiRunning)
            return;

        _aiRunning = true;
        BtnAnalyzeWithAi.IsEnabled = false;
        AiDiagnosticStatus.IsVisible = true;
        AiDiagnosticStatus.Text = "Analisando log + diagnóstico do dispositivo com IA…";

        try
        {
            string analysis = await _diagnostics.AnalyzeAsync();
            AiDiagnosticStatus.Text = analysis;
        }
        catch (Exception ex)
        {
            AiDiagnosticStatus.Text = "Falha na análise IA: " + ex.Message;
            AuraLog.Exception("DiagnosticoPage.OnAnalyzeWithAiClicked", ex);
        }
        finally
        {
            _aiRunning = false;
            BtnAnalyzeWithAi.IsEnabled = true;
        }
    }

    private async void OnCellDetailsClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_lastCellJson))
            return;

        await DisplayAlert("Detalhes técnicos", _lastCellJson.Length > 3500
            ? _lastCellJson.Substring(0, 3500) + "\n…"
            : _lastCellJson, "OK");
    }

    private static string BuildSummary(object? data)
    {
        if (data == null) return "Sem dados.";
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var parts = new List<string>();

            if (root.TryGetProperty("Device", out _))
                parts.Add("Dispositivo OK");
            if (root.TryGetProperty("Battery", out _))
                parts.Add("Bateria OK");
            if (root.TryGetProperty("Network", out _))
                parts.Add("Rede OK");
            if (root.TryGetProperty("DeviceProperties", out _))
                parts.Add("Propriedades OK");

            return parts.Count > 0
                ? string.Join(" · ", parts)
                : "Resultado recebido (ver detalhes).";
        }
        catch
        {
            return "Resultado recebido (ver detalhes).";
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            var sys = await Task.Run(() => _systemAnalyzer.Analyze());
            var net = await Task.Run(() => _networkManager.CheckConnection());

            CpuValue.Text = sys.ProcessorCount > 0 ? "Ativo" : "—";
            CpuDetail.Text = sys.Architecture ?? "";

            RamValue.Text = sys.TotalMemoryGb > 0 ? $"{sys.AvailableMemoryGb:0.0} GB" : "—";
            RamDetail.Text = sys.TotalMemoryGb > 0
                ? $"{sys.TotalMemoryGb:0.0} GB total"
                : "";

            DiskValue.Text = sys.FreeDiskSpaceGb > 0 ? $"{sys.FreeDiskSpaceGb:0.0} GB" : "—";
            DiskDetail.Text = sys.TotalDiskSpaceGb > 0
                ? $"{sys.TotalDiskSpaceGb:0.0} GB total"
                : "";

            OsValue.Text = sys.OperatingSystem ?? "—";
            OsDetail.Text = sys.SystemDrive is not null ? $"Disco {sys.SystemDrive}" : "";

            CoresValue.Text = sys.ProcessorCount > 0 ? sys.ProcessorCount.ToString() : "—";
            CoresDetail.Text = sys.ProcessorCount == 1 ? "núcleo" : "núcleos";

            LatencyValue.Text = net.LatencyMilliseconds > 0
                ? $"{net.LatencyMilliseconds} ms"
                : net.HasInternetAccess ? "✓" : "—";
            LatencyDetail.Text = net.HasInternetAccess ? "conectado" : "offline";

            IpValue.Text = !string.IsNullOrWhiteSpace(net.LocalIpAddress)
                ? net.LocalIpAddress
                : "—";
            IpDetail.Text = net.HasInternetAccess ? "roteável" : "local apenas";

            VersionValue.Text = AURA.Core.VersionInfo.FullName ?? "—";
            VersionDetail.Text = "AURA Mobile";

            var available = _agentManager.AvailableAssistants();
            AgentsLabel.Text = available.Count == 0
                ? "Nenhum agente CLI instalado no dispositivo. Use a aba Assistente."
                : string.Join("  •  ", available.Select(a => a.Name));

            var online = net.HasInternetAccess;
            ConnectionIcon.Text = online ? "🌐" : "⚠️";
            ConnectionLabel.Text = online
                ? "Dispositivo conectado"
                : "Sem conexão com a internet";
            ConnectionCard.Stroke = online
                ? (Color)Application.Current!.Resources["AuraBorderAccent"]
                : (Color)Application.Current!.Resources["AuraBorder"];
        }
        catch (Exception ex)
        {
            ConnectionLabel.Text = "Erro ao coletar diagnóstico: " + ex.Message;
        }
    }
}
