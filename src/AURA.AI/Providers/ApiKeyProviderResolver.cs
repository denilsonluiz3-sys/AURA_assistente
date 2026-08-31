using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AURA.AI;

namespace AURA.AI.Providers
{
    /// <summary>
    /// Resolve a qual provedor de IA pertence uma API key e valida a
    /// credencial. Desacoplado da UI: recebe um ProviderCredential e devolve
    /// resultados estruturados. A detecção determinística por formato da chave
    /// nunca faz rede; o teste de endpoints só ocorre com AllowProbe=true.
    /// </summary>
    public sealed class ApiKeyProviderResolver : IApiKeyProviderResolver
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        public ProviderDetectionResult Detect(ProviderCredential credential)
        {
            string key = (credential.ApiKey ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                return new ProviderDetectionResult
                {
                    Source = ProviderDetectionSource.None,
                    Message = "Chave vazia."
                };
            }

            var matched = new List<IAiProvider>();
            int longestPrefix = 0;
            foreach (ProviderInfo p in ProviderCatalog.Providers)
            {
                if (!p.NeedsKey) continue;
                foreach (string prefix in p.KeyPrefixes)
                {
                    if (key.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        if (prefix.Length > longestPrefix)
                        {
                            longestPrefix = prefix.Length;
                            matched.Clear();
                            matched.Add(p);
                        }
                        else if (prefix.Length == longestPrefix)
                        {
                            matched.Add(p);
                        }
                        break;
                    }
                }
            }

            if (matched.Count == 1)
            {
                return new ProviderDetectionResult
                {
                    Provider = matched[0],
                    Source = ProviderDetectionSource.KeyFormat,
                    Message = "Provedor identificado pelo formato da chave: " + matched[0].Name + "."
                };
            }

            if (!string.IsNullOrWhiteSpace(credential.PreferredProviderName))
            {
                ProviderInfo? preferred = ProviderCatalog.Find(credential.PreferredProviderName);
                if (preferred != null)
                {
                    if (matched.Count > 1 && !matched.Contains(preferred))
                    {
                        return new ProviderDetectionResult
                        {
                            Candidates = matched,
                            Source = ProviderDetectionSource.None,
                            Message = "Chave ambígua (formato compatível com vários provedores); o selecionado não é um deles."
                        };
                    }

                    return new ProviderDetectionResult
                    {
                        Provider = preferred,
                        Candidates = matched,
                        Source = matched.Count > 0 ? ProviderDetectionSource.KeyFormat : ProviderDetectionSource.Context,
                        Message = "Sem prefixo de chave conhecido; usando o provedor selecionado: " + preferred.Name + "."
                    };
                }
            }

            if (matched.Count > 1)
            {
                return new ProviderDetectionResult
                {
                    Candidates = matched,
                    Source = ProviderDetectionSource.None,
                    Message = "Chave ambígua (formato compatível com " + matched.Count + " provedores). Toque em 'Testar' para descobrir."
                };
            }

            return new ProviderDetectionResult
            {
                Candidates = new List<IAiProvider>(ProviderCatalog.KeyedProbeCandidates()),
                Source = ProviderDetectionSource.None,
                Message = "Formato da chave desconhecido. Toque em 'Testar' para descobrir o provedor."
            };
        }

        public async Task<ProviderHealthResult> ValidateAsync(
            ProviderCredential credential,
            HttpClient? http = null,
            CancellationToken ct = default)
        {
            ProviderDetectionResult detection = Detect(credential);
            if (detection.Provider == null && detection.Candidates.Count == 0)
                return new ProviderHealthResult { Status = ProviderHealthStatus.UnknownProvider, Message = "Não foi possível identificar o provedor." };

            if (detection.Provider == null && !credential.AllowProbe)
                return new ProviderHealthResult
                {
                    Status = ProviderHealthStatus.UnknownProvider,
                    Message = "Provedor não identificado e teste externo não autorizado. Habilite a validação para testar os provedores compatíveis."
                };

            var candidates = new List<IAiProvider>();
            if (detection.Provider != null) candidates.Add(detection.Provider);
            foreach (IAiProvider c in detection.Candidates)
                if (!candidates.Contains(c)) candidates.Add(c);

            HttpClient client = http ?? new HttpClient();
            bool ownsClient = http == null;
            ProviderHealthResult? best = null;
            try
            {
                foreach (IAiProvider provider in candidates)
                {
                    if (!provider.NeedsKey) continue;
                    ProviderHealthResult r = await ProbeAsync(client, provider, credential.ApiKey, credential.Timeout ?? DefaultTimeout, ct);
                    if (r.Status == ProviderHealthStatus.Valid) return r;
                    if (best == null || Prefer(best.Status, r.Status)) best = r;
                    if (!credential.AllowProbe) break;
                }
            }
            finally
            {
                if (ownsClient) client.Dispose();
            }

            return best ?? new ProviderHealthResult
            {
                Status = ProviderHealthStatus.UnknownProvider,
                Message = "Nenhum provedor compatível pôde ser testado."
            };
        }

