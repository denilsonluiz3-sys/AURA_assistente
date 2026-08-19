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
                        new() { Id = "qwen/qwen-plus", Label = "Qwen Plus", Category = "Razoável", IsFree = false },
                        new() { Id = "qwen/qwen3.7-plus", Label = "Qwen 3.7 Plus", Category = "Flagship", IsFree = false },
                        new() { Id = "qwen/qwen3.5-plus-20260420", Label = "Qwen 3.5 Plus", Category = "Flagship", IsFree = false },
                        new() { Id = "openrouter/free", Label = "Auto (qualquer grátis)", Category = "Grátis", IsFree = true },
                        new() { Id = "openai/gpt-oss-20b:free", Label = "GPT-OSS 20B", Category = "Grátis", IsFree = true },
                        new() { Id = "google/gemma-4-26b-a4b-it:free", Label = "Gemma 4 26B", Category = "Grátis", IsFree = true },
                        new() { Id = "nvidia/nemotron-3-nano-30b-a3b:free", Label = "Nemotron Nano 30B", Category = "Grátis", IsFree = true },
                        new() { Id = "poolside/laguna-s-2.1:free", Label = "Laguna S 2.1", Category = "Grátis", IsFree = true },
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
                        new() { Id = "llama-3.3-70b-versatile", Label = "Llama 3.3 70B", Category = "Grátis", IsFree = true },
                        new() { Id = "llama-3.1-8b-instant", Label = "Llama 3.1 8B", Category = "Grátis", IsFree = true },
                        new() { Id = "llama-3.2-3b-preview", Label = "Llama 3.2 3B", Category = "Grátis", IsFree = true },
                        new() { Id = "qwen-2.5-32b", Label = "Qwen 2.5 32B", Category = "Grátis", IsFree = true },
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
                        new() { Id = "llama-3.3-70b", Label = "Llama 3.3 70B", Category = "Grátis", IsFree = true },
                        new() { Id = "llama-3.1-8b", Label = "Llama 3.1 8B", Category = "Grátis", IsFree = true },
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
                        new() { Id = "gemini-2.5-flash", Label = "Gemini 2.5 Flash", Category = "Grátis", IsFree = true },
                        new() { Id = "gemini-2.5-pro", Label = "Gemini 2.5 Pro", Category = "Pago", IsFree = false },
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
