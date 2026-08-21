using CommunityToolkit.Maui.Views;
#if ANDROID
using AURA.Mobile.Platforms.Android;
#endif

namespace AURA.Mobile.Pages;

public partial class HomePage : ContentPage
{
    private const string VideoBgPrefKey = "aura_video_bg";

    public HomePage()
    {
        InitializeComponent();
        App.ThemeChanged += OnThemeChanged;
        UpdateThemeIcon();

        var doubleTap = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
        doubleTap.Tapped += OnThemeDoubleTapped;
        BtnTheme.GestureRecognizers.Add(doubleTap);

        // V16 - Teste nativo Android
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
        ApplyVideoBackground();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        PauseVideoBackground();
    }

    // ── Tema Solar / Lunar ─────────────────────────────────────────

    private void OnThemeToggleClicked(object? sender, EventArgs e)
    {
        App.ToggleTheme();
    }

    private void OnThemeChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateThemeIcon();
            ApplyVideoBackground();
        });
    }

    private void UpdateThemeIcon()
    {
        if (BtnTheme is null) return;
        BtnTheme.Text = App.IsSolar ? "☾" : "☀";
    }

    // ── Vídeo de fundo ────────────────────────────────────────────

    private bool IsVideoBgEnabled => Preferences.Default.Get(VideoBgPrefKey, true);

    private void OnThemeDoubleTapped(object? sender, TappedEventArgs e)
    {
        bool next = !IsVideoBgEnabled;
        Preferences.Default.Set(VideoBgPrefKey, next);
        AuraLog.Info($"Vídeo de fundo {(next ? "ativado" : "desativado")}");
        ApplyVideoBackground();
        _ = PlayButtonFeedbackAsync(BtnTheme);
    }

    private async void ApplyVideoBackground()
    {
        if (BgVideo is null) return;

        if (!IsVideoBgEnabled)
        {
            PauseVideoBackground();
            BgVideo.IsVisible = false;
            return;
        }

        try
        {
            string resource = App.IsSolar ? "solar_bg.mp4" : "lunar_bg.mp4";
            await BgVideo.FadeTo(0, 150, Easing.Linear);
            BgVideo.Stop();
            BgVideo.Source = MediaSource.FromResource(resource);
            BgVideo.IsVisible = true;
            await BgVideo.FadeTo(1, 300, Easing.Linear);
            BgVideo.Play();
        }
        catch (Exception ex)
        {
            AuraLog.Exception("HomePage.ApplyVideoBackground", ex);
            BgVideo.IsVisible = true;
            BgVideo.Play();
        }
    }

    private void PauseVideoBackground()
    {
        try { BgVideo?.Pause(); } catch { }
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

    // ── Bottom bar ──────────────────────────────────────────────────

    private async void OnInicioClicked(object? sender, EventArgs e)
    {
        await PlayButtonFeedbackAsync(BtnInicio);
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
