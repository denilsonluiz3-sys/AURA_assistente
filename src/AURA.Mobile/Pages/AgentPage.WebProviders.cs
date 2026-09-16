using AURA.Mobile.Services;
using AURA.Core.Security;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace AURA.Mobile.Pages;

/// <summary>
/// Catálogo, navegação e gerenciamento persistente de URLs da Web AI.
/// Os provedores oficiais são fixos; URLs adicionadas pelo usuário podem ser editadas ou removidas.
/// </summary>
public partial class AgentPage
{
    private const string CustomWebProvidersPreference = "aura.webai.custom-providers.v1";
    private readonly List<CustomWebProvider> _customWebProviders = new();
    private string _activeWebProviderId = "deepseek";
    private bool _webEventsHooked;
    private bool _customWebProvidersLoaded;

    private sealed class CustomWebProvider
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    private void HookWebViewEvents()
    {
        if (_webEventsHooked || BridgeWebView == null)
            return;
        _webEventsHooked = true;
        LoadCustomWebProviders();
        BridgeWebView.Navigated += OnWebNavigated;
        BridgeWebView.Navigating += OnWebNavigating;
    }

    private void OnWebNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!WebSecurityPolicy.TryValidateHttpUrl(e.Url, out _))
        {
            e.Cancel = true;
            AuraLog.Info("Web AI: navegação bloqueada: " + WebSecurityPolicy.RedactForLog(e.Url));
        }
    }

    private async void OnWebNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result == WebNavigationResult.Success)
            return;
        await SafeAlertAsync("Web AI", "Não foi possível carregar este provedor. Verifique a conexão ou escolha outro site.");
    }

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

    private void LoadCustomWebProviders()
    {
        if (_customWebProvidersLoaded) return;
        _customWebProvidersLoaded = true;
        try
        {
            string raw = Preferences.Default.Get(CustomWebProvidersPreference, "[]");
            var saved = JsonSerializer.Deserialize<List<CustomWebProvider>>(raw) ?? new();
            foreach (var item in saved.Where(IsValidCustomProvider))
                _customWebProviders.Add(item);
        }
        catch (Exception ex) { AuraLog.Exception("WebProvider.LoadCustom", ex); }
    }

    private void SaveCustomWebProviders()
    {
        Preferences.Default.Set(CustomWebProvidersPreference, JsonSerializer.Serialize(_customWebProviders));
    }

    private static bool IsValidCustomProvider(CustomWebProvider? item) =>
        item != null && !string.IsNullOrWhiteSpace(item.Id) && !string.IsNullOrWhiteSpace(item.Label)
        && WebSecurityPolicy.TryValidateHttpUrl(item.Url, out _);

    private IEnumerable<(string Id, string Label, string Url)> AllWebProviders()
    {
        LoadCustomWebProviders();
        return WebProvidersPrimary.Concat(WebProvidersMore)
            .Concat(_customWebProviders.Select(x => (x.Id, x.Label, x.Url)));
    }

    private static bool TryExtractHttpUrl(string text, out string url)
    {
        url = string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var match = System.Text.RegularExpressions.Regex.Match(text, @"https?://[^\s<>""']+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success || !WebSecurityPolicy.TryValidateHttpUrl(match.Value.TrimEnd('.', ',', ';', ')', ']'), out Uri parsed)) return false;
        url = parsed.AbsoluteUri;
        return true;
    }

    private void OpenWebUrlFromAgent(string url)
    {
        _webMode = true; StatusBarHost.IsVisible = false; InputBarHost.IsVisible = false; ApplyModeUi();
        OpenWebProvider("custom", url);
    }

    private void OpenWebProvider(string id, string url)
    {
        if (!WebSecurityPolicy.TryValidateHttpUrl(url, out Uri validated)) return;
        url = validated.AbsoluteUri;
        try { BridgeWebView.Source = url; _webLoaded = true; _activeWebProviderId = id ?? string.Empty; HighlightActiveWebProvider(); }
        catch (Exception ex) { AuraLog.Exception("WebProvider.Open", ex); }
    }

    private void HighlightActiveWebProvider()
    {
        try
        {
            var accent = (Color)Application.Current!.Resources["AuraAccent"];
            var surface = (Color)Application.Current.Resources["AuraSurface2"];
            var primary = (Color)Application.Current.Resources["AuraTextPrimary"];
            void Style(Button? btn, string id) { if (btn != null) { bool on = string.Equals(_activeWebProviderId, id, StringComparison.OrdinalIgnoreCase); btn.BackgroundColor = on ? accent : surface; btn.TextColor = on ? Colors.White : primary; } }
            Style(WebBtnDeepSeek, "deepseek"); Style(WebBtnChatGpt, "chatgpt"); Style(WebBtnClaude, "claude"); Style(WebBtnGemini, "gemini"); Style(WebBtnPerplexity, "perplexity");
        }
        catch { }
    }

    private string? CurrentWebUrl()
    {
        try { if (BridgeWebView?.Source is UrlWebViewSource u && !string.IsNullOrWhiteSpace(u.Url)) return u.Url; } catch { }
        var hit = AllWebProviders().FirstOrDefault(p => p.Id == _activeWebProviderId);
        return string.IsNullOrEmpty(hit.Url) ? null : hit.Url;
    }

    private void OnWebProviderClicked(object? sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string id) return;
        var hit = AllWebProviders().FirstOrDefault(p => p.Id == id);
        if (!string.IsNullOrEmpty(hit.Url)) OpenWebProvider(hit.Id, hit.Url);
    }

    private async void OnWebMoreClicked(object? sender, EventArgs e)
    {
        try
        {
            LoadCustomWebProviders();
            string[] labels = WebProvidersMore.Select(p => p.Label)
                .Concat(_customWebProviders.Select(p => "★ " + p.Label))
                .Append("⚙ Gerenciar URLs").Append("↗ Abrir no Navegador").ToArray();
            string chosen = await DisplayActionSheetAsync("Mais sites", "Cancelar", null, labels);
            if (string.IsNullOrEmpty(chosen) || chosen == "Cancelar") return;
            if (chosen == "⚙ Gerenciar URLs") { await ManageCustomWebProvidersAsync(); return; }
            if (chosen == "↗ Abrir no Navegador")
            {
                string? url = CurrentWebUrl();
                if (string.IsNullOrWhiteSpace(url)) { await SafeAlertAsync("Navegador", "Nenhuma URL ativa na Web AI."); return; }
                await AuraBrowserBridge.OpenInAppBrowserAsync(url, this); return;
            }
            var hit = WebProvidersMore.FirstOrDefault(p => p.Label == chosen);
            if (!string.IsNullOrEmpty(hit.Url)) { OpenWebProvider(hit.Id, hit.Url); return; }
            string customLabel = chosen.StartsWith("★ ", StringComparison.Ordinal) ? chosen[2..] : chosen;
            var custom = _customWebProviders.FirstOrDefault(p => p.Label == customLabel);
            if (custom != null) OpenWebProvider(custom.Id, custom.Url);
        }
        catch (Exception ex) { AuraLog.Exception("WebProvider.More", ex); }
    }

    private async Task ManageCustomWebProvidersAsync()
    {
        LoadCustomWebProviders();
        string[] actions = { "Adicionar URL", "Editar URL existente", "Remover URL existente", "Cancelar" };
        string action = await DisplayActionSheetAsync("URLs personalizadas", "Cancelar", null, actions);
        switch (action)
        {
            case "Adicionar URL": await AddCustomWebProviderAsync(); break;
            case "Editar URL existente": await EditCustomWebProviderAsync(); break;
            case "Remover URL existente": await RemoveCustomWebProviderAsync(); break;
        }
    }

    private async Task AddCustomWebProviderAsync()
    {
        string label = (await DisplayPromptAsync("Adicionar URL", "Nome do site ou serviço", "Salvar", "Cancelar", "Minha IA", keyboard: Keyboard.Text) ?? string.Empty).Trim();
        if (label.Length == 0) return;
        string rawUrl = (await DisplayPromptAsync("Adicionar URL", "URL completa", "Salvar", "Cancelar", "https://", keyboard: Keyboard.Url) ?? string.Empty).Trim();
        if (!TryExtractHttpUrl(rawUrl, out string url)) { await SafeAlertAsync("URL inválida", "Informe uma URL http:// ou https:// válida."); return; }
        string id = "custom-" + Guid.NewGuid().ToString("N");
        _customWebProviders.Add(new CustomWebProvider { Id = id, Label = label, Url = url }); SaveCustomWebProviders(); OpenWebProvider(id, url);
    }

    private async Task EditCustomWebProviderAsync()
    {
        if (_customWebProviders.Count == 0) { await SafeAlertAsync("URLs personalizadas", "Nenhuma URL personalizada cadastrada."); return; }
        string[] labels = _customWebProviders.Select(x => x.Label).Append("Cancelar").ToArray();
        string selected = await DisplayActionSheetAsync("Escolha uma URL", "Cancelar", null, labels);
        var item = _customWebProviders.FirstOrDefault(x => x.Label == selected);
        if (item == null) return;
        string label = (await DisplayPromptAsync("Editar URL", "Nome", "Salvar", "Cancelar", item.Label, keyboard: Keyboard.Text) ?? item.Label).Trim();
        string rawUrl = (await DisplayPromptAsync("Editar URL", "URL completa", "Salvar", "Cancelar", item.Url, keyboard: Keyboard.Url) ?? item.Url).Trim();
        if (label.Length == 0 || !TryExtractHttpUrl(rawUrl, out string url)) { await SafeAlertAsync("Dados inválidos", "Mantenha um nome e uma URL http:// ou https:// válida."); return; }
        item.Label = label; item.Url = url; SaveCustomWebProviders(); OpenWebProvider(item.Id, item.Url);
    }

    private async Task RemoveCustomWebProviderAsync()
    {
        if (_customWebProviders.Count == 0) { await SafeAlertAsync("URLs personalizadas", "Nenhuma URL personalizada cadastrada."); return; }
        string[] labels = _customWebProviders.Select(x => x.Label).Append("Cancelar").ToArray();
        string selected = await DisplayActionSheetAsync("Escolha uma URL", "Cancelar", null, labels);
        var item = _customWebProviders.FirstOrDefault(x => x.Label == selected);
        if (item == null) return;
        bool confirm = await DisplayAlertAsync("Remover URL", $"Remover '{item.Label}' da Web AI?", "Remover", "Cancelar");
        if (!confirm) return;
        _customWebProviders.Remove(item); SaveCustomWebProviders();
        if (_activeWebProviderId == item.Id) OpenWebProvider("deepseek", WebProvidersPrimary[0].Url);
    }
}
