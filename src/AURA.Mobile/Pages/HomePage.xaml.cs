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

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        try
        {
            VersionLabel.Text = AURA.Core.VersionInfo.FullName;

            var diagnostics = await Task.Run(() => _systemAnalyzer.Analyze());
            OsLabel.Text = "SO: " + diagnostics.OperatingSystem;
            CpuLabel.Text = "Arquitetura: " + diagnostics.Architecture + "  |  Núcleos: " + diagnostics.ProcessorCount;
            RamLabel.Text = $"RAM: {diagnostics.TotalMemoryGb:0.0} GB total / {diagnostics.AvailableMemoryGb:0.0} GB livre";
            DiskLabel.Text = $"Disco {diagnostics.SystemDrive}: {diagnostics.FreeDiskSpaceGb:0.0}/{diagnostics.TotalDiskSpaceGb:0.0} GB";

            var network = await Task.Run(() => _networkManager.CheckConnection());
            NetLabel.Text = network.Message
                + (network.HasInternetAccess ? $"  (latência {network.LatencyMilliseconds} ms)" : "");
            IpLabel.Text = "IP local: " + network.LocalIpAddress;

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
}
