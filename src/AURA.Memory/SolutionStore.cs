using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AURA.Core.Logging;

namespace AURA.Memory
{
    /// <summary>
    /// Memória procedural: salva tarefas bem-sucedidas e recupera por similaridade
    /// (Levenshtein + palavras-chave + recência). Sem dependências extras.
    /// </summary>
    public sealed class SolutionStore
    {
        private readonly string _path;
        private readonly ILogger _logger;
        private readonly object _sync = new object();
        private List<SolutionEntry> _entries = new();
        private readonly int _maxAgeDays;
        private readonly int _maxEntries;

        public SolutionStore(ILogger logger, string path = null, int maxAgeDays = 30, int maxEntries = 50)
        {
            _logger = logger ?? new ConsoleLogger();
            _maxAgeDays = maxAgeDays;
            _maxEntries = maxEntries;
            string baseDir = path ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "aura");
            Directory.CreateDirectory(baseDir);
            _path = Path.Combine(baseDir, "aura_agent_memory.json");
            Load();
            Cleanup();
        }

        public SolutionEntry FindBestMatch(string task, int threshold = 75)
        {
            if (string.IsNullOrWhiteSpace(task)) return null;
            string q = task.Trim().ToLowerInvariant();

            lock (_sync)
            {
                SolutionEntry best = null;
                double bestScore = 0;

                foreach (SolutionEntry m in _entries.Where(e => e.IsSuccess))
                {
                    if (!HasMinKeywords(q, m.TaskDescription, 2))
                        continue;

                    int sim = Levenshtein.SimilarityPercent(q, m.TaskDescription);
                    double days = (DateTime.UtcNow - m.Timestamp).TotalDays;
                    double bonus = Math.Max(0, (7 - days) / 7.0 * 10.0);
                    double score = sim + bonus;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = m;
                    }
                }

                return bestScore >= threshold ? best : null;
            }
        }

        public void Record(string task, string actionTaken, string resultDetails, bool success)
        {
            if (string.IsNullOrWhiteSpace(task)) return;

            lock (_sync)
            {
                _entries.Add(new SolutionEntry
                {
                    Id = Guid.NewGuid().ToString("N"),
                    TaskDescription = task.Trim().ToLowerInvariant(),
                    ActionTaken = actionTaken ?? "",
                    ResultDetails = resultDetails ?? "",
                    IsSuccess = success,
                    Timestamp = DateTime.UtcNow
                });
                CleanupLocked();
                SaveLocked();
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                string json = File.ReadAllText(_path);
                _entries = JsonSerializer.Deserialize<List<SolutionEntry>>(json) ?? new List<SolutionEntry>();
            }
            catch (Exception ex)
            {
                _logger.Warning("SolutionStore load: " + ex.Message);
                _entries = new List<SolutionEntry>();
            }
        }

        private void SaveLocked()
        {
            try
            {
                var opts = new JsonSerializerOptions { WriteIndented = false };
                File.WriteAllText(_path, JsonSerializer.Serialize(_entries, opts));
            }
            catch (Exception ex)
            {
                _logger.Warning("SolutionStore save: " + ex.Message);
            }
        }

        private void Cleanup()
        {
            lock (_sync) CleanupLocked();
        }

        private void CleanupLocked()
        {
            DateTime cutoff = DateTime.UtcNow.AddDays(-_maxAgeDays);
            _entries = _entries
                .Where(e => e.Timestamp >= cutoff)
                .OrderByDescending(e => e.Timestamp)
                .Take(_maxEntries)
                .ToList();
        }

        private static bool HasMinKeywords(string input, string stored, int min)
        {
            var stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "o","a","os","as","um","uma","de","do","da","em","com","para","por","e","the","a","to","of"
            };
            var a = input.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !stop.Contains(w)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var b = (stored ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !stop.Contains(w)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return a.Intersect(b, StringComparer.OrdinalIgnoreCase).Count() >= min;
        }
    }

    public sealed class SolutionEntry
    {
        public string Id { get; set; } = "";
        public string TaskDescription { get; set; } = "";
        public string ActionTaken { get; set; } = "";
        public string ResultDetails { get; set; } = "";
        public bool IsSuccess { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>Levenshtein leve (sem NuGet).</summary>
    public static class Levenshtein
    {
        public static int SimilarityPercent(string s, string t)
        {
            if (s == t) return 100;
            if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 100 : 0;
            if (string.IsNullOrEmpty(t)) return 0;

            int n = s.Length, m = t.Length;
            int[] prev = new int[m + 1];
            int[] cur = new int[m + 1];
            for (int j = 0; j <= m; j++) prev[j] = j;

            for (int i = 1; i <= n; i++)
            {
                cur[0] = i;
                for (int j = 1; j <= m; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                (prev, cur) = (cur, prev);
            }

            int dist = prev[m];
            int maxLen = Math.Max(n, m);
            return (int)((1.0 - (double)dist / maxLen) * 100.0);
        }
    }
}
