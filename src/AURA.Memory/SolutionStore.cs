using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AURA.Core.Logging;
using AURA.Core.Runtime;

namespace AURA.Memory
{
    /// <summary>
    /// Armazena somente procedimentos conhecidos pela AURA.
    ///
    /// Diferente do histórico de conversa, este armazenamento representa
    /// conhecimento operacional reutilizável.
    /// </summary>
    public sealed class SolutionStore
    {
        private static readonly JsonSerializerOptions Options =
            new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                IncludeFields = true
            };

        private readonly ILogger _logger;
        private readonly string _path;
        private readonly object _sync = new object();

        public SolutionStore(
            ILogger? logger = null,
            string? path = null)
        {
            _logger = logger ?? new ConsoleLogger();

            _path = path ??
                SimulationRuntime.ExpandUserHome(
                    "~/.aura/solutions.json");
        }

        public string Path => _path;

        public IReadOnlyList<SolutionRule> ReadAll()
        {
            lock (_sync)
            {
                return LoadLocked()
                    .Where(x => x.Validated)
                    .ToList();
            }
        }

        // Abaixo do qual duas solicitações são consideradas intenções diferentes.
        // [ASSUMPTION: 0.82 veio de teste manual com variações comuns de frase
        // em pt-BR ("criar arquivo" vs "crie um arquivo"); ajustar se gerar
        // falsos positivos/negativos em uso real.]
        private const double FuzzyMatchThreshold = 0.82;

        public SolutionRule? Find(
            string intent,
            string target,
            string goal)
        {
            lock (_sync)
            {
                List<SolutionRule> validated = LoadLocked()
                    .Where(x => x.Validated)
                    .ToList();

                // Camada rápida (padrão): correspondência exata, normalizada.
                SolutionRule? exact = validated
                    .OrderByDescending(x => x.SuccessCount)
                    .FirstOrDefault(x =>
                        Same(x.Intent, intent) &&
                        Same(x.Target, target) &&
                        Same(x.Goal, goal));

                if (exact != null)
                    return exact;

                // Camada rápida (fallback): distância de Levenshtein normalizada.
                // Evita que a AURA "reaprenda" um procedimento já validado só
                // porque o usuário pediu a mesma coisa com outras palavras.
                return FindFuzzy(validated, intent, target, goal);
            }
        }

        private static SolutionRule? FindFuzzy(
            IEnumerable<SolutionRule> validated,
            string intent,
            string target,
            string goal)
        {
            SolutionRule? best = null;
            double bestScore = 0.0;

            foreach (SolutionRule rule in validated)
            {
                double score =
                    (Similarity(rule.Intent, intent) * 0.5) +
                    (Similarity(rule.Target, target) * 0.3) +
                    (Similarity(rule.Goal, goal) * 0.2);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = rule;
                }
            }

            return bestScore >= FuzzyMatchThreshold ? best : null;
        }

        /// <summary>
        /// Similaridade normalizada (0..1) via distância de Levenshtein.
        /// 1.0 = idêntico, 0.0 = completamente diferente.
        /// </summary>
        private static double Similarity(string? a, string? b)
        {
            string sa = (a ?? string.Empty).Trim().ToLowerInvariant();
            string sb = (b ?? string.Empty).Trim().ToLowerInvariant();

            int maxLen = Math.Max(sa.Length, sb.Length);
            if (maxLen == 0)
                return 1.0;

            int distance = LevenshteinDistance(sa, sb);
            return 1.0 - ((double)distance / maxLen);
        }

        private static int LevenshteinDistance(string a, string b)
        {
            int[] previous = new int[b.Length + 1];
            int[] current = new int[b.Length + 1];

            for (int j = 0; j <= b.Length; j++)
                previous[j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(
                        Math.Min(current[j - 1] + 1, previous[j] + 1),
                        previous[j - 1] + cost);
                }

                (int[] tmp0, int[] tmp1) = (current, previous);
                previous = tmp0;
                current = tmp1;
            }

            return previous[b.Length];
        }

        public void SaveValidated(SolutionRule rule)
        {
            if (rule == null)
                throw new ArgumentNullException(nameof(rule));

            if (!rule.Validated)
                throw new InvalidOperationException(
                    "Somente soluções validadas podem ser armazenadas.");

            if (string.IsNullOrWhiteSpace(rule.Id))
                throw new InvalidOperationException(
                    "A solução precisa de um Id.");

            lock (_sync)
            {
                List<SolutionRule> all = LoadLocked();

                SolutionRule? existing =
                    all.FirstOrDefault(x =>
                        string.Equals(
                            x.Id,
                            rule.Id,
                            StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    all.Add(rule);
                }
                else
                {
                    int index = all.IndexOf(existing);
                    all[index] = rule;
                }

                PersistLocked(all);
            }
        }

        public void RegisterSuccess(
            string id,
            DateTime? validatedAtUtc = null)
        {
            lock (_sync)
            {
                List<SolutionRule> all = LoadLocked();

                SolutionRule? rule =
                    all.FirstOrDefault(x =>
                        string.Equals(
                            x.Id,
                            id,
                            StringComparison.OrdinalIgnoreCase));

                if (rule == null)
                    return;

                rule.Validated = true;
                rule.SuccessCount++;
                rule.LastValidatedAtUtc =
                    validatedAtUtc ?? DateTime.UtcNow;

                PersistLocked(all);
            }
        }

        private List<SolutionRule> LoadLocked()
        {
            try
            {
                if (!File.Exists(_path))
                    return new List<SolutionRule>();

                string json = File.ReadAllText(_path);

                return JsonSerializer.Deserialize<
                    List<SolutionRule>>(json, Options)
                    ?? new List<SolutionRule>();
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    "Falha ao carregar soluções em '" +
                    _path + "': " +
                    ex.Message);

                return new List<SolutionRule>();
            }
        }

        private void PersistLocked(
            List<SolutionRule> rules)
        {
            string? directory =
                System.IO.Path.GetDirectoryName(_path);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string json =
                JsonSerializer.Serialize(rules, Options);

            string tmp = _path + ".tmp";

            File.WriteAllText(tmp, json);

            try
            {
                File.Move(
                    tmp,
                    _path,
                    overwrite: true);
            }
            catch
            {
                if (File.Exists(tmp))
                    File.Delete(tmp);

                throw;
            }
        }

        private static bool Same(
            string? a,
            string? b)
        {
            return string.Equals(
                a?.Trim(),
                b?.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
