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
        public override string ToString() => IsFree ? $"{Label} (grátis)" : Label;
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
            ProvidersList.AddRange(Load());
        }

        public static IReadOnlyList<IAiProvider> KeyedProbeCandidates() =>
            ProvidersList.Where(p => p.NeedsKey).Cast<IAiProvider>().ToList();

        private static List<ProviderInfo> Load()
        {
            try
            {
                var list = TryDeserialize(ReadEmbeddedJson());
                if (list != null) return list;
            }
            catch (Exception ex) { Console.Error.WriteLine("[AURA] Embedded providers.json: " + ex.Message); }

            try
            {
                string? path = FindCatalogFile();
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    var list = TryDeserialize(File.ReadAllText(path));
                    if (list != null) return list;
                }
            }
            catch (Exception ex) { Console.Error.WriteLine("[AURA] File providers.json: " + ex.Message); }

            return BuildFallback();
        }

        private static string? ReadEmbeddedJson()
        {
            using Stream? stream = typeof(ProviderCatalog).Assembly.GetManifestResourceStream(EmbeddedName);
            if (stream == null) return null;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static List<ProviderInfo>? TryDeserialize(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            var catalog = JsonSerializer.Deserialize<ProviderCatalogFile>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (catalog?.Providers == null || catalog.Providers.Count == 0) return null;
            Normalize(catalog.Providers);
            return catalog.Providers;
        }

        private static string? FindCatalogFile()
        {
            string current = Directory.GetCurrentDirectory();
            string? baseDir = AppContext.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(current, "config", "providers.json"),
                Path.Combine(current, "..", "config", "providers.json"),
                Path.Combine(current, "..", "..", "config", "providers.json"),
                Path.Combine(baseDir, "config", "providers.json"),
                Path.Combine(baseDir, "..", "..", "..", "..", "config", "providers.json")
            };
            foreach (string candidate in candidates)
            {
                try { string full = Path.GetFullPath(candidate); if (File.Exists(full)) return full; }
                catch { }
            }
            return null;
        }

        private static void Normalize(List<ProviderInfo> providers)
        {
            foreach (ProviderInfo provider in providers)
            {
                if (string.IsNullOrWhiteSpace(provider.Id)) provider.Id = NormalizeId(provider.Name);
                provider.Models ??= new List<ProviderModel>();
                if (string.IsNullOrWhiteSpace(provider.DefaultModelId) && provider.Models.Count > 0)
                    provider.DefaultModelId = provider.Models[0].Id;
                if (string.IsNullOrWhiteSpace(provider.ModelsUrl) && !string.IsNullOrWhiteSpace(provider.BaseUrl))
                    provider.ModelsUrl = provider.BaseUrl
                        .Replace("/chat/completions", "/models", StringComparison.OrdinalIgnoreCase)
                        .Replace("/v1/messages", "/v1/models", StringComparison.OrdinalIgnoreCase);
                if (string.IsNullOrWhiteSpace(provider.AuthHeaderName)) provider.AuthHeaderName = "Authorization";
                provider.AuthScheme ??= "Bearer ";
                provider.KeyPrefixesList ??= new List<string>();
                foreach (ProviderModel model in provider.Models)
                {
                    if (string.IsNullOrWhiteSpace(model.Label)) model.Label = model.Id;
                    if (string.IsNullOrWhiteSpace(model.Category)) model.Category = model.IsFree ? "Grátis" : "Modelo";
                }
            }
        }

        private static string NormalizeId(string? value) => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');

        public static ProviderInfo? Find(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return ProvidersList.FirstOrDefault();
            string wanted = name.Trim();
            return ProvidersList.FirstOrDefault(p =>
                string.Equals(p.Id, wanted, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, wanted, StringComparison.OrdinalIgnoreCase));
        }

        public static ProviderModel? FindModel(string? provider, string? model)
        {
            if (string.IsNullOrWhiteSpace(model)) return null;
            return Find(provider)?.Models.FirstOrDefault(m =>
                string.Equals(m.Id, model.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static List<ProviderInfo> BuildFallback() => new()
        {
            new ProviderInfo
            {
                Id = "ollama", Name = "Ollama (local)",
                BaseUrl = "http://127.0.0.1:11434/v1/chat/completions",
                ModelsUrl = "http://127.0.0.1:11434/v1/models", NeedsKey = false,
                ApiFormat = AiApiFormat.OpenAICompletions
            }
        };

        private sealed class ProviderCatalogFile
        {
            public int SchemaVersion { get; set; }
            public string Description { get; set; } = string.Empty;
            public List<ProviderInfo> Providers { get; set; } = new();
        }
    }
}
