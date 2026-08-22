using System.Text;
using AURA.AI;

namespace AURA.Mobile.Diagnostics;

/// <summary>
/// Pipeline único para diagnóstico assistido por IA no Mobile.
/// Mantém o log/contexto diagnóstico que alimentam Analisar e Correções.
/// </summary>
public sealed class AiDiagnosticsService
{
    private readonly OpenRouterClient _client;

    public AiDiagnosticsService(OpenRouterClient client)
    {
        _client = client;
    }

    public string LastDiagnosticContext { get; private set; } = string.Empty;
    public string LastAnalysis { get; private set; } = string.Empty;
    public IReadOnlyList<FixProposal> LastProposals { get; private set; } = Array.Empty<FixProposal>();

    public void CaptureDiagnosticContext(string? context)
    {
        LastDiagnosticContext = context ?? string.Empty;
    }

    public string BuildInput()
    {
        string log = AuraLog.ReadRecentLog(RuntimeConfig.LogLinesForAnalysis);
        var sb = new StringBuilder();
        sb.AppendLine("LOG DE EXECUÇÃO:");
        sb.AppendLine(string.IsNullOrWhiteSpace(log) ? "(vazio)" : log);

        if (!string.IsNullOrWhiteSpace(LastDiagnosticContext))
        {
            sb.AppendLine();
            sb.AppendLine("DIAGNÓSTICO DO DISPOSITIVO:");
            sb.AppendLine(LastDiagnosticContext);
        }

        sb.AppendLine();
        sb.AppendLine("CONFIGURAÇÃO DA IA:");
        sb.AppendLine($"Provedor: {RuntimeConfig.Provider}");
        sb.AppendLine($"Modelo: {_client.Options.Model}");
        sb.AppendLine($"max_tokens: {_client.Options.MaxTokens}");
        sb.AppendLine($"timeout_seconds: {_client.Options.TimeoutSeconds}");
        sb.AppendLine($"log_lines: {RuntimeConfig.LogLinesForAnalysis}");
        sb.AppendLine($"api_key: {(string.IsNullOrWhiteSpace(_client.Options.ApiKey) ? "(vazio)" : "(configurada)")}");
        return sb.ToString();
    }

    public async Task<string> AnalyzeAsync(CancellationToken ct = default)
    {
        EnsureReady();

        string input = BuildInput();
        if (string.IsNullOrWhiteSpace(input.Replace("(vazio)", string.Empty, StringComparison.Ordinal)))
        {
            return "Não há log ou diagnóstico disponível para análise.";
        }

        string systemPrompt =
            "Você é o engenheiro de diagnóstico do AURA (.NET MAUI Android). " +
            "Analise o LOG DE EXECUÇÃO e, quando presente, o DIAGNÓSTICO DO DISPOSITIVO. " +
            "Identifique a causa raiz antes de sintomas secundários. " +
            "Separe erro real, warning e informação. Explique em português de forma objetiva. " +
            "Quando houver falha de código, indique arquivo, símbolo e linha somente quando houver " +
            "evidência no material recebido. Não invente arquivos, linhas ou correções. " +
            "Se não houver falha, diga explicitamente que o diagnóstico não encontrou erro estrutural.";

        string analysis = await _client.ChatAsync(input, systemPrompt: systemPrompt, ct: ct);
        LastAnalysis = analysis;
        AuraLog.Info("Diagnóstico IA concluído.");
        return analysis;
    }

    public async Task<List<FixProposal>> ProposeFixesAsync(CancellationToken ct = default)
    {
        EnsureReady();

        string input = BuildInput();
        if (!string.IsNullOrWhiteSpace(LastAnalysis))
        {
            input += "\n\nANÁLISE IA ANTERIOR:\n" + LastAnalysis;
        }

        string systemPrompt =
            "Você é o engenheiro de manutenção do AURA (.NET MAUI para Android). " +
            "Receba log, diagnóstico e, se existir, análise anterior. Proponha SOMENTE correções " +
            "determinísticas aplicáveis em tempo de execução, sem recompilar o APK. " +
            "Responda EXCLUSIVAMENTE JSON válido no formato " +
            "{\"fixes\":[{\"key\":\"...\",\"label\":\"...\",\"current\":\"...\",\"suggested\":\"...\",\"reason\":\"...\"}]}. " +
            "Keys permitidas: model, provider, max_tokens, timeout_seconds, log_lines. " +
            "Nunca proponha api_key, código arbitrário, comandos shell, alteração de arquivos ou mudança " +
            "de PolicyGuard. Se não houver correção determinística, retorne {\"fixes\":[]}.";

        string answer = await _client.ChatAsync(input, systemPrompt: systemPrompt, ct: ct);
        List<FixProposal> proposals = FixProposalParser.Parse(answer);
        LastProposals = proposals;
        AuraLog.Info("Correções IA propostas: " + proposals.Count);
        return proposals;
    }

    public int Apply(IEnumerable<FixProposal> selected)
    {
        int applied = 0;
        foreach (FixProposal fix in selected)
        {
            try
            {
                switch (fix.Key.Trim().ToLowerInvariant())
                {
                    case "model":
                        RuntimeConfig.Model = fix.Suggested.Trim();
                        applied++;
                        break;
                    case "provider":
                        RuntimeConfig.Provider = fix.Suggested.Trim();
                        applied++;
                        break;
                    case "max_tokens":
                        if (int.TryParse(fix.Suggested, out int tokens) && tokens > 0)
                        {
                            RuntimeConfig.MaxTokens = tokens;
                            applied++;
                        }
                        break;
                    case "timeout_seconds":
                        if (int.TryParse(fix.Suggested, out int timeout) && timeout > 0)
                        {
                            RuntimeConfig.TimeoutSeconds = timeout;
                            applied++;
                        }
                        break;
                    case "log_lines":
                        if (int.TryParse(fix.Suggested, out int lines) && lines > 0)
                        {
                            RuntimeConfig.LogLinesForAnalysis = lines;
                            applied++;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                AuraLog.Exception("AiDiagnosticsService.Apply '" + fix.Key + "'", ex);
            }
        }

        RuntimeConfig.Apply(_client);
        AuraLog.Info("Correções IA aplicadas: " + applied);
        return applied;
    }

    private void EnsureReady()
    {
        RuntimeConfig.Apply(_client);
        string? error = RuntimeConfig.EnsureReadyForRequest(_client);
        if (!string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException(error);
        }
    }
}