        public async Task<ProviderDetectionResult> ResolveAsync(
            ProviderCredential credential,
            HttpClient? http = null,
            CancellationToken ct = default)
        {
            ProviderDetectionResult detection = Detect(credential);
            if (detection.Provider != null)
            {
                ProviderHealthResult health = await ValidateAsync(credential, http, ct);
                detection.Message = detection.Message + " " + health.Message;
            }
            else if (credential.AllowProbe && detection.Candidates.Count > 0)
            {
                ProviderHealthResult health = await ValidateAsync(credential, http, ct);
                if (health.Provider != null && health.Status == ProviderHealthStatus.Valid)
                {
                    detection.Provider = health.Provider;
                    detection.Source = ProviderDetectionSource.Probe;
                    detection.Message = "Provedor descoberto testando os endpoints: " + health.Provider.Name + ".";
                }
                else
                {
                    detection.Message = detection.Message + " " + health.Message;
                }
            }
            return detection;
        }

        public void ApplyToClient(OpenRouterClient client, ProviderDetectionResult result)
        {
            if (client == null || result.Provider is not ProviderInfo p) return;

            // O resolver determina o transporte. A credencial permanece sob o
            // controle do RuntimeConfig, que a lê do armazenamento seguro por
            // provider. Assim a detecção não precisa transportar ou persistir a chave.
            client.Options.Provider = p.Id;
            client.Options.BaseUrl = p.BaseUrl;
            client.Options.Model = string.IsNullOrWhiteSpace(p.DefaultModelId) && p.Models.Count > 0
                ? p.Models[0].Id
                : p.DefaultModelId;
            client.Options.AuthHeaderName = p.AuthHeaderName;
            client.Options.AuthScheme = p.AuthScheme;
            client.Options.ApiFormat = p.ApiFormat;
        }

        private static bool Prefer(ProviderHealthStatus current, ProviderHealthStatus next) =>
            next == ProviderHealthStatus.Unauthorized && current != ProviderHealthStatus.Unauthorized;

        private static async Task<ProviderHealthResult> ProbeAsync(
            HttpClient client, IAiProvider provider, string key, TimeSpan timeout, CancellationToken ct)
        {
            var result = new ProviderHealthResult { Provider = provider };
            string url = string.IsNullOrWhiteSpace(provider.ModelsUrl) ? provider.BaseUrl : provider.ModelsUrl;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (!string.IsNullOrWhiteSpace(key))
                request.Headers.TryAddWithoutValidation(provider.AuthHeaderName, provider.AuthScheme + key);

            try
            {
                HttpResponseMessage response = await client.SendAsync(request, cts.Token).ConfigureAwait(false);
                result.HttpStatusCode = (int)response.StatusCode;
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        result.Status = ProviderHealthStatus.Valid;
                        result.Message = "Credencial válida em " + provider.Name + ".";
                        break;
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        result.Status = ProviderHealthStatus.Unauthorized;
                        result.Message = "Chave rejeitada por " + provider.Name + " (" + (int)response.StatusCode + ").";
                        break;
                    case (HttpStatusCode)402:
                    case (HttpStatusCode)429:
                        result.Status = ProviderHealthStatus.InsufficientCredits;
                        result.Message = provider.Name + " aceitou a chave mas está sem créditos/cota (" + (int)response.StatusCode + ").";
                        break;
                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.NotFound:
                        result.Status = ProviderHealthStatus.Invalid;
                        result.Message = "Endpoint inválido para " + provider.Name + " (" + (int)response.StatusCode + ").";
                        break;
                    default:
                        result.Status = (int)response.StatusCode >= 500
                            ? ProviderHealthStatus.ProviderUnavailable
                            : ProviderHealthStatus.Invalid;
                        result.Message = provider.Name + " respondeu (" + (int)response.StatusCode + ").";
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                result.Status = ProviderHealthStatus.ProviderUnavailable;
                result.Message = "Timeout ao contatar " + provider.Name + ".";
            }
            catch (HttpRequestException hre)
            {
                result.Status = ProviderHealthStatus.ProviderUnavailable;
                result.Message = "Falha de rede ao contatar " + provider.Name +
                                 (hre.InnerException != null ? " (" + hre.InnerException.GetType().Name + ")" : string.Empty) + ".";
            }
            catch (Exception)
            {
                result.Status = ProviderHealthStatus.ProviderUnavailable;
                result.Message = "Falha ao contatar " + provider.Name + ".";
            }
            return result;
        }
    }
}
