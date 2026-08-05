using AURA.AI;

namespace AURA.Mobile.Pages;

public partial class LogsPage : ContentPage
{
    private readonly OpenRouterClient _client;

    public LogsPage(OpenRouterClient client)
    {
        InitializeComponent();
        _client = client;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _client.Options.ApiKey = Preferences.Default.Get("openrouter_key", string.Empty);
        LoadLog();
    }

    private void OnRefreshClicked(object sender, EventArgs e)
    {
        LoadLog();
    }

    private void LoadLog()
    {
        try
        {
            LogViewer.Text = AuraLog.ReadRecentLog(400);
        }
        catch (Exception ex)
        {
            LogViewer.Text = "Erro ao carregar o log: " + ex.Message;
        }
    }

    private async void OnAnalyzeClicked(object sender, EventArgs e)
    {
        string apiKey = _client.Options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            LogViewer.Text = "Configure a chave OpenRouter na aba Assistente primeiro.";
            return;
        }

        string logContent = AuraLog.ReadRecentLog(400);
        if (string.IsNullOrWhiteSpace(logContent))
        {
            LogViewer.Text = "Log vazio — não há o que analisar.";
            return;
        }

        AnalyzeButton.IsEnabled = false;
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;
        LogViewer.Text = "Enviando log para análise da IA...\n\n" + logContent;

        string systemPrompt =
            "Você é o engenheiro de diagnóstico do app AURA (assistente de IA para Android, " +
            "feito em .NET MAUI). Receba o log de execução do app e: " +
            "1) identifique a causa raiz de qualquer exceção/erro; " +
            "2) explique em português de forma clara e curta; " +
            "3) sugira a correção exata (arquivo, linha e trecho de código quando possível). " +
            "Se não houver erro, apenas resuma o que o log mostra. Responda de forma objetiva.";

        try
        {
            string analysis = await _client.ChatAsync(logContent, systemPrompt: systemPrompt);
            LogViewer.Text = "=== ANÁLISE DA IA ===\n\n" + analysis;
            AuraLog.Info("Análise IA concluída.");
        }
        catch (Exception ex)
        {
            LogViewer.Text = "Falha na análise: " + ex.Message;
            AuraLog.Exception("LogsPage.OnAnalyzeClicked", ex);
        }
        finally
        {
            AnalyzeButton.IsEnabled = true;
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }
}
