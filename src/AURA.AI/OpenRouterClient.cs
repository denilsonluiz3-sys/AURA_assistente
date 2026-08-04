using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Logging;
using AURA.Memory;

namespace AURA.AI
{
    /// <summary>
    /// Configurações do provedor LLM. O mobile (AURA.AI) expõe o mesmo
    /// provedor via MemoryService; aqui o cliente HTTP direto. Defaults seguem
    /// o config do aichat (OpenRouter, qwen/qwen-plus).
    /// </summary>
    public sealed class OpenRouterOptions
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";
        public string Model { get; set; } = "qwen/qwen-plus";
        public string? AppReference { get; set; }
    }

    /// <summary>
    /// Cliente mínimo para OpenRouter chat completions. Construa a requisição
    /// (testável sem rede) com BuildRequest; execute com ChatAsync.
    /// </summary>
    public sealed class OpenRouterClient
    {
        private readonly OpenRouterOptions _options;
        private readonly ILogger _logger;

        public OpenRouterClient(OpenRouterOptions options, ILogger? logger = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? new ConsoleLogger();
        }

        public HttpRequestMessage BuildRequest(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("A pergunta não pode ser vazia.", nameof(question));
            }

            var payload = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new { role = "user", content = question }
                },
                @const = new Dictionary<string, object>
                {
                    ["include_reasoning"] = false
                }
            };

            string json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + _options.ApiKey);
            if (_options.AppReference != null)
            {
                request.Headers.TryAddWithoutValidation("X-Title", "AURA");
                request.Headers.TryAddWithoutValidation("X-URL", _options.AppReference);
            }

            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return request;
        }

        public async Task<string> ChatAsync(string question,
            HttpClient? httpClient = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException(
                    "ApiKey do provedor LLM não configurada. Defina OpenRouterOptions.ApiKey.");
            }

            HttpClient client = httpClient ?? new HttpClient();
            HttpRequestMessage request = BuildRequest(question);

            HttpResponseMessage response = await client.SendAsync(request, ct).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Error("LLM: " + response.StatusCode + " " + body);
                throw new HttpRequestException("Falha na chamada LLM: " + response.StatusCode);
            }

            using var document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("choices", out JsonElement choices) &&
                choices.GetArrayLength() > 0)
            {
                JsonElement first = choices[0];
                if (first.TryGetProperty("message", out JsonElement message) &&
                    message.TryGetProperty("content", out JsonElement content))
                {
                    return content.GetString() ?? string.Empty;
                }
            }

            return body;
        }
    }
}
