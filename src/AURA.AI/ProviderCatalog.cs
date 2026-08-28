using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

    public sealed class ProviderInfo : IAiProvider
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public bool NeedsKey { get; set; } = true;
        public string KeyHint { get; set; } = string.Empty;
        public string KeyEnv { get; set; } = string.Empty;
        public List<ProviderModel> Models { get; set; } = new();

        public string ModelsUrl { get; set; } = string.Empty;
        public string DefaultModelId { get; set; } = string.Empty;
        public string AuthHeaderName { get; set; } = "Authorization";
        public string AuthScheme { get; set; } = "Bearer ";
        public AiApiFormat ApiFormat { get; set; } = AiApiFormat.OpenAICompletions;
        public List<string> KeyPrefixesList { get; set; } = new();

        IReadOnlyList<string> IAiProvider.KeyPrefixes => KeyPrefixesList;
        public IReadOnlyList<string> KeyPrefixes => KeyPrefixesList;
    }

    public static class ProviderCatalog
    {
        private const string EmbeddedName = "AURA.AI.config.providers.json";
        private static readonly List<ProviderInfo> ProvidersList = Load();

        public static List<ProviderInfo> Providers => ProvidersList;

        public static void Reload()
        {
            ProvidersList.Clear();
            foreach (var provider in Load())
                ProvidersList.Add(provider);
        }

        public static IReadOnlyList<IAiProvider> KeyedProbeCandidates()
        {
            return ProvidersList.Where(p => p.NeedsKey).Cast<IAiProvider>().ToList();
        }

        private static List<ProviderInfo> Load()
        {
            try
            {
                var list = TryDeserialize(ReadEmbeddedJson());
                if (list != null)
                {
                    Console.WriteLine("[AURA] Provider catalog (embedded): " + list.Count + " providers.");
                    return list;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[AURA] Embedded providers.json: " + ex.Message);
            }

            try
            {
                string? path = FindCatalogFile();
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    var list = TryDeserialize(File.ReadAllText(path));
                    if (list != null)
                    {
                        Console.WriteLine("[AURA] Provider catalog (file): " + list.Count + " providers.");
                        return list;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[AURA] File providers.json: " + ex.Message);
            }

            Console.Error.WriteLine("[AURA] Usando catálogo interno de fallback.");
            return BuildFallback();
        }

        private static string? ReadEmbeddedJson()
        {
            var asm = typeof(ProviderCatalog).Assembly;
            using Stream? stream = asm.GetManifestResourceStream(EmbeddedName);
            if (stream == null)
                return null;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static List<ProviderInfo>? TryDeserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            ProviderCatalogFile? catalog = JsonSerializer.Deserialize<ProviderCatalogFile>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (catalog?.Providers == null || catalog.Providers.Count == 0)
                return null;

            Normalize(catalog.Providers);
            return catalog.Providers;
        }

        private static string? FindCatalogFile()
        {
            var candidates = new List<string>();
            string current = Directory.GetCurrentDirectory();
            candidates.Add(Path.Combine(current, "config", "providers.json"));
            candidates.Add(Path.Combine(current, "..", "config", "providers.json"));
            candidates.Add(Path.Combine(current, "..", "..", "config", "providers.json"));
            string? baseDir = AppContext.BaseDirectory;
            candidates.Add(Path.Combine(baseDir, "config", "providers.json"));
            candidates.Add(Path.Combine(baseDir, "..", "..", "..", "..", "config", "providers.json"));

            foreach (string candidate in candidates)
            {
                try
                {
                    string full = Path.GetFullPath(candidate);
                    if (File.Exists(full))
                        return full;
                }
                catch { }
            }

            return null;
        }

        private static void Normalize(List<ProviderInfo> providers)
        {
            foreach (ProviderInfo provider in providers)
            {
                if (string.IsNullOrWhiteSpace(provider.Id))
                    provider.Id = NormalizeId(provider.Name);
                provider.Models ??= new List<ProviderModel>();
                if (string.IsNullOrWhiteSpace(provider.DefaultModelId) && provider.Models.Count > 0)
                    provider.DefaultModelId = provider.Models[0].Id;

                // Ollama: BaseUrl deve ser path OpenAI-compat completo
                if (string.Equals(provider.Id, "ollama", StringComparison.OrdinalIgnoreCase))
                {
                    string b = provider.BaseUrl?.TrimEnd('/') ?? string.Empty;
                    if (!string.IsNullOrEmpty(b) &&
                        !b.Contains("/chat/completions", StringComparison.OrdinalIgnoreCase) &&
                        !b.Contains("/api/chat", StringComparison.OrdinalIgnoreCase))
                    {
                        provider.BaseUrl = b + "/v1/chat/completions";
                    }
                    if (string.IsNullOrWhiteSpace(provider.ModelsUrl))
                        provider.ModelsUrl = b.Contains("/v1/") ? b.Replace("/chat/completions", "/models") : b + "/v1/models";
                }

                if (string.IsNullOrWhiteSpace(provider.ModelsUrl))
                {
                    provider.ModelsUrl = provider.BaseUrl
                        .Replace("/chat/completions", "/models", StringComparison.OrdinalIgnoreCase)
                        .Replace("/v1/messages", "/v1/models", StringComparison.OrdinalIgnoreCase);
                }
                if (string.IsNullOrWhiteSpace(provider.AuthHeaderName))
                    provider.AuthHeaderName = "Authorization";
                if (provider.AuthScheme == null)
                    provider.AuthScheme = "Bearer ";
                provider.KeyPrefixesList ??= new List<string>();
                foreach (ProviderModel model in provider.Models)
                {
                    if (string.IsNullOrWhiteSpace(model.Label))
                        model.Label = model.Id;
                    if (string.IsNullOrWhiteSpace(model.Category))
                        model.Category = model.IsFree ? "Grátis" : "Modelo";
                }
            }
        }

        private static string NormalizeId(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return new string(
                value.Trim().ToLowerInvariant()
                    .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                    .ToArray()).Trim('-');
        }

        public static ProviderInfo? Find(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return ProvidersList.FirstOrDefault();
            string wanted = name.Trim();
            return ProvidersList.FirstOrDefault(p =>
                string.Equals(p.Id, wanted, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, wanted, StringComparison.OrdinalIgnoreCase));
        }

        public static ProviderModel? FindModel(string? provider, string? model)
        {
            if (string.IsNullOrWhiteSpace(model))
                return null;
            ProviderInfo? info = Find(provider);
            if (info == null)
                return null;
            return info.Models.FirstOrDefault(m =>
                string.Equals(m.Id, model.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static List<ProviderInfo> BuildFallback()
        {
            return new List<ProviderInfo>
            {
                new ProviderInfo
                {
                    Id = "openrouter",
                    Name = "OpenRouter",
                    BaseUrl = "https://openrouter.ai/api/v1/chat/completions",
                    ModelsUrl = "https://openrouter.ai/api/v1/models",
                    NeedsKey = true,
                    KeyEnv = "OPENROUTER_API_KEY",
                    KeyHint = "sk-or-…",
                    DefaultModelId = "openrouter/free",
                    AuthHeaderName = "Authorization",
                    AuthScheme = "Bearer ",
                    ApiFormat = AiApiFormat.OpenAICompletions,
                    KeyPrefixesList = new List<string> { "sk-or-" },
                    Models = new List<ProviderModel>
                    {
                        new() { Id = "openrouter/free", Label = "Auto grátis", Category = "Grátis", IsFree = true },
                        new() { Id = "google/gemma-2-9b-it:free", Label = "Gemma 2 9B (free)", Category = "Grátis", IsFree = true }
                    }
                },
                new ProviderInfo
                {
                    Id = "ollama",
                    Name = "Ollama (local)",
                    BaseUrl = "http://127.0.0.1:11435/v1/chat/completions",
                    ModelsUrl = "http://127.0.0.1:11435/v1/models",
                    NeedsKey = false,
                    DefaultModelId = "qwen2:0.5b",
                    AuthHeaderName = "",
                    AuthScheme = "",
                    ApiFormat = AiApiFormat.OpenAICompletions,
                    Models = new List<ProviderModel>
                    {
                        new() { Id = "qwen2:0.5b", Label = "Qwen2 0.5B (local)", Category = "Local", IsFree = true },
                        new() { Id = "qwen2.5-coder:1.5b", Label = "Qwen 2.5 Coder 1.5B", Category = "Local", IsFree = true },
                        new() { Id = "llama3.2:3b", Label = "Llama 3.2 3B", Category = "Local", IsFree = true }
                    }
                }
            };
        }

        private sealed class ProviderCatalogFile
        {
            public int SchemaVersion { get; set; }
            public string Description { get; set; } = string.Empty;
            public List<ProviderInfo> Providers { get; set; } = new();
        }
    }
}
