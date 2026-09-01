using AURA.AI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Pages;

/// <summary>
/// UX extra do Agente: stop da execução, TTS por bolha, status curto.
/// Partial — não reescreve o AgentPage.xaml.cs inteiro.
/// </summary>
public partial class AgentPage
{
    private CancellationTokenSource? _runCts;

    /// <summary>Se já estiver rodando, cancela; senão delega ao fluxo normal.</summary>
    private void OnRunOrStopClicked(object? sender, EventArgs e)
    {
        if (_runInFlight)
        {
            RequestStopRun();
            return;
        }

        OnRunClicked(sender, e);
    }

    private void RequestStopRun()
    {
        try
        {
            _runCts?.Cancel();
        }
        catch { /* ignore */ }

        try { _ = _speech.StopAsync(); } catch { /* ignore */ }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                RunButton.Text = "▶";
                RunButton.BackgroundColor = (Color)Application.Current!.Resources["AuraAccent"];
            }
            catch { /* ignore */ }
        });

        _ = AppendBubbleAsync("⏹ Execução interrompida.", user: false, isTool: true);
    }

    private CancellationToken BeginRunToken()
    {
        try { _runCts?.Cancel(); } catch { /* ignore */ }
        try { _runCts?.Dispose(); } catch { /* ignore */ }
        _runCts = new CancellationTokenSource();
        return _runCts.Token;
    }

    private void SetRunButtonBusy(bool busy)
    {
        try
        {
            if (busy)
            {
                RunButton.Text = "■";
                RunButton.IsEnabled = true; // precisa permanecer clicável para stop
                BusyIndicator.IsRunning = true;
                BusyIndicator.IsVisible = true;
            }
            else
            {
                RunButton.Text = "▶";
                RunButton.IsEnabled = true;
                BusyIndicator.IsRunning = false;
                BusyIndicator.IsVisible = false;
            }
        }
        catch { /* ignore */ }
    }

    private void RefreshModelStatusLabel()
    {
        try { ModelLabel.Text = AiStatusText.ForClient(_client); }
        catch { /* ignore */ }
    }

    private HorizontalStackLayout BuildBubbleActions(string payload, bool showSpeak)
    {
        var row = new HorizontalStackLayout
        {
            Spacing = 4,
            HorizontalOptions = LayoutOptions.End
        };

        var copyBtn = new Button
        {
            Text = "📋",
            FontSize = 11,
            Padding = new Thickness(6, 2),
            HeightRequest = 28,
            WidthRequest = 36,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#8a9bb8")
        };
        copyBtn.Clicked += async (_, _) =>
        {
            try
            {
                await Clipboard.Default.SetTextAsync(payload);
                AuraLog.Info("AgentPage: texto copiado (botão)");
            }
            catch (Exception ex) { AuraLog.Exception("CopyBtn", ex); }
        };
        row.Children.Add(copyBtn);

        if (showSpeak)
        {
            var speakBtn = new Button
            {
                Text = "🔊",
                FontSize = 11,
                Padding = new Thickness(6, 2),
                HeightRequest = 28,
                WidthRequest = 36,
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#8a9bb8")
            };
            speakBtn.Clicked += async (_, _) =>
            {
                try
                {
                    await SpeakAsync(payload);
                }
                catch (Exception ex) { AuraLog.Exception("SpeakBtn", ex); }
            };
            row.Children.Add(speakBtn);
        }

        return row;
    }
}
