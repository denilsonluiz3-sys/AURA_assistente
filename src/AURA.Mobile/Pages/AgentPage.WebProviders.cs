using AURA.Mobile.Services;

namespace AURA.Mobile.Pages;

/// <summary>
/// Catálogo e navegação Web AI (chrome compacto).
/// Partial — não altera o monólito AgentPage.xaml.cs.
/// </summary>
public partial class AgentPage
{
    private string _activeWebProviderId = "deepseek";

    private static readonly (string Id, string Label, string Url)[] WebProvidersPrimary =
    {
        ("deepseek", "DeepSeek", "https://chat.deepseek.com"),
        ("chatgpt", "ChatGPT", "https://chatgpt.com"),
        ("claude", "Claude", "https://claude.ai"),
        ("gemini", "Gemini", "https://gemini.google.com"),
        ("perplexity", "Perplexity", "https://www.perplexity.ai"),
    };

    private static readonly (string Id, string Label, string Url)[] WebProvidersMore =
    {
        ("copilot", "Copilot", "https://copilot.microsoft.com"),
        ("poe", "Poe", "https://poe.com"),
        ("openrouter", "OpenRouter", "https://openrouter.ai/chat"),
        ("hf", "Hugging Face", "https://huggingface.co/chat"),
        ("aistudio", "Google AI Studio", "https://aistudio.google.com"),
        ("notebooklm", "NotebookLM", "https://notebooklm.google.com"),
        ("github", "GitHub", "https://github.com"),
        ("stackoverflow", "Stack Overflow", "https://stackoverflow.com"),
    };

    private void OpenWebProvider(string id, string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            BridgeWebView.Source = url;
            _webLoaded = true;
            _activeWebProviderId = id ?? string.Empty;
            HighlightActiveWebProvider();
        }
        catch (Exception ex)
        {
            AuraLog.Exception("WebProvider.Open", ex);
        }
    }

    private void HighlightActiveWebProvider()
    {
        try
        {
            var accent = (Color)Application.Current!.Resources["AuraAccent"];
            var surface = (Color)Application.Current!.Resources["AuraSurface2"];
            var onAccent = Colors.White;
            var onSurface = (Color)Application.Current!.Resources["AuraTextPrimary"];

            void Style(Button? btn, string id)
            {
                if (btn == null) return;
                bool on = string.Equals(_activeWebProviderId, id, StringComparison.OrdinalIgnoreCase);
                btn.BackgroundColor = on ? accent : surface;
                btn.TextColor = on ? onAccent : onSurface;
            }

            Style(WebBtnDeepSeek, "deepseek");
            Style(WebBtnChatGpt, "chatgpt");
            Style(WebBtnClaude, "claude");
            Style(WebBtnGemini, "gemini");
            Style(WebBtnPerplexity, "perplexity");
        }
        catch
        {
            /* resources may be unavailable early */
        }
    }

    private string? CurrentWebUrl()
    {
        try
        {
            if (BridgeWebView?.Source is UrlWebViewSource u && !string.IsNullOrWhiteSpace(u.Url))
                return u.Url;
        }
        catch { /* ignore */ }

        var all = WebProvidersPrimary.Concat(WebProvidersMore);
        var hit = all.FirstOrDefault(p => p.Id == _activeWebProviderId);
        return string.IsNullOrEmpty(hit.Url) ? null : hit.Url;
    }

    private void OnWebProviderClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string id)
            return;

        var all = WebProvidersPrimary.Concat(WebProvidersMore);
        var hit = all.FirstOrDefault(p => p.Id == id);
        if (string.IsNullOrEmpty(hit.Url))
            return;
        OpenWebProvider(hit.Id, hit.Url);
    }

    private async void OnWebMoreClicked(object? sender, EventArgs e)
    {
        try
        {
            string[] labels = WebProvidersMore.Select(p => p.Label)
                .Append("↗ Abrir no Navegador")
                .ToArray();
            string chosen = await DisplayActionSheetAsync("Mais sites", "Cancelar", null, labels);
            if (string.IsNullOrEmpty(chosen) || chosen == "Cancelar")
                return;

            if (chosen == "↗ Abrir no Navegador")
            {
                string? url = CurrentWebUrl();
                if (string.IsNullOrWhiteSpace(url))
                {
                    await SafeAlertAsync("Navegador", "Nenhuma URL ativa na Web AI.");
                    return;
                }
                await AuraBrowserBridge.OpenInAppBrowserAsync(url, this);
                return;
            }

            var hit = WebProvidersMore.FirstOrDefault(p => p.Label == chosen);
            if (string.IsNullOrEmpty(hit.Url))
                return;
            OpenWebProvider(hit.Id, hit.Url);
        }
        catch (Exception ex)
        {
            AuraLog.Exception("WebProvider.More", ex);
        }
    }
}
