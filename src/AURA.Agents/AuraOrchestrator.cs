using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Logging;
using AURA.Core.Launchers;
using AURA.Core.Runtime;
using AURA.Memory;

namespace AURA.Agents
{
    /// <summary>
    /// Fase 3: loop Sense → Plan → Act → Verify sem depender de LLM pago.
    /// Usa memória procedural, busca DuckDuckGo Lite e Runner de células.
    /// </summary>
    public sealed class AuraOrchestrator
    {
        private const int MaxSteps = 5;
        private readonly ILogger _logger;
        private readonly SolutionStore _memory;
        private readonly Runner _runner;
        private readonly SimulationRuntime _runtime;
        private readonly HttpClient _http;

        public AuraOrchestrator(
            ILogger logger,
            SolutionStore memory,
            Runner runner,
            SimulationRuntime runtime,
            HttpClient httpClient = null)
        {
            _logger = logger ?? new ConsoleLogger();
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _http = httpClient ?? CreateAntiDetectClient();
        }

        /// <summary>Executa comando do usuário de forma autônoma (headless).</summary>
        public async Task<string> ExecuteAsync(string userCommand, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(userCommand))
                return "Comando vazio.";

            userCommand = userCommand.Trim();
            _logger.Info("[ORQUESTRA] " + userCommand);

            // 1) SENSE — memória
            SolutionEntry hit = _memory.FindBestMatch(userCommand);
            if (hit != null)
            {
                _logger.Info("[MEMÓRIA] hit " + hit.Id);
                return "💾 Memória:\nAção: " + hit.ActionTaken + "\n" + hit.ResultDetails;
            }

            // 2) PLAN + ACT
            var history = new List<string>();
            string context = "";

            for (int step = 1; step <= MaxSteps; step++)
            {
                ct.ThrowIfCancellationRequested();
                bool wantsSearch = NeedsSearch(userCommand) || (step == 1 && NeedsResearchFirst(userCommand));
                bool wantsRun = NeedsExecution(userCommand);

                if (wantsSearch && string.IsNullOrEmpty(context))
                {
                    _logger.Info("[ORQUESTRA] busca web passo " + step);
                    context = await SearchWithRefinementAsync(userCommand, ct).ConfigureAwait(false);
                    history.Add("search:" + userCommand);

                    if (IsSearchOnly(userCommand))
                    {
                        _memory.Record(userCommand, "web_search", context, success: true);
                        return context;
                    }
                    continue;
                }

                if (wantsRun)
                {
                    string path = ExtractFilePath(userCommand);
                    if (path != null && _runner.CanRun(path))
                    {
                        _logger.Info("[ORQUESTRA] runner " + path);
                        try
                        {
                            Cell cell = await _runner.RunAsync(_runtime, null, path).ConfigureAwait(false);
                            await Task.Delay(800, ct).ConfigureAwait(false);
                            string log = _runtime.ReadCellLog(cell.Id, 40);
                            string msg = "✅ Célula " + cell.Id + " [" + cell.State + "]\n" + log;
                            _memory.Record(userCommand, "run:" + path, msg, success: true);
                            return msg;
                        }
                        catch (Exception ex)
                        {
                            string err = "❌ Execução: " + ex.Message;
                            if (!string.IsNullOrEmpty(context))
                                err += "\n\nContexto web:\n" + context;
                            _memory.Record(userCommand, "run_fail", err, success: false);
                            return err;
                        }
                    }

                    // Sem arquivo: devolve contexto ou orientação
                    if (!string.IsNullOrEmpty(context))
                    {
                        string combined = "Contexto obtido. Refine o comando ou indique o arquivo:\n" + context;
                        _memory.Record(userCommand, "context_only", combined, success: true);
                        return combined;
                    }
                }

                // Fallback: só busca
                if (string.IsNullOrEmpty(context))
                {
                    context = await SearchWithRefinementAsync(userCommand, ct).ConfigureAwait(false);
                    _memory.Record(userCommand, "web_search", context, success: !string.IsNullOrWhiteSpace(context));
                    return context;
                }

                break;
            }

