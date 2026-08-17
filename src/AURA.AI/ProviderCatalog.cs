using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AURA.AI.Providers;

namespace AURA.AI
{
    public sealed class ProviderModel
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsFree { get; set; }

        public override string ToString() =>
            IsFree ? $"{Label} (grátis)" : Label;
    }

    /// <summary>
    /// Descrição de um provedor LLM. Implementa <see cref="IAiProvider"/>
    /// para ser usado pelo <see cref="ApiKeyProviderResolver"/>.
    /// </summary>
    public sealed class ProviderInfo : IAiProvider
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public bool NeedsKey { get; set; } = true;
        public string KeyHint { get; set; } = string.Empty;
        public string KeyEnv { get; set; } = string.Empty;
        public List<ProviderModel> Models { get; set; } = new();

        // ── IAiProvider ──────────────────────────────────────────────
        public string ModelsUrl { get; set; } = string.Empty;
        public string DefaultModelId { get; set; } = string.Empty;
        public string AuthHeaderName { get; set; } = "Authorization";
        public string AuthScheme { get; set; } = "Bearer ";
        public AiApiFormat ApiFormat { get; set; } = AiApiFormat.OpenAICompletions;
        public List<string> KeyPrefixesList { get; set; } = new();

        IReadOnlyList<string> IAiProvider.KeyPrefixes => KeyPrefixesList;

        /// <summary>Atalho para código que não usa a interface.</summary>
        public IReadOnlyList<string> KeyPrefixes => KeyPrefixesList;
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

        /// <summary>
        /// Provedores que exigem chave e podem ser testados via probe (GET models).
        /// </summary>
        public static IReadOnlyList<IAiProvider> KeyedProbeCandidates()
        {
            return ProvidersList
                .Where(p => p.NeedsKey)
                .Cast<IAiProvider>()
                .ToList();
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

                if (string.IsNullOrWhiteSpace(provider.DefaultModelId) &&
                    provider.Models.Count > 0)
                {
                    provider.DefaultModelId = provider.Models[0].Id;
                }

                if (string.IsNullOrWhiteSpace(provider.ModelsUrl))
                {
                    // Heurística: /chat/completions → /models
                    provider.ModelsUrl = provider.BaseUrl
                        .Replace("/chat/completions", "/models", StringComparison.OrdinalIgnoreCase)
                        .Replace("/v1/messages", "/v1/models", StringComparison.OrdinalIgnoreCase);
                }

                if (string.IsNullOrWhiteSpace(provider.AuthHeaderName))
                {
                    provider.AuthHeaderName = "Authorization";
                }

                if (string.IsNullOrWhiteSpace(provider.AuthScheme))
                {
                    provider.AuthScheme = "Bearer ";
                }

                if (provider.KeyPrefixesList == null)
                {
                    provider.KeyPrefixesList = new List<string>();
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
                    ModelsUrl =
                        "https://api.openai.com/v1/models",
                    NeedsKey = true,
                    KeyEnv = "OPENAI_API_KEY",
                    KeyHint = "OPENAI_API_KEY",
                    DefaultModelId = "gpt-5-mini",
                    AuthHeaderName = "Authorization",
                    AuthScheme = "Bearer ",
                    ApiFormat = AiApiFormat.OpenAICompletions,
                    KeyPrefixesList = new List<string> { "sk-" },
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
                    ModelsUrl =
                        "https://openrouter.ai/api/v1/models",
                    NeedsKey = true,
                    KeyEnv = "OPENROUTER_API_KEY",
                    KeyHint = "OPENROUTER_API_KEY",
                    DefaultModelId = "qwen/qwen-plus",
                    AuthHeaderName = "Authorization",
                    AuthScheme = "Bearer ",
                    ApiFormat = AiApiFormat.OpenAICompletions,
                    KeyPrefixesList = new List<string> { "sk-or-" },
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
                    ModelsUrl =
                        "http://127.0.0.1:11434/v1/models",
                    NeedsKey = false,
                    DefaultModelId = "qwen2.5-coder:1.5b",
                    AuthHeaderName = "Authorization",
                    AuthScheme = "Bearer ",
                    ApiFormat = AiApiFormat.OpenAICompletions,
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
                },

                new ProviderInfo
                {
                    Id = "groq",
                    Name = "Groq (grátis)",
                    BaseUrl =
                        "https://api.groq.com/openai/v1/chat/completions",
                    ModelsUrl =
                        "https://api.groq.com/openai/v1/models",
                    NeedsKey = true,
                    KeyEnv = "GROQ_API_KEY",
                    KeyHint = "GROQ_API_KEY",
                    DefaultModelId = "llama-3.3-70b-versatile",
                    AuthHeaderName = "Authorization",
                    AuthScheme = "Bearer ",
                    ApiFormat = AiApiFormat.OpenAICompletions,
                    KeyPrefixesList = new List<string> { "gsk_" },
                    Models = new List<ProviderModel>
                    {
                        new ProviderModel
                        {
                            Id = "llama-3.3-70b-versatile",
                            Label = "Llama 3.3 70B",
                            Category = "Grátis",
                            IsFree = true
                        },
                        new ProviderModel
                        {
                            Id = "llama-3.1-8b-instant",
                            Label = "Llama 3.1 8B",
                            Category = "Grátis",
                            IsFree = true
                        }
                    }
                },

                new ProviderInfo
                {
                    Id = "gemini",
                    Name = "Google Gemini",
                    BaseUrl =
                        "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
                    ModelsUrl =
                        "https://generativelanguage.googleapis.com/v1beta/openai/models",
                    NeedsKey = true,
                    KeyEnv = "GEMINI_API_KEY",
                    KeyHint = "GEMINI_API_KEY",
                    DefaultModelId = "gemini-2.5-flash",
                    AuthHeaderName = "Authorization",
                    AuthScheme = "Bearer ",
                    ApiFormat = AiApiFormat.OpenAICompletions,
                    KeyPrefixesList = new List<string> { "AIza", "AQ." },
                    Models = new List<ProviderModel>
                    {
                        new ProviderModel
                        {
                            Id = "gemini-2.5-flash",
                            Label = "Gemini 2.5 Flash",
                            Category = "Grátis",
                            IsFree = true
                        },
                        new ProviderModel
                        {
                            Id = "gemini-2.5-flash-lite",
                            Label = "Gemini 2.5 Flash-Lite",
                            Category = "Grátis",
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
