using System.Collections.Generic;

namespace AURA.AI
{
    public sealed class ProviderModel
    {
        public string Id { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public bool IsFree { get; init; }

        public override string ToString() =>
            IsFree ? $"{Label} (grátis)" : Label;
    }

    public sealed class ProviderInfo
    {
        public string Name { get; init; } = string.Empty;
        public string BaseUrl { get; init; } = string.Empty;
        public bool NeedsKey { get; init; } = true;
        public string KeyHint { get; init; } = string.Empty;
        public List<ProviderModel> Models { get; init; } = new();
    }

    public static class ProviderCatalog
    {
        private static readonly List<ProviderInfo> ProvidersList = Build();

        public static List<ProviderInfo> Providers => ProvidersList;

        private static List<ProviderInfo> Build()
        {
            return new List<ProviderInfo>
            {
                new ProviderInfo
                {
                    Name = "OpenRouter",
                    BaseUrl = "https://openrouter.ai/api/v1/chat/completions",
                    NeedsKey = true,
                    KeyHint = "sk-or-...",
                    Models = new List<ProviderModel>
                    {
                        new() { Id = "openrouter/free", Label = "Auto (qualquer grátis)", Category = "Grátis", IsFree = true },
                        new() { Id = "openai/gpt-oss-120b", Label = "GPT-OSS 120B", Category = "Flagship", IsFree = false },
                        new() { Id = "openai/gpt-oss-20b:free", Label = "GPT-OSS 20B", Category = "Grátis", IsFree = true },
                        new() { Id = "nvidia/nemotron-3-ultra-550b-a55b:free", Label = "Nemotron 3 Ultra", Category = "Grátis", IsFree = true },
                        new() { Id = "nvidia/nemotron-3-super-120b-a12b:free", Label = "Nemotron 3 Super", Category = "Grátis", IsFree = true },
                        new() { Id = "nvidia/nemotron-3-nano-30b-a3b:free", Label = "Nemotron Nano 30B", Category = "Grátis", IsFree = true },
                        new() { Id = "nvidia/nemotron-nano-9b-v2:free", Label = "Nemotron Nano 9B v2", Category = "Grátis", IsFree = true },
                        new() { Id = "google/gemma-4-31b-it:free", Label = "Gemma 4 31B", Category = "Grátis", IsFree = true },
                        new() { Id = "google/gemma-4-26b-a4b-it:free", Label = "Gemma 4 26B", Category = "Grátis", IsFree = true },
                        new() { Id = "poolside/laguna-s-2.1:free", Label = "Laguna S 2.1", Category = "Grátis", IsFree = true },
                        new() { Id = "poolside/laguna-xs-2.1:free", Label = "Laguna XS 2.1", Category = "Grátis", IsFree = true },
                        new() { Id = "inclusionai/ling-3.0-flash:free", Label = "Ling 3.0 Flash", Category = "Grátis", IsFree = true },
                        new() { Id = "cohere/north-mini-code:free", Label = "North Mini Code", Category = "Grátis", IsFree = true },
                    }
                },
                new ProviderInfo
                {
                    Name = "Groq (grátis)",
                    BaseUrl = "https://api.groq.com/openai/v1/chat/completions",
                    NeedsKey = true,
                    KeyHint = "gsk_...",
                    Models = new List<ProviderModel>
                    {
                        new() { Id = "openai/gpt-oss-120b", Label = "GPT-OSS 120B", Category = "Grátis", IsFree = true },
                        new() { Id = "openai/gpt-oss-20b", Label = "GPT-OSS 20B", Category = "Grátis", IsFree = true },
                        new() { Id = "qwen/qwen3.6-27b", Label = "Qwen 3.6 27B", Category = "Grátis", IsFree = true },
                    }
                },
                new ProviderInfo
                {
                    Name = "Cerebras (grátis)",
                    BaseUrl = "https://api.cerebras.ai/v1/chat/completions",
                    NeedsKey = true,
                    KeyHint = "csk-...",
                    Models = new List<ProviderModel>
                    {
                        new() { Id = "gpt-oss-120b", Label = "GPT-OSS 120B", Category = "Grátis", IsFree = true },
                        new() { Id = "gemma-4-31b", Label = "Gemma 4 31B", Category = "Grátis", IsFree = true },
                    }
                },
                new ProviderInfo
                {
                    Name = "Google Gemini",
                    BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
                    NeedsKey = true,
                    KeyHint = "AIza...",
                    Models = new List<ProviderModel>
                    {
                        new() { Id = "gemini-3.6-flash", Label = "Gemini 3.6 Flash", Category = "Grátis", IsFree = true },
                        new() { Id = "gemini-3-flash-preview", Label = "Gemini 3 Flash", Category = "Grátis", IsFree = true },
                        new() { Id = "gemini-3.1-pro-preview", Label = "Gemini 3.1 Pro", Category = "Pago", IsFree = false },
                    }
                },
                new ProviderInfo
                {
                    Name = "Ollama (local)",
                    BaseUrl = "http://localhost:11434/v1/chat/completions",
                    NeedsKey = false,
                    KeyHint = "deixe vazio",
                    Models = new List<ProviderModel>
                    {
                        new() { Id = "llama3.2", Label = "Llama 3.2", Category = "Local", IsFree = true },
                        new() { Id = "qwen2.5", Label = "Qwen 2.5", Category = "Local", IsFree = true },
                        new() { Id = "mistral", Label = "Mistral", Category = "Local", IsFree = true },
                    }
                },
            };
        }

        public static ProviderInfo? Find(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return ProvidersList[0];
            }

            foreach (var p in ProvidersList)
            {
                if (string.Equals(p.Name, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }

            return ProvidersList[0];
        }
    }
}