            return string.IsNullOrEmpty(context)
                ? "Limite de passos. Seja mais específico."
                : context;
        }

        // ── busca ──────────────────────────────────────────────

        public async Task<string> SearchWithRefinementAsync(string query, CancellationToken ct = default)
        {
            string current = query;
            for (int i = 0; i <= 2; i++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await Task.Delay(Random.Shared.Next(400, 1200), ct).ConfigureAwait(false);
                    var results = await SearchDuckDuckGoLiteAsync(current, ct).ConfigureAwait(false);
                    if (results.Count > 0)
                        return FormatResults(results);

                    current = RefineQuery(query, i);
                }
                catch (Exception ex)
                {
                    _logger.Warning("[SEARCH] " + ex.Message);
                    if (i == 2) return "Falha na busca: " + ex.Message;
                }
            }
            return "Nenhum resultado após refinamentos.";
        }

        private async Task<List<(string Title, string Url)>> SearchDuckDuckGoLiteAsync(string query, CancellationToken ct)
        {
            string url = "https://lite.duckduckgo.com/lite/?q=" + Uri.EscapeDataString(query);
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("Referer", "https://lite.duckduckgo.com/");
            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
            string html = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            var list = new List<(string, string)>();
            // links da versão lite
            var re = new Regex(@"<a[^>]+href=""(https?://[^""]+)""[^>]*>([^<]+)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
            foreach (Match m in re.Matches(html))
            {
                string href = m.Groups[1].Value;
                string title = System.Net.WebUtility.HtmlDecode(m.Groups[2].Value).Trim();
                if (href.Contains("duckduckgo.com", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (title.Length < 3) continue;
                list.Add((title, href));
                if (list.Count >= 5) break;
            }
            return list;
        }

        private static string FormatResults(List<(string Title, string Url)> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Resultados da Web:");
            foreach (var r in results)
                sb.AppendLine("- **" + r.Title + "**: " + r.Url);
            return sb.ToString();
        }

        private static string RefineQuery(string q, int attempt)
        {
            if (attempt == 0) return q + " tutorial";
            if (attempt == 1)
            {
                string[] parts = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return string.Join(" ", parts.Take(Math.Min(4, parts.Length))) + " how to";
            }
            return q;
        }

        // ── heurísticas ────────────────────────────────────────

        private static bool NeedsSearch(string t)
        {
            string l = t.ToLowerInvariant();
            return l.Contains("pesquise") || l.Contains("busque") || l.Contains("procure")
                || l.Contains("o que é") || l.Contains("o que e") || l.Contains("como ")
                || l.Contains("search") || l.Contains("what is");
        }

        private static bool IsSearchOnly(string t)
        {
            string l = t.ToLowerInvariant();
            return (l.Contains("pesquise") || l.Contains("busque") || l.Contains("procure"))
                && !NeedsExecution(t);
        }

        private static bool NeedsResearchFirst(string t)
        {
            string l = t.ToLowerInvariant();
            return l.Contains("como ") || l.Contains("tutorial");
        }

        private static bool NeedsExecution(string t)
        {
            string l = t.ToLowerInvariant();
            return l.Contains("execute") || l.Contains("rode") || l.Contains("rodar")
                || l.Contains("crie") || l.Contains("run ")
                || l.EndsWith(".py") || l.EndsWith(".sh") || l.EndsWith(".jar")
                || l.EndsWith(".dll") || l.EndsWith(".js");
        }

        private static string ExtractFilePath(string t)
        {
            var m = Regex.Match(t, @"(/[^\s]+?\.(py|sh|jar|dll|js|bash))", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;
            m = Regex.Match(t, @"([\w\./\\-]+\.(py|sh|jar|dll|js|bash))", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        private static HttpClient CreateAntiDetectClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Linux; Android 14; Pixel 8 Pro) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Mobile Safari/537.36");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "pt-BR,pt;q=0.9,en;q=0.8");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Ch-Ua-Mobile", "?1");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Ch-Ua-Platform", "\"Android\"");
            return client;
        }
    }
}
