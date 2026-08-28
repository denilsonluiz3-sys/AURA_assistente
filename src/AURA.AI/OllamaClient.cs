using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AURA.AI
{
    /// <summary>
    /// Cliente nativo da API HTTP do Ollama.
    /// Não exige API key e usa /api/chat em vez da compatibilidade OpenAI.
    /// </summary>
    public sealed class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://127.0.0.1:11435";
        public string Model { get; set; } = "qwen2:0.5b";
        public int TimeoutSeconds { get; set; } = 120;
    }

    public sealed class OllamaClient
    {
        private readonly HttpClient _httpClient;

        public OllamaOptions Options { get; }

        public OllamaClient(OllamaOptions options, HttpClient? httpClient = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, Options.TimeoutSeconds));
        }

        /// <summary>
        /// Cria uma requisição para POST {BaseUrl}/api/chat.
        /// </summary>
        public HttpRequestMessage BuildRequest(
            IReadOnlyList<OllamaMessage> messages,
            bool stream = false,
            CancellationToken ct = default)
        {
            if (messages == null || messages.Count == 0)
                throw new ArgumentException("A lista de mensagens não pode ser vazia.", nameof(messages));
            if (string.IsNullOrWhiteSpace(Options.Model))
                throw new InvalidOperationException("O modelo Ollama não foi configurado.");

            var payload = new
            {
                model = Options.Model,
                messages,
                stream
            };

            string json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Post, BuildEndpoint("/api/chat"));
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return request;
        }

        public async Task<string> ChatAsync(
            string question,
            string? systemPrompt = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("A pergunta não pode ser vazia.", nameof(question));

            var messages = new List<OllamaMessage>();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                messages.Add(new OllamaMessage("system", systemPrompt));
            messages.Add(new OllamaMessage("user", question));

            using HttpRequestMessage request = BuildRequest(messages, stream: false, ct);
            using HttpResponseMessage response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string detail = string.IsNullOrWhiteSpace(body) ? "(sem corpo)" : body;
                if (detail.Length > 1000)
                    detail = detail.Substring(0, 1000);
                throw new HttpRequestException(
                    $"Falha na API Ollama ({(int)response.StatusCode} {response.StatusCode}): {detail}");
            }

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("message", out JsonElement message) &&
                message.TryGetProperty("content", out JsonElement content))
            {
                return content.GetString() ?? string.Empty;
            }

            return body;
        }

        /// <summary>
        /// Lista os modelos instalados no Ollama através de GET /api/tags.
        /// </summary>
        public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default)
        {
            using HttpResponseMessage response = await _httpClient
                .GetAsync(BuildEndpoint("/api/tags"), ct)
                .ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                string detail = string.IsNullOrWhiteSpace(body) ? "(sem corpo)" : body;
                throw new HttpRequestException(
                    $"Falha ao listar modelos Ollama ({(int)response.StatusCode} {response.StatusCode}): {detail}");
            }

            using JsonDocument document = JsonDocument.Parse(body);
            var result = new List<string>();
            if (document.RootElement.TryGetProperty("models", out JsonElement models))
            {
                foreach (JsonElement model in models.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out JsonElement name))
                    {
                        string? value = name.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            result.Add(value);
                    }
                }
            }

            return result;
        }

        private string BuildEndpoint(string path)
        {
            string baseUrl = Options.BaseUrl.TrimEnd('/');
            return baseUrl + (path.StartsWith('/') ? path : "/" + path);
        }
    }

    public sealed record OllamaMessage(string Role, string Content);
}
