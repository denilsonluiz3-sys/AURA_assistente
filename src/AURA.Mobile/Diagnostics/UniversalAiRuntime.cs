using AURA.AI;
using AURA.AI.UniversalAI;

namespace AURA.Mobile.Diagnostics;

/// <summary>
/// Único ponto de entrada da camada Mobile para criar o cliente de IA.
/// A seleção vem exclusivamente do RuntimeConfig; nenhum provider, modelo ou endpoint é escolhido aqui.
/// </summary>
public sealed class UniversalAiRuntime
{
    private readonly ILogger _logger;

    public UniversalAiRuntime(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public OpenRouterClient CreateClientFromRuntimeConfig()
    {
        string providerId = RuntimeConfig.Provider?.Trim() ?? string.Empty;
        string model = RuntimeConfig.Model?.Trim() ?? string.Empty;
        string apiKey = RuntimeConfig.ApiKey?.Trim() ?? string.Empty;
        string baseUrl = RuntimeConfig.BaseUrlOverride?.Trim() ?? string.Empty;
        string modelsUrl = RuntimeConfig.ModelsUrlOverride?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(providerId))
            throw new InvalidOperationException("Nenhum provedor de IA foi selecionado. Configure um provider antes de usar Chat ou Agent.");
        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Nenhum modelo de IA foi selecionado. Configure um modelo antes de usar Chat ou Agent.");

        var connection = UniversalRuntimeAdapter.CreateConnection(
            providerId,
            apiKey,
            model,
            string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl,
            string.IsNullOrWhiteSpace(modelsUrl) ? null : modelsUrl);

        OpenRouterClient client = UniversalAiClientFactory.Create(connection);
        RuntimeConfig.Apply(client);
        _logger.Info("AI runtime configurado: " + connection.Provider.Id + " · " + connection.Model);
        return client;
    }
}
