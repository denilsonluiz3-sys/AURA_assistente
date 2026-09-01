using System.Text;
using AURA.AI.UniversalAI;

namespace AURA.Mobile.Diagnostics;

public sealed class AiDiagnosticsService
{
    private readonly IUniversalAiClient _client;
    public AiDiagnosticsService(IUniversalAiClient client) => _client = client ?? throw new ArgumentNullException(nameof(client));
    public string LastDiagnosticContext { get; private set; } = string.Empty;
    public string LastAnalysis { get; private set; } = string.Empty;
    public IReadOnlyList<FixProposal> LastProposals { get; private set; } = Array.Empty<FixProposal>();
    public void CaptureDiagnosticContext(string? context) => LastDiagnosticContext = context ?? string.Empty;

    public string BuildInput()
    {
        var sb = new StringBuilder();
        sb.AppendLine("LOG DE EXECUÇÃO:"); sb.AppendLine(AuraLog.ReadRecentLog(RuntimeConfig.LogLinesForAnalysis) ?? "(vazio)");
        if (!string.IsNullOrWhiteSpace(LastDiagnosticContext)) { sb.AppendLine(); sb.AppendLine("DIAGNÓSTICO DO DISPOSITIVO:"); sb.AppendLine(LastDiagnosticContext); }
        sb.AppendLine(); sb.AppendLine("CONFIGURAÇÃO DA IA:");
        sb.AppendLine($"Provedor: {RuntimeConfig.Provider}"); sb.AppendLine($"Modelo: {_client.Options.Model}");
        sb.AppendLine($"max_tokens: {_client.Options.MaxTokens}"); sb.AppendLine($"timeout_seconds: {_client.Options.TimeoutSeconds}");
        return sb.ToString();
    }

    public async Task<string> AnalyzeAsync(CancellationToken ct = default)
    {
        var input = BuildInput();
        var prompt = "Você é o engenheiro de diagnóstico da AURA. Analise o material recebido, identifique causa raiz e diferencie erro, warning e informação. Responda em português.";
        LastAnalysis = await _client.ChatAsync(input, systemPrompt: prompt, ct: ct).ConfigureAwait(false);
        AuraLog.Info("Diagnóstico IA concluído."); return LastAnalysis;
    }

    public async Task<List<FixProposal>> ProposeFixesAsync(CancellationToken ct = default)
    {
        var input = BuildInput() + (string.IsNullOrWhiteSpace(LastAnalysis) ? string.Empty : "\n\nANÁLISE:\n" + LastAnalysis);
        var prompt = "Retorne exclusivamente JSON {\"fixes\":[{\"key\":\"...\",\"label\":\"...\",\"current\":\"...\",\"suggested\":\"...\",\"reason\":\"...\"}]}. Keys permitidas: model, provider, max_tokens, timeout_seconds, log_lines. Nunca retorne api_key ou código.";
        var answer = await _client.ChatAsync(input, systemPrompt: prompt, ct: ct).ConfigureAwait(false);
        LastProposals = FixProposalParser.Parse(answer); return LastProposals.ToList();
    }

    public int Apply(IEnumerable<FixProposal> selected)
    {
        var applied = 0;
        foreach (var fix in selected)
        {
            switch (fix.Key.Trim().ToLowerInvariant())
            {
                case "model": RuntimeConfig.Model = fix.Suggested.Trim(); applied++; break;
                case "provider": RuntimeConfig.Provider = fix.Suggested.Trim(); applied++; break;
                case "max_tokens" when int.TryParse(fix.Suggested, out var t) && t > 0: RuntimeConfig.MaxTokens = t; applied++; break;
                case "timeout_seconds" when int.TryParse(fix.Suggested, out var timeout) && timeout > 0: RuntimeConfig.TimeoutSeconds = timeout; applied++; break;
                case "log_lines" when int.TryParse(fix.Suggested, out var lines) && lines > 0: RuntimeConfig.LogLinesForAnalysis = lines; applied++; break;
            }
        }
        return applied;
    }
}
