namespace AURA.AI.Providers
{
    /// <summary>Formato de API do provedor.</summary>
    public enum AiApiFormat
    {
        /// <summary>POST /chat/completions com payload da OpenAI (OpenRouter, Groq, DeepSeek, Mistral, xAI, Together, Fireworks, Cohere, etc.).</summary>
        OpenAICompletions,

        /// <summary>POST /v1/messages com payload Anthropic (Claude). Header x-api-key, max_tokens + system + messages + tool_use.</summary>
        AnthropicMessages
    }
}
