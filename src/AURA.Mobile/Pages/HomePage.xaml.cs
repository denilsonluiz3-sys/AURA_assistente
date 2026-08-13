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
        await RefreshAsync();
        StartOrbPulse();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopOrbPulse();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    // ── Pulse holográfico (F2 — MAUI nativo, zero NuGet) ───────────

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
        // Loop suave: escala 1.0 ↔ 1.08 + leve variação de opacidade no anel médio.
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
                // Página pode ter sido descarregada; encerra o loop.
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
        catch
        {
            // ignore
        }
    }

    // ── Bottom bar alinhada à referência (Início | Diagnóstico | Módulos | Agentes | Config) ──

    private async void OnInicioClicked(object? sender, EventArgs e)
    {
        await PlayButtonFeedbackAsync(BtnInicio);
        await RefreshAsync();
    }

    private async void OnDiagnosticoClicked(object? sender, EventArgs e)
    {
        await PlayButtonFeedbackAsync(BtnDiagnostico);
        await NavigateToSectionAndPageAsync("Sistema", "Diagnóstico");
    }

    private async void OnModulosClicked(object? sender, EventArgs e)
    {
        await PlayButtonFeedbackAsync(BtnModulos);
        await NavigateToSectionAndPageAsync("Ferramentas", "Módulos");
    }

    private async void OnAgentesClicked(object? sender, EventArgs e)
    {
        await PlayButtonFeedbackAsync(BtnAgentes);
        await NavigateToSectionAndPageAsync("Assistente", "Agente");
    }

    private async void OnConfigClicked(object? sender, EventArgs e)
    {
        await PlayButtonFeedbackAsync(BtnConfig);
        // Ainda não existe página Config dedicada. Leva à seção Sistema (Início/Diagnóstico/Logs).
        if (!TrySwitchToSection("Sistema"))
            await DisplayAlert("Config", "Seção Sistema não disponível no momento.", "OK");
    }

    /// <summary>
    /// Troca a aba do TabbedPage para a seção e, se possível, faz PushAsync da página com o label dado.
    /// </summary>
    private async Task NavigateToSectionAndPageAsync(string sectionTitle, string pageLabel)
    {
        if (!TrySwitchToSection(sectionTitle))
        {
            await DisplayAlert(pageLabel, $"Seção \"{sectionTitle}\" ainda não está ativa (módulo não aplicado).", "OK");
            return;
        }

        // Após trocar a aba, tenta empurrar a página alvo a partir da SectionPage.
        try
        {
            if (Parent is NavigationPage nav && nav.Parent is TabbedPage tabs
                && tabs.CurrentPage is NavigationPage sectionNav
                && sectionNav.CurrentPage is SectionPage)
            {
                // SectionPage já está no topo da NavigationPage da seção.
                // Percorre os itens conhecidos via Reflection não é ideal;
                // em vez disso, o usuário vê a grade da seção e toca no card.
                // Para UX imediata: se a página alvo estiver registrada no MainPage,
                // tentamos localizar via Children do SectionPage (não exposto).
                // Fallback seguro: apenas muda a aba — a SectionPage mostra os cards.
            }
        }
        catch (Exception ex)
        {
            AuraLog.Info("NavigateToSectionAndPage: " + ex.Message);
        }
    }

    private bool TrySwitchToSection(string sectionTitle)
    {
        if (Parent is not NavigationPage nav || nav.Parent is not TabbedPage tabs)
            return false;

        foreach (var child in tabs.Children)
        {
            if (child is NavigationPage np && string.Equals(np.Title, sectionTitle, StringComparison.OrdinalIgnoreCase))
            {
                tabs.CurrentPage = child;
                return true;
            }
        }
        return false;
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
