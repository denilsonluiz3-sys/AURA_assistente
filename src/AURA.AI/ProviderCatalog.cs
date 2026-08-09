using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace AURA.AI
{
    public sealed class ProviderModel
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsFree { get; init; }

        public override string ToString() =>
            IsFree ? $"{Label} (grátis)" : Label;
    }

    public sealed class ProviderInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string BaseUrl { get; init; } = string.Empty;
        public bool NeedsKey { get; init; } = true;
        public string KeyHint { get; init; } = string.Empty;
        public string KeyEnv { get; init; } = string.Empty;
        public List<ProviderModel> Models { get; set; } = new();
    }

    public static class ProviderCatalog
    {
        private static readonly List<ProviderInfo> ProvidersList = Load();

        public static List<ProviderInfo> Providers => ProvidersList;

        /// <summary>
        /// Recarrega o catálogo de providers a partir de config/providers.json.
        /// </summary>
        public static void Reload()
        {
            ProvidersList.Clear();

            foreach (var provider in Load())
            {
                ProvidersList.Add(provider);
            }
        }

        private static List<ProviderInfo> Load()
        {
            try
            {
                string? path = FindCatalogFile();

                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    string json = File.ReadAllText(path);

                    ProviderCatalogFile? catalog =
                        JsonSerializer.Deserialize<ProviderCatalogFile>(
                            json,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                    if (catalog?.Providers != null &&
                        catalog.Providers.Count > 0)
                    {
                        Normalize(catalog.Providers);

                        Console.WriteLine(
                            "[AURA] Provider catalog carregado: " +
                            catalog.Providers.Count + " providers.");

                        return catalog.Providers;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "[AURA] Falha ao carregar config/providers.json: " +
                    ex.Message);
            }

            Console.Error.WriteLine(
                "[AURA] Usando catálogo interno de fallback.");

            return BuildFallback();
        }

        private static string? FindCatalogFile()
        {
            var candidates = new List<string>();

            string current = Directory.GetCurrentDirectory();

            candidates.Add(
                Path.Combine(current, "config", "providers.json"));

            candidates.Add(
                Path.Combine(current, "..", "config", "providers.json"));

            candidates.Add(
                Path.Combine(current, "..", "..", "config", "providers.json"));

            string? baseDir = AppContext.BaseDirectory;

            candidates.Add(
                Path.Combine(baseDir, "config", "providers.json"));

            candidates.Add(
                Path.Combine(baseDir, "..", "..", "..", "..",
                    "config", "providers.json"));

            foreach (string candidate in candidates)
            {
                try
                {
                    string full = Path.GetFullPath(candidate);

                    if (File.Exists(full))
                    {
                        return full;
                    }
                }
                catch
                {
                    // Ignora caminhos inválidos.
                }
            }

            return null;
        }

        private static void Normalize(List<ProviderInfo> providers)
        {
            foreach (ProviderInfo provider in providers)
            {
                if (string.IsNullOrWhiteSpace(provider.Id))
                {
                    provider.Id = NormalizeId(provider.Name);
                }

                if (provider.Models == null)
                {
                    provider.Models = new List<ProviderModel>();
                }

                foreach (ProviderModel model in provider.Models)
                {
                    if (string.IsNullOrWhiteSpace(model.Label))
                    {
                        model.Label = model.Id;
                    }

                    if (string.IsNullOrWhiteSpace(model.Category))
                    {
                        model.Category =
                            model.IsFree ? "Grátis" : "Modelo";
                    }
                }
            }
        }

        private static string NormalizeId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(
                value
                    .Trim()
                    .ToLowerInvariant()
                    .Select(c =>
                        char.IsLetterOrDigit(c) ? c : '-')
                    .ToArray())
                .Trim('-');
        }

        public static ProviderInfo? Find(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ProvidersList.FirstOrDefault();
            }

            string wanted = name.Trim();

            return ProvidersList.FirstOrDefault(p =>
                string.Equals(
                    p.Id,
                    wanted,
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    p.Name,
                    wanted,
                    StringComparison.OrdinalIgnoreCase));
        }

        public static ProviderModel? FindModel(
            string? provider,
            string? model)
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                return null;
            }

            ProviderInfo? info = Find(provider);

            if (info == null)
            {
                return null;
            }

            return info.Models.FirstOrDefault(m =>
                string.Equals(
                    m.Id,
                    model.Trim(),
                    StringComparison.OrdinalIgnoreCase));
        }

        private static List<ProviderInfo> BuildFallback()
        {
            return new List<ProviderInfo>
            {
                new ProviderInfo
                {
                    Id = "openai",
                    Name = "OpenAI",
                    BaseUrl =
                        "https://api.openai.com/v1/chat/completions",
                    NeedsKey = true,
                    KeyEnv = "OPENAI_API_KEY",
                    KeyHint = "OPENAI_API_KEY",
                    Models = new List<ProviderModel>
                    {
                        new ProviderModel
                        {
                            Id = "gpt-5",
                            Label = "GPT-5",
                            Category = "Flagship"
                        },
                        new ProviderModel
                        {
                            Id = "gpt-5-mini",
                            Label = "GPT-5 Mini",
                            Category = "Eficiente"
                        }
                    }
                },

                new ProviderInfo
                {
                    Id = "openrouter",
                    Name = "OpenRouter",
                    BaseUrl =
                        "https://openrouter.ai/api/v1/chat/completions",
                    NeedsKey = true,
                    KeyEnv = "OPENROUTER_API_KEY",
                    KeyHint = "OPENROUTER_API_KEY",
                    Models = new List<ProviderModel>
                    {
                        new ProviderModel
                        {
                            Id = "qwen/qwen-plus",
                            Label = "Qwen Plus",
                            Category = "Razoável"
                        },
                        new ProviderModel
                        {
                            Id = "openrouter/free",
                            Label = "Auto grátis",
                            Category = "Grátis",
                            IsFree = true
                        }
                    }
                },

                new ProviderInfo
                {
                    Id = "ollama",
                    Name = "Ollama (local)",
                    BaseUrl =
                        "http://127.0.0.1:11434/v1/chat/completions",
                    NeedsKey = false,
                    Models = new List<ProviderModel>
                    {
                        new ProviderModel
                        {
                            Id = "qwen2.5-coder:1.5b",
                            Label = "Qwen 2.5 Coder 1.5B",
                            Category = "Local",
                            IsFree = true
                        }
                    }
                }
            };
        }

        private sealed class ProviderCatalogFile
        {
            public int SchemaVersion { get; set; }

            public string Description { get; set; } = string.Empty;

            public List<ProviderInfo> Providers { get; set; } =
                new();
        }
    }
}
