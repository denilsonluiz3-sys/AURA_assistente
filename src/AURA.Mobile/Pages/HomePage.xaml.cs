using AURA.Agents;
using AURA.Network;
using AURA.SystemInfo;

namespace AURA.Mobile.Pages;

public partial class HomePage : ContentPage
{
    private readonly SystemAnalyzer _systemAnalyzer;
    private readonly NetworkManager _networkManager;
    private readonly AgentManager _agentManager;
    private bool _pulseRunning;

    public HomePage(SystemAnalyzer systemAnalyzer, NetworkManager networkManager, AgentManager agentManager)
    {
        InitializeComponent();
        _systemAnalyzer = systemAnalyzer;
        _networkManager = networkManager;
        _agentManager = agentManager;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        App.ThemeChanged += OnThemeChanged;
        UpdateThemeIcon();
        await RefreshAsync();
        StartOrbPulse();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        App.ThemeChanged -= OnThemeChanged;
        StopOrbPulse();
    }

    private void OnThemeChanged()
    {
        MainThread.BeginInvokeOnMainThread(UpdateThemeIcon);
    }

    private void UpdateThemeIcon()
    {
        var icon = App.IsSolar ? "☀️" : "🌙";
        ThemeIcon.Text = icon;
        FabIcon.Text = icon;
    }

    private async void OnThemeToggled(object? sender, EventArgs e)
    {
        App.ToggleTheme();
        await PlayButtonFeedbackAsync(FabTheme);
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private void StartOrbPulse()
    {
        if (_pulseRunning || CoreOrb is null)
            return;

        _pulseRunning = true;
        _ = RunPulseLoopAsync();
    }

    private void StopOrbPulse()
    {
        _pulseRunning = false;
        CoreOrb?.AbortAnimation("OrbPulse");
        MiddleRing?.AbortAnimation("RingPulse");
        OuterRing?.AbortAnimation("OuterPulse");
    }

    private async Task RunPulseLoopAsync()
    {
        while (_pulseRunning)
        {
            try
            {
                var scaleUp = CoreOrb.ScaleTo(1.08, 900, Easing.SinInOut);
                var fadeOut = MiddleRing.FadeTo(0.45, 900, Easing.SinInOut);
                await Task.WhenAll(scaleUp, fadeOut);
                if (!_pulseRunning) break;

                var scaleDown = CoreOrb.ScaleTo(1.0, 900, Easing.SinInOut);
                var fadeIn = MiddleRing.FadeTo(0.7, 900, Easing.SinInOut);
                await Task.WhenAll(scaleDown, fadeIn);
            }
            catch
            {
                break;
            }
        }
    }

    private static async Task PlayButtonFeedbackAsync(View? button)
    {
        if (button is null) return;
        try
        {
            await button.ScaleTo(0.85, 80, Easing.CubicOut);
            await button.ScaleTo(1.0, 120, Easing.CubicIn);
        }
        catch { }
    }

    private async Task RefreshAsync()
    {
        try
        {
            VersionLabel.Text = AURA.Core.VersionInfo.FullName;

            await Task.WhenAll(RefreshSystemOnlyAsync(), RefreshNetworkOnlyAsync());

            var available = _agentManager.AvailableAssistants();
            AgentsLabel.Text = available.Count == 0
                ? "Nenhum agente CLI instalado no dispositivo. Use a aba Assistente."
                : string.Join("  •  ", available.Select(a => a.Name));
        }
        catch (Exception ex)
        {
            VersionLabel.Text = "Erro ao coletar diagnóstico: " + ex.Message;
        }
    }

    private async Task RefreshSystemOnlyAsync()
    {
        var diagnostics = await Task.Run(() => _systemAnalyzer.Analyze());
        OsLabel.Text = "SO: " + diagnostics.OperatingSystem;
        CpuLabel.Text = "Arquitetura: " + diagnostics.Architecture + "  |  Núcleos: " + diagnostics.ProcessorCount;
        RamLabel.Text = $"RAM: {diagnostics.TotalMemoryGb:0.0} GB total / {diagnostics.AvailableMemoryGb:0.0} GB livre";
        DiskLabel.Text = $"Disco {diagnostics.SystemDrive}: {diagnostics.FreeDiskSpaceGb:0.0}/{diagnostics.TotalDiskSpaceGb:0.0} GB";
    }

    private async Task RefreshNetworkOnlyAsync()
    {
        var network = await Task.Run(() => _networkManager.CheckConnection());
        NetLabel.Text = network.Message
            + (network.HasInternetAccess ? $"  (latência {network.LatencyMilliseconds} ms)" : "");
        IpLabel.Text = "IP local: " + network.LocalIpAddress;
    }
}