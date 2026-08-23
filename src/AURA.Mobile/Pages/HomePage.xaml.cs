#if ANDROID
using AURA.Mobile.Platforms.Android;
#endif

namespace AURA.Mobile.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
        App.ThemeChanged += OnThemeChanged;
        UpdateThemeIcon();

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += OnThemeDoubleTapped;
        BtnTheme.GestureRecognizers.Add(doubleTap);

        RunAndroidBridgeTest();
    }

    private void RunAndroidBridgeTest()
    {
#if ANDROID
        try
        {
            var resultado = AuraAndroidBridgeTest.Run();
            System.Diagnostics.Debug.WriteLine(resultado);
            AuraLog.Info("V16 TESTE EXECUTADO");
            AuraLog.Info(resultado);
        }
        catch (Exception ex)
        {
            AuraLog.Exception("V16 Teste", ex);
        }
#endif
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateThemeIcon();
        VersionLabel.Text = AURA.Core.VersionInfo.FullName;
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        try
        {
            StatusSistema.Text = "Sistema ✓";
            StatusSistema.TextColor = Color.FromArgb("#3ec97a");

            var access = Connectivity.Current.NetworkAccess;
            bool online = access == NetworkAccess.Internet || access == NetworkAccess.ConstrainedInternet;
            StatusRede.Text = online ? "Rede ✓" : "Rede ✗";
            StatusRede.TextColor = online ? Color.FromArgb("#3ec97a") : Color.FromArgb("#e05560");

#if ANDROID
            try
            {
                var bm = (Android.OS.BatteryManager?)Android.App.Application.Context.GetSystemService(Android.Content.Context.BatteryService);
                int level = bm?.GetIntProperty((int)Android.OS.BatteryProperty.Capacity) ?? -1;
                if (level >= 0)
                {
                    StatusBateria.Text = $"Bateria {level}%";
                    StatusBateria.TextColor = level > 20 ? Color.FromArgb("#3ec97a") : Color.FromArgb("#f0a050");
                }
                else
                {
                    StatusBateria.Text = "Bateria —";
                    StatusBateria.TextColor = Color.FromArgb("#7a7a90");
                }
            }
            catch
            {
                StatusBateria.Text = "Bateria —";
                StatusBateria.TextColor = Color.FromArgb("#7a7a90");
            }
#else
            StatusBateria.Text = "Bateria —";
            StatusBateria.TextColor = Color.FromArgb("#7a7a90");
#endif
        }
        catch (Exception ex)
        {
            AuraLog.Exception("HomePage.RefreshStatus", ex);
        }
    }

    private void OnThemeToggleClicked(object? sender, EventArgs e)
    {
        App.ToggleTheme();
    }

    private void OnThemeChanged()
    {
        MainThread.BeginInvokeOnMainThread(UpdateThemeIcon);
    }

    private void UpdateThemeIcon()
    {
        if (BtnTheme is null) return;
        BtnTheme.Text = App.IsSolar ? "☾" : "☀";
    }

    private void OnThemeDoubleTapped(object? sender, TappedEventArgs e)
    {
        _ = PlayButtonFeedbackAsync(BtnTheme);
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

    private async void OnCommandCompleted(object? sender, EventArgs e)
    {
        await SubmitCommandAsync();
    }

    private async void OnSendCommandClicked(object? sender, EventArgs e)
    {
        await SubmitCommandAsync();
    }

    private async Task SubmitCommandAsync()
    {
        string text = (CommandEntry?.Text ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(text)) return;

        RecentActivityLabel.Text = $"Comando: {text}";
        CommandEntry.Text = string.Empty;
        await NavigateToLabelAsync("Chat");
    }

    private async void OnQuickDiagnostico(object? sender, TappedEventArgs e)
    {
        RecentActivityLabel.Text = "Ação: Diagnosticar";
        await NavigateToLabelAsync("Diagnóstico");
    }

    private async void OnQuickProgramas(object? sender, TappedEventArgs e)
    {
        RecentActivityLabel.Text = "Ação: Programas";
        await NavigateToLabelAsync("Programas");
    }

    private async void OnQuickTerminal(object? sender, TappedEventArgs e)
    {
        RecentActivityLabel.Text = "Ação: Terminal";
        await NavigateToLabelAsync("Terminal");
    }

    private async void OnQuickChat(object? sender, TappedEventArgs e)
    {
        RecentActivityLabel.Text = "Ação: Perguntar à AURA";
        await NavigateToLabelAsync("Chat");
    }

    private async Task NavigateToLabelAsync(string label)
    {
        if (Application.Current?.Windows?.FirstOrDefault()?.Page is MainPage main)
        {
            await main.NavigateToProcessAsync(label);
            return;
        }

        string section = label switch
        {
            "Diagnóstico" or "Logs" or "Correções" or "Ecossistema" => "Sistema",
            "Chat" or "Agente" or "Memória" or "Navegador" => "Assistente",
            "Terminal" or "Executores" or "Módulos" => "Ferramentas",
            "Programas" or "Células" or "Rodar programa" => "Apps",
            _ => "Sistema"
        };
        await NavigateToSectionAndPageAsync(section, label);
    }

    private async void OnInicioClicked(object? sender, EventArgs e)
    {
        await PlayButtonFeedbackAsync(BtnInicio);
    }

    private async void OnDiagnosticoClicked(object? sender, EventArgs e)
    {
        await PlayButtonFeedbackAsync(BtnDiagnostico);
        await NavigateToLabelAsync("Diagnóstico");
    }

    private async void OnModulosClicked(object? sender, EventArgs e)
    {
        await PlayButtonFeedbackAsync(BtnModulos);
        await NavigateToLabelAsync("Módulos");
    }

    private async void OnAgentesClicked(object? sender, EventArgs e)
    {
        await PlayButtonFeedbackAsync(BtnAgentes);
        await NavigateToLabelAsync("Agente");
    }

    private async void OnConfigClicked(object? sender, EventArgs e)
    {
        await PlayButtonFeedbackAsync(BtnConfig);
        if (!TrySwitchToSection("Sistema"))
            await DisplayAlert("Config", "Seção Sistema não disponível no momento.", "OK");
    }

    private async Task NavigateToSectionAndPageAsync(string sectionTitle, string pageLabel)
    {
        if (!TrySwitchToSection(sectionTitle))
        {
            await DisplayAlert(pageLabel, $"Seção \"{sectionTitle}\" ainda não está ativa.", "OK");
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
}
