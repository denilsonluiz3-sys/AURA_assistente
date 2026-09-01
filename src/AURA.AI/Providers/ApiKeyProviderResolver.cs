using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AURA.AI;

namespace AURA.AI.Providers
{
    public sealed class ApiKeyProviderResolver : IApiKeyProviderResolver
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        public ProviderDetectionResult Detect(ProviderCredential credential)
        {
            string key = (credential.ApiKey ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key)) return new ProviderDetectionResult { Source = ProviderDetectionSource.None, Message = "Chave vazia." };
            var matched = new List<IAiProvider>();
            int longestPrefix = 0;
            foreach (ProviderInfo p in ProviderCatalog.Providers)
            {
                if (!p.NeedsKey) continue;
                foreach (string prefix in p.KeyPrefixes)
                {
                    if (!key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                    if (prefix.Length > longestPrefix) { longestPrefix = prefix.Length; matched.Clear(); matched.Add(p); }
                    else if (prefix.Length == longestPrefix) matched.Add(p);
                    break;
                }
            }
            if (matched.Count == 1) return new ProviderDetectionResult { Provider = matched[0], Source = ProviderDetectionSource.KeyFormat, Message = "Provedor identificado pelo formato da chave: " + matched[0].Name + "." };
            if (!string.IsNullOrWhiteSpace(credential.PreferredProviderName))
            {
                ProviderInfo? preferred = ProviderCatalog.Find(credential.PreferredProviderName);
                if (preferred != null) return new ProviderDetectionResult { Provider = preferred, Candidates = matched, Source = ProviderDetectionSource.Context, Message = "Usando o provedor selecionado: " + preferred.Name + "." };
            }
            return new ProviderDetectionResult { Candidates = matched.Count > 0 ? matched : new List<IAiProvider>(ProviderCatalog.KeyedProbeCandidates()), Source = ProviderDetectionSource.None, Message = "Formato da chave não identifica o provedor; endpoints compatíveis serão testados." };
        }

        public async Task<ProviderHealthResult> ValidateAsync(ProviderCredential credential, HttpClient? http = null, CancellationToken ct = default)
        {
            ProviderDetectionResult detection = Detect(credential);
            if (!credential.AllowProbe && detection.Provider == null) return new ProviderHealthResult { Status = ProviderHealthStatus.UnknownProvider, Message = "A descoberta por endpoint requer validação autorizada." };
            var candidates = new List<IAiProvider>();
            if (detection.Provider != null) candidates.Add(detection.Provider);
            foreach (IAiProvider c in detection.Candidates) if (!candidates.Contains(c)) candidates.Add(c);
            using HttpClient? owned = http == null ? new HttpClient() : null;
            HttpClient client = http ?? owned!;
            ProviderHealthResult? best = null;
            foreach (IAiProvider provider in candidates)
            {
                if (!provider.NeedsKey || string.IsNullOrWhiteSpace(provider.ModelsUrl)) continue;
                ProviderHealthResult result = await ProbeAsync(client, provider, credential.ApiKey, credential.Timeout ?? DefaultTimeout, ct).ConfigureAwait(false);
                if (result.Status == ProviderHealthStatus.Valid) return result;
                if (best == null || Prefer(best.Status, result.Status)) best = result;
            }
            return best ?? new ProviderHealthResult { Status = ProviderHealthStatus.UnknownProvider, Message = "Nenhum endpoint compatível foi encontrado." };
        }

        public async Task<ProviderDetectionResult> ResolveAsync(ProviderCredential credential, HttpClient? http = null, CancellationToken ct = default)
        {
            ProviderDetectionResult detection = Detect(credential);
            if (!credential.AllowProbe) return detection;
            ProviderHealthResult health = await ValidateAsync(credential, http, ct).ConfigureAwait(false);
            if (health.Provider != null && health.Status == ProviderHealthStatus.Valid)
            {
                detection.Provider = health.Provider;
                detection.Source = ProviderDetectionSource.Probe;
                detection.Message = "Provedor descoberto: " + health.Provider.Name + ".";
            }
            else detection.Message = health.Message;
            return detection;
        }

        public void ApplyToClient(OpenRouterClient client, ProviderDetectionResult result)
        {
            if (client == null || result.Provider is not ProviderInfo p) return;
            client.Options.Provider = p.Id;
            client.Options.BaseUrl = p.BaseUrl;
            client.Options.Model = !string.IsNullOrWhiteSpace(p.DefaultModelId) ? p.DefaultModelId : (p.Models.Count > 0 ? p.Models[0].Id : string.Empty);
            client.Options.AuthHeaderName = p.AuthHeaderName;
            client.Options.AuthScheme = p.AuthScheme;
            client.Options.ApiFormat = p.ApiFormat;
        }

        private static bool Prefer(ProviderHealthStatus current, ProviderHealthStatus next) => next == ProviderHealthStatus.Unauthorized && current != ProviderHealthStatus.Unauthorized;

        private static async Task<ProviderHealthResult> ProbeAsync(HttpClient client, IAiProvider provider, string key, TimeSpan timeout, CancellationToken ct)
        {
            var result = new ProviderHealthResult { Provider = provider };
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            using var request = new HttpRequestMessage(HttpMethod.Get, provider.ModelsUrl);
            if (!string.IsNullOrWhiteSpace(key)) request.Headers.TryAddWithoutValidation(provider.AuthHeaderName, provider.AuthScheme + key);
            try
            {
                HttpResponseMessage response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                result.HttpStatusCode = (int)response.StatusCode;
                if (response.IsSuccessStatusCode) { result.Status = ProviderHealthStatus.Valid; result.Message = "Credencial válida em " + provider.Name + "."; }
                else if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { result.Status = ProviderHealthStatus.Unauthorized; result.Message = "Chave rejeitada por " + provider.Name + "."; }
                else if ((int)response.StatusCode is 402 or 429) { result.Status = ProviderHealthStatus.InsufficientCredits; result.Message = provider.Name + " aceitou a credencial, mas retornou " + (int)response.StatusCode + "."; }
                else { result.Status = ProviderHealthStatus.Invalid; result.Message = provider.Name + " retornou HTTP " + (int)response.StatusCode + "."; }
            }
            catch (OperationCanceledException) { result.Status = ProviderHealthStatus.ProviderUnavailable; result.Message = "Timeout ao contatar " + provider.Name + "."; }
            catch (HttpRequestException) { result.Status = ProviderHealthStatus.ProviderUnavailable; result.Message = "Falha de rede ao contatar " + provider.Name + "."; }
            return result;
        }
    }
}
