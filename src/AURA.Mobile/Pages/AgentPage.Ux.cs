using AURA.AI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Pages;

/// <summary>
/// UX alinhada à visão: status curto, stop com CT, 🔊, limpar histórico compartilhado.
/// Partial — não reescreve o AgentPage.xaml.cs grande.
/// </summary>
public partial class AgentPage
{
    private CancellationTokenSource? _runCts;
    private bool _uxHooked;

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler == null)
            return;

        HookBubbleSpeakInjector();
        try { RefreshModelStatusLabel(); } catch { /* ignore */ }
    }

    private void HookBubbleSpeakInjector()
    {
        if (_uxHooked)
            return;
        _uxHooked = true;

        try
        {
            ConversationContainer.ChildAdded += (_, args) =>
            {
                if (args.Element is Border border)
                    MainThread.BeginInvokeOnMainThread(() => TryInjectSpeakButton(border));
            };
        }
        catch (Exception ex)
        {
            AuraLog.Exception("HookBubbleSpeakInjector", ex);
        }
    }

    /// <summary>▶ envia · ■ cancela via AgentSession.CancelAmbientRun.</summary>
    private void OnRunOrStopClicked(object? sender, EventArgs e)
    {
        HookBubbleSpeakInjector();
        try { RefreshModelStatusLabel(); } catch { /* ignore */ }

        if (_runInFlight)
        {
            RequestStopRun();
            return;
        }

        try
        {
            _runCts = AgentSession.BeginAmbientRun();
        }
        catch (Exception ex)
        {
            AuraLog.Exception("BeginAmbientRun", ex);
            _runCts = new CancellationTokenSource();
        }

        OnRunClicked(sender, e);

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(80);
            if (_runInFlight)
                SetRunButtonBusy(true);
        });
    }

    private void RequestStopRun()
    {
        try { AgentSession.CancelAmbientRun(); } catch { /* ignore */ }
        try { _runCts?.Cancel(); } catch { /* ignore */ }
        try { _ = _speech.StopAsync(); } catch { /* ignore */ }

        MainThread.BeginInvokeOnMainThread(() => SetRunButtonBusy(false));

        if (_runInFlight)
            _ = AppendBubbleAsync("⏹ Cancelando…", user: false, isTool: true);
    }

    private void SetRunButtonBusy(bool busy)
    {
        try
        {
            if (busy)
            {
                RunButton.Text = "■";
                RunButton.IsEnabled = true;
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

    /// <summary>
    /// Limpa bolhas + histórico compartilhado do AgentSession (continuidade sob controle).
    /// Pode ser chamado do menu se o handler legado não limpar SharedHistory.
    /// </summary>
    private void ResetConversationContinuity()
    {
        try { AgentSession.ClearSharedHistory(); } catch { /* ignore */ }
        try { _session = null; } catch { /* ignore */ }
    }

    private void TryInjectSpeakButton(Border border)
    {
        try
        {
            if (border.HorizontalOptions == LayoutOptions.End)
                return;

            if (border.Content is not VerticalStackLayout stack || stack.Children.Count < 2)
                return;

            foreach (var child in stack.Children)
            {
                if (child is Button b0 && b0.Text == "🔊")
                    return;
                if (child is HorizontalStackLayout hs)
                {
                    foreach (var c in hs.Children)
                        if (c is Button bb && bb.Text == "🔊")
                            return;
                }
            }

            string? payload = null;
            if (stack.Children[0] is Label lbl)
                payload = lbl.Text;
            if (string.IsNullOrWhiteSpace(payload))
                return;

            for (int i = 0; i < stack.Children.Count; i++)
            {
                if (stack.Children[i] is not Button btn || btn.Text != "📋")
                    continue;

                stack.Children.RemoveAt(i);
                var row = new HorizontalStackLayout
                {
                    Spacing = 4,
                    HorizontalOptions = LayoutOptions.End
                };
                row.Children.Add(btn);

                string text = payload!;
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
                    try { await SpeakAsync(text); }
                    catch (Exception ex) { AuraLog.Exception("SpeakBtn", ex); }
                };
                row.Children.Add(speakBtn);
                stack.Children.Insert(i, row);
                return;
            }
        }
        catch (Exception ex)
        {
            AuraLog.Exception("TryInjectSpeakButton", ex);
        }
    }
}
