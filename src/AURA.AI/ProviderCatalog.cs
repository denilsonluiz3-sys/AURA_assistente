using System;
using System.Collections.Generic;
using System.Linq;
using AURA.AI.Providers;
using AURA.AI.UniversalAI;

namespace AURA.AI
{
    /// <summary>Compatibilidade legada para consumidores que ainda usam ProviderModel.</summary>
    public sealed class ProviderModel
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsFree { get; set; }
        public override string ToString() => IsFree ? $"{Label} (grátis)" : Label;
    }

    /// <summary>Compatibilidade legada; os dados reais vêm de UniversalProviderRegistry.</summary>
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

    /// <summary>
    /// Fachada de compatibilidade. Não possui catálogo próprio: UniversalProviderRegistry
    /// é a única fonte de providers e modelos.
    /// </summary>
    public static class ProviderCatalog
    {
        private static List<ProviderInfo> _providers = Map();
        public static List<ProviderInfo> Providers => _providers;

        public static void Reload() => _providers = Map();

        public static IReadOnlyList<IAiProvider> KeyedProbeCandidates() =>
            _providers.Where(p => p.NeedsKey).Cast<IAiProvider>().ToList();

        public static ProviderInfo? Find(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string wanted = name.Trim();
            return _providers.FirstOrDefault(p =>
                string.Equals(p.Id, wanted, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.Name, wanted, StringComparison.OrdinalIgnoreCase));
        }

        public static ProviderModel? FindModel(string? provider, string? model)
        {
            if (string.IsNullOrWhiteSpace(model)) return null;
            return Find(provider)?.Models.FirstOrDefault(m =>
                string.Equals(m.Id, model.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static List<ProviderInfo> Map() => UniversalProviderRegistry.BuiltIns.Select(p => new ProviderInfo
        {
            Id = p.Id,
            Name = p.Name,
            BaseUrl = p.BaseUrl,
            ModelsUrl = p.ModelsUrl,
            NeedsKey = p.RequiresApiKey,
            KeyEnv = p.KeyEnv,
            KeyHint = p.KeyHint,
            DefaultModelId = p.DefaultModelId,
            AuthHeaderName = p.AuthHeader,
            AuthScheme = string.IsNullOrWhiteSpace(p.AuthScheme) ? string.Empty : p.AuthScheme.TrimEnd() + " ",
            ApiFormat = p.Format switch
            {
                UniversalApiFormat.AnthropicMessages => AiApiFormat.AnthropicMessages,
                _ => AiApiFormat.OpenAICompletions
            },
            KeyPrefixesList = p.KeyPrefixes.ToList(),
            Models = p.Models.Select(m => new ProviderModel
            {
                Id = m.Id,
                Label = m.DisplayName,
                Category = m.Category,
                IsFree = m.IsFree
            }).ToList()
        }).ToList();
    }
}
