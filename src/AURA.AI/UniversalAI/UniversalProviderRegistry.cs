namespace AURA.AI.UniversalAI;

public static class UniversalProviderRegistry
{
    public static IReadOnlyList<UniversalProvider> BuiltIns { get; } = new[]
    {
        new UniversalProvider("openai", "OpenAI", "https://api.openai.com/v1/chat/completions", "https://api.openai.com/v1/models"),
        new UniversalProvider("openrouter", "OpenRouter", "https://openrouter.ai/api/v1/chat/completions", "https://openrouter.ai/api/v1/models"),
        new UniversalProvider("deepseek", "DeepSeek", "https://api.deepseek.com/chat/completions", "https://api.deepseek.com/models"),
        new UniversalProvider("groq", "Groq", "https://api.groq.com/openai/v1/chat/completions", "https://api.groq.com/openai/v1/models"),
        new UniversalProvider("mistral", "Mistral", "https://api.mistral.ai/v1/chat/completions", "https://api.mistral.ai/v1/models"),
        new UniversalProvider("together", "Together AI", "https://api.together.xyz/v1/chat/completions", "https://api.together.xyz/v1/models"),
        new UniversalProvider("fireworks", "Fireworks AI", "https://api.fireworks.ai/inference/v1/chat/completions", "https://api.fireworks.ai/inference/v1/models"),
        new UniversalProvider("cerebras", "Cerebras", "https://api.cerebras.ai/v1/chat/completions", "https://api.cerebras.ai/v1/models"),
        new UniversalProvider("xai", "xAI / Grok", "https://api.x.ai/v1/chat/completions", "https://api.x.ai/v1/models"),
        new UniversalProvider("ollama", "Ollama", "http://127.0.0.1:11434/v1/chat/completions", "http://127.0.0.1:11434/v1/models", UniversalApiFormat.OpenAiCompatible, "", ""),
        new UniversalProvider("anthropic", "Anthropic", "https://api.anthropic.com/v1/messages", "https://api.anthropic.com/v1/models", UniversalApiFormat.AnthropicMessages, "x-api-key", "")
    };

    public static UniversalProvider Custom(string baseUrl, string? modelsUrl = null, string name = "Custom OpenAI-compatible")
    {
        var baseValue = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseValue)) throw new ArgumentException("Base URL obrigatória.", nameof(baseUrl));
        var chat = baseValue.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ? baseValue : baseValue + "/chat/completions";
        var models = string.IsNullOrWhiteSpace(modelsUrl) ? baseValue + "/models" : modelsUrl.Trim();
        return new UniversalProvider("custom", name.Trim(), chat, models);
    }
}
