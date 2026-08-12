using AURA.Agents;
using AURA.Network;
using AURA.SystemInfo;

namespace AURA.Mobile.Pages;

public partial class HomePage : ContentPage
{
    private readonly SystemAnalyzer _systemAnalyzer;
    private readonly NetworkManager _networkManager;
    private readonly AgentManager _agentManager;

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
        await RefreshAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    // ── Bottom bar (conceito holográfico) ──────────────────────────

    private async void OnNetworkClicked(object? sender, EventArgs e)
    {
        await RefreshNetworkOnlyAsync();
        await DisplayAlert("Network", "Status de rede atualizado.", "OK");
    }

    private async void OnSensorClicked(object? sender, EventArgs e)
    {
        await RefreshSystemOnlyAsync();
        await DisplayAlert("Sensor", "Diagnóstico de sistema atualizado.", "OK");
    }

    private async void OnEthereumClicked(object? sender, EventArgs e)
    {
        await DisplayAlert("Ethereum", "Módulo reservado para integração futura.", "OK");
    }

    private async void OnSystemClicked(object? sender, EventArgs e)
    {
        await RefreshAsync();
        await DisplayAlert("System", "Painel de sistema atualizado.", "OK");
    }

    private async void OnDeviceClicked(object? sender, EventArgs e)
    {
        // Navega para a seção Apps (Células) se existir no TabbedPage pai.
        if (Parent is NavigationPage nav && nav.Parent is TabbedPage tabs)
        {
            foreach (var child in tabs.Children)
            {
                if (child is NavigationPage np && np.Title == "Apps")
                {
                    tabs.CurrentPage = child;
                    return;
                }
            }
        }

        await DisplayAlert("Device", "Abra a seção Apps → Células para gerenciar o dispositivo.", "OK");
    }

    // ── Refresh ────────────────────────────────────────────────────

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
