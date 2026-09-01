using AURA.AI.UniversalAI;
using System.Net;
using System.Net.Http;
using AURA.AI;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Pages;

public partial class LogsPage : ContentPage
{
    private readonly IUniversalAiClient _client;
    private readonly AiDiagnosticsService _diagnostics;

    public LogsPage(IUniversalAiClient client, AiDiagnosticsService diagnostics)
    {
        InitializeComponent();
        _client = client;
        _diagnostics = diagnostics;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RuntimeConfig.Apply(_client);
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

    private async void OnCopyClicked(object sender, EventArgs e)
    {
        string content = AuraLog.ReadRecentLog(2000);
        if (string.IsNullOrWhiteSpace(content))
        {
            LogViewer.Text = "(log vazio)";
            return;
        }

        await Clipboard.Default.SetTextAsync(content);
        LogViewer.Text = "Log copiado para a área de transferência.\n\n" + content;
    }

    private async void OnShareClicked(object sender, EventArgs e)
    {
        string content = AuraLog.ReadRecentLog(2000);
        if (string.IsNullOrWhiteSpace(content))
        {
            LogViewer.Text = "(log vazio)";
            return;
        }

        string filePath = Path.Combine(FileSystem.CacheDirectory, "aura_log.txt");
        await File.WriteAllTextAsync(filePath, content);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Log AURA",
            File = new ShareFile(filePath, "text/plain")
        });
    }

    private async void OnTestClicked(object sender, EventArgs e)
    {
        TestButton.IsEnabled = false;
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;
        LogViewer.Text = "Testando conexão...\n";

        var sb = new System.Text.StringBuilder();
        try
        {
            RuntimeConfig.Apply(_client);
            string? readinessError = RuntimeConfig.EnsureReadyForRequest(_client);
            sb.AppendLine($"Provedor: {_client.Options.Provider}");
            sb.AppendLine($"Modelo: {_client.Options.Model}");
            sb.AppendLine($"URL: {_client.Options.BaseUrl}");
            sb.AppendLine($"Chave: {(string.IsNullOrWhiteSpace(_client.Options.ApiKey) ? "não configurada" : "configurada")}");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(readinessError))
            {
                sb.AppendLine("RESULTADO: FALHA — " + readinessError);
                LogViewer.Text = sb.ToString();
                return;
            }

            var current = Connectivity.Current.NetworkAccess;
            sb.AppendLine($"1) Acesso à rede: {current}");

            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.All
            };
            using var http = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(Math.Max(30, _client.Options.TimeoutSeconds))
            };

            var baseUri = new Uri(_client.Options.BaseUrl);
            sb.AppendLine($"2) Conectando a {baseUri.Host} (TLS)...");
            using (var probe = new HttpRequestMessage(HttpMethod.Head, new Uri(baseUri.GetLeftPart(UriPartial.Authority))))
            {
                using HttpResponseMessage ping = await http.SendAsync(probe);
                sb.AppendLine($"   Resposta: HTTP {(int)ping.StatusCode} {ping.StatusCode}");
            }

            sb.AppendLine("3) Chamada de teste ao LLM...");
            string modelEcho = await _client.ChatAsync(
                "Responda apenas: OK",
                http,
                systemPrompt: "Você responde apenas OK.");
            sb.AppendLine($"   Resposta do modelo: \"{modelEcho}\"");
            sb.AppendLine();
            sb.AppendLine("RESULTADO: CONEXÃO OK — a IA respondeu.");
            AuraLog.Info("Teste de conexão AURA: OK");
        }
        catch (HttpRequestException hex)
        {
            sb.AppendLine();
            sb.AppendLine("RESULTADO: FALHA de HTTP.");
            sb.AppendLine("Erro: " + hex.Message);
            AuraLog.Exception("LogsPage.OnTestClicked (Http)", hex);
        }
        catch (TaskCanceledException)
        {
            sb.AppendLine();
            sb.AppendLine("RESULTADO: FALHA — tempo esgotado.");
            sb.AppendLine("Dica: verifique rede, endpoint e modelo selecionado.");
        }
        catch (Exception ex)
        {
            sb.AppendLine();
            sb.AppendLine("RESULTADO: FALHA inesperada.");
            sb.AppendLine("Erro: " + ex);
            AuraLog.Exception("LogsPage.OnTestClicked", ex);
        }
        finally
        {
            LogViewer.Text = sb.ToString();
            TestButton.IsEnabled = true;
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    private async void OnAnalyzeClicked(object sender, EventArgs e)
    {
        AnalyzeButton.IsEnabled = false;
        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;
        LogViewer.Text = "Enviando diagnóstico consolidado para a IA...\n\n" +
                         AuraLog.ReadRecentLog(RuntimeConfig.LogLinesForAnalysis);

        try
        {
            string analysis = await _diagnostics.AnalyzeAsync();
            LogViewer.Text = "=== ANÁLISE DA IA ===\n\n" + analysis;
        }
        catch (Exception ex)
        {
            LogViewer.Text = "Falha na análise: " + ex.Message +
                "\n\nUse 'Testar conexão' para verificar o provedor.";
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
