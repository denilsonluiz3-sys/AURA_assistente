using AURA.AI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Pages;

public partial class FixesPage : ContentPage
{
    private readonly OpenRouterClient _client;
    private readonly AiDiagnosticsService _diagnostics;
    private List<FixProposal> _pending = new();

    public FixesPage(OpenRouterClient client, AiDiagnosticsService diagnostics)
    {
        InitializeComponent();
        _client = client;
        _diagnostics = diagnostics;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RuntimeConfig.Apply(_client);
        ShowCurrentConfig();

        if (_diagnostics.LastProposals.Count > 0)
        {
            _pending = _diagnostics.LastProposals.ToList();
            FixesView.ItemsSource = _pending;
            StatusLabel.Text =
                $"{_pending.Count} correção(ões) já propostas pela IA. " +
                "Revise e toque em Aplicar.\n\nConfiguração atual:\n" + ShowCurrentConfigRaw();
        }
    }

    private void ShowCurrentConfig()
    {
        string current =
            $"Configuração atual:\n" +
            $"Provedor: {RuntimeConfig.Provider} ({(RuntimeConfig.Provider.Length == 0 ? "padrão" : RuntimeConfig.Provider)})\n" +
            $"Modelo: {_client.Options.Model}\n" +
            $"max_tokens: {_client.Options.MaxTokens}\n" +
            $"timeout: {_client.Options.TimeoutSeconds}s\n" +
            $"linhas de log analisadas: {RuntimeConfig.LogLinesForAnalysis}\n" +
            $"chave: {(string.IsNullOrWhiteSpace(_client.Options.ApiKey) ? "ausente" : "configurada")}\n" +
            $"URL: {_client.Options.BaseUrl}";

        StatusLabel.Text = current;
    }

    private async void OnAnalyzeClicked(object sender, EventArgs e)
    {
        AnalyzeButton.IsEnabled = false;
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;
        StatusLabel.Text = "Analisando log, diagnóstico e configuração...";

        try
        {
            string? readinessError = RuntimeConfig.EnsureReadyForRequest(_client);
            if (!string.IsNullOrWhiteSpace(readinessError))
            {
                StatusLabel.Text = readinessError;
                return;
            }

            _pending = await _diagnostics.ProposeFixesAsync();
            FixesView.ItemsSource = null;
            FixesView.ItemsSource = _pending;

            if (_pending.Count == 0)
            {
                StatusLabel.Text =
                    "Nenhuma correção determinística identificada pela IA.\n\n" +
                    "Análise anterior:\n" +
                    (_diagnostics.LastAnalysis.Length > 3000
                        ? _diagnostics.LastAnalysis.Substring(0, 3000) + "\n…"
                        : _diagnostics.LastAnalysis);
            }
            else
            {
                StatusLabel.Text =
                    $"{_pending.Count} correção(ões) proposta(s) pela IA. " +
                    "Marque as desejadas e toque em Aplicar.\n\n" +
                    "Configuração atual:\n" + ShowCurrentConfigRaw();
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Falha na análise: " + ex.Message;
            AuraLog.Exception("FixesPage.OnAnalyzeClicked", ex);
        }
        finally
        {
            AnalyzeButton.IsEnabled = true;
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    private string ShowCurrentConfigRaw()
    {
        return
            $"Provedor: {RuntimeConfig.Provider}\n" +
            $"Modelo: {_client.Options.Model}\n" +
            $"max_tokens: {_client.Options.MaxTokens}\n" +
            $"timeout_seconds: {_client.Options.TimeoutSeconds}\n" +
            $"log_lines: {RuntimeConfig.LogLinesForAnalysis}\n" +
            $"api_key: {(string.IsNullOrWhiteSpace(_client.Options.ApiKey) ? "(vazio)" : "(configurada)" )}";
    }

    private void OnApplyClicked(object sender, EventArgs e)
    {
        var selected = _pending.Where(p => p.Selected).ToList();
        if (selected.Count == 0)
        {
            StatusLabel.Text = "Nenhuma correção marcada para aplicar.";
            return;
        }

        int applied = _diagnostics.Apply(selected);
        RuntimeConfig.Apply(_client);
        ShowCurrentConfig();

        StatusLabel.Text =
            $"Aplicadas {applied} de {selected.Count} correção(ões).\n\n" +
            "Configuração atual:\n" + ShowCurrentConfigRaw();
        AuraLog.Info("Correções aplicadas pela UI: " + applied + "/" + selected.Count);
    }

    private void OnResetClicked(object sender, EventArgs e)
    {
        Preferences.Default.Remove("ai_provider");
        Preferences.Default.Remove("ai_model");
        Preferences.Default.Remove("ai_max_tokens");
        Preferences.Default.Remove("ai_timeout_seconds");
        Preferences.Default.Remove("ai_log_lines");
        Preferences.Default.Remove("ai_api_key");

        RuntimeConfig.Apply(_client);
        _pending = new List<FixProposal>();
        FixesView.ItemsSource = null;
        ShowCurrentConfig();
        StatusLabel.Text = "Configuração restaurada para o padrão.";
    }
}
