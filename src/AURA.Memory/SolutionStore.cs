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

        public SolutionRule? Find(
            string intent,
            string target,
            string goal)
        {
            lock (_sync)
            {
                return LoadLocked()
                    .Where(x => x.Validated)
                    .OrderByDescending(x => x.SuccessCount)
                    .FirstOrDefault(x =>
                        Same(x.Intent, intent) &&
                        Same(x.Target, target) &&
                        Same(x.Goal, goal));
            }
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
