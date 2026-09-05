using AURA.AI.UniversalAI;

namespace AURA.Mobile.Diagnostics;

/// <summary>Presets de API incluindo Claude e Gemini (Fase 4).</summary>
public static class AiProviderPresets
{
    public sealed record Preset(
        string Id,
        string Label,
        string BaseUrl,
        string ModelsUrl,
        UniversalApiFormat Format,
        bool RequiresKey,
        string ModelHint);

    public static readonly Preset[] All =
    {
        new("openrouter", "OpenRouter (free)",
            "https://openrouter.ai/api/v1/chat/completions",
            "https://openrouter.ai/api/v1/models",
            UniversalApiFormat.OpenAiCompatible, true, "openrouter/free"),
        new("deepseek", "DeepSeek",
            "https://api.deepseek.com/chat/completions",
            "https://api.deepseek.com/models",
            UniversalApiFormat.OpenAiCompatible, true, "deepseek-v4-flash"),
        new("openai", "OpenAI",
            "https://api.openai.com/v1/chat/completions",
            "https://api.openai.com/v1/models",
            UniversalApiFormat.OpenAiCompatible, true, "gpt-4o-mini"),
        new("anthropic", "Claude (Anthropic)",
            "https://api.anthropic.com/v1/messages",
            "",
            UniversalApiFormat.AnthropicMessages, true, "claude-sonnet-4-20250514"),
        new("gemini", "Gemini",
            "https://generativelanguage.googleapis.com/v1beta",
            "",
            UniversalApiFormat.Gemini, true, "gemini-2.0-flash"),
        new("ollama", "Ollama (local)",
            "http://127.0.0.1:11434/v1/chat/completions",
            "http://127.0.0.1:11434/v1/models",
            UniversalApiFormat.OpenAiCompatible, false, "llama3.2"),
    };

    public static bool Apply(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        var p = All.FirstOrDefault(x =>
            string.Equals(x.Id, id.Trim(), StringComparison.OrdinalIgnoreCase));
        if (p == null)
            return false;

        RuntimeConfig.Provider = p.Id;
        RuntimeConfig.BaseUrlOverride = p.BaseUrl;
        RuntimeConfig.ModelsUrlOverride = p.ModelsUrl;
        RuntimeConfig.ApiFormat = p.Format;
        RuntimeConfig.RequiresApiKey = p.RequiresKey;
        if (!string.IsNullOrWhiteSpace(p.ModelHint))
            RuntimeConfig.Model = p.ModelHint;

        if (p.Format == UniversalApiFormat.AnthropicMessages)
        {
            RuntimeConfig.AuthHeader = "x-api-key";
            RuntimeConfig.AuthScheme = "";
        }
        else
        {
            RuntimeConfig.AuthHeader = "Authorization";
            RuntimeConfig.AuthScheme = "Bearer";
        }

        return true;
    }
}
