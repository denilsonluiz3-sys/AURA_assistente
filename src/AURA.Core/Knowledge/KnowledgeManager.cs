using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Abstractions;
using AURA.Core.Logging;

namespace AURA.Core.Knowledge
{
    /// <summary>
    /// Camada de conhecimento da AURA: consulta offline (cache de arquivos JSON)
    /// e, quando não encontra, busca online no DuckDuckGo e aprende salvando o
    /// resultado. Implementa IAgent para entrar no fluxo unificado do núcleo.
    /// </summary>
    public sealed class KnowledgeManager : IAgent
    {
        private readonly string _cachePath;
        private readonly ILogger _logger;
        private readonly HttpClient _http;
        private readonly Dictionary<string, string> _local = new(StringComparer.OrdinalIgnoreCase);

        public string Name => "knowledge";
        public string Description => "Conhecimento offline/online (cache + DuckDuckGo) com aprendizado local";

        public KnowledgeManager(string cachePath = null, ILogger logger = null)
        {
            _cachePath = cachePath ?? "knowledge";
            _logger = logger ?? new ConsoleLogger();
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            Directory.CreateDirectory(_cachePath);
            SeedDefaults();
            LoadLocalKnowledge();
        }

        public void Start() => _logger.Info("[Knowledge] iniciado");

        public void Stop() => _logger.Info("[Knowledge] parado");

        public Task<string> AskAsync(string question, CancellationToken ct = default)
            => GetKnowledgeAsync(question, ct);

        public async Task<string> GetKnowledgeAsync(string query, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return string.Empty;

            string key = query.Trim();

            if (_local.TryGetValue(key, out string? cached) && !string.IsNullOrWhiteSpace(cached))
                return cached;

            string? extracted = await SearchOnlineAsync(key, ct).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(extracted))
                return string.Empty;

            _local[key] = extracted;
            SaveKnowledge(key, extracted);
            return extracted;
        }

        private async Task<string?> SearchOnlineAsync(string query, CancellationToken ct)
        {
            try
            {
                string url = "https://api.duckduckgo.com/?q="
                    + Uri.EscapeDataString(query)
                    + "&format=json&no_html=1&skip_disambig=1";

                string json = await _http.GetStringAsync(url, ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("AbstractText", out JsonElement abstractText))
                {
                    string? text = abstractText.GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }

                if (root.TryGetProperty("Heading", out JsonElement heading))
                {
                    string? head = heading.GetString();
                    if (!string.IsNullOrWhiteSpace(head))
                        return head;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Warning("[Knowledge] online: " + ex.Message);
                return null;
            }
        }

        private void LoadLocalKnowledge()
        {
            try
            {
                foreach (string file in Directory.GetFiles(_cachePath, "*.json"))
                {
                    string json = File.ReadAllText(file);
                    var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (data == null)
                        continue;

                    foreach (KeyValuePair<string, string> item in data)
                        _local[item.Key.Trim()] = item.Value;
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("[Knowledge] load: " + ex.Message);
            }
        }

        private void SaveKnowledge(string key, string value)
        {
            try
            {
                string file = Path.Combine(_cachePath, Guid.NewGuid().ToString("N") + ".json");
                File.WriteAllText(file, JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    [key] = value
                }));
            }
            catch (Exception ex)
            {
                _logger.Warning("[Knowledge] save: " + ex.Message);
            }
        }

        private void SeedDefaults()
        {
            _local["prazo contestação"] = "15 dias úteis para apresentar defesa";
            _local["liminar"] = "Decisão provisória em até 48h";
            _local["revelia"] = "Réu não contestou, presunção de veracidade";
            _local["cobrança"] = "Notificação → Liminar → Citação → Sentença → Execução";
            _local["ação trabalhista"] = "Reclamação → Audiência → Sentença → Recurso";
            _local["execução"] = "Cumprimento da sentença, penhora de bens";
        }
    }
}