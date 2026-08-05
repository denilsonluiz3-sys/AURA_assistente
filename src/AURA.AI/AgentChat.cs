using System.Collections.Generic;

namespace AURA.AI
{
    /// <summary>
    /// Uma mensagem da conversa do agente, no protocolo OpenAI-compatível
    /// (roles: system | user | assistant | tool). Em tool_calls o conteúdo é
    /// null; o resultado da ferramenta volta com ToolCallId apontando o call.
    /// </summary>
    public sealed class AgentMessage
    {
        public string Role { get; set; } = "user";

        public string? Content { get; set; }

        public string? ToolCallId { get; set; }

        public List<AgentToolCall>? ToolCalls { get; set; }
    }

    /// <summary>Uma chamada de ferramenta solicitada pelo modelo.</summary>
    public sealed class AgentToolCall
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        /// <summary>Argumentos em JSON (string) como retornado pelo modelo.</summary>
        public string ArgumentsJson { get; set; } = "{}";
    }

    /// <summary>Resposta de uma rodada de chat com suporte a ferramentas.</summary>
    public sealed class AgentChatResponse
    {
        /// <summary>Texto final (quando a resposta não usa ferramentas).</summary>
        public string? Content { get; set; }

        /// <summary>Chamadas de ferramenta solicitadas (quando houver).</summary>
        public List<AgentToolCall>? ToolCalls { get; set; }

        public string? Error { get; set; }
    }

    /// <summary>Evento emitido pelo AgentSession a cada ferramenta executada (para a UI).</summary>
    public sealed class AgentStep
    {
        public AgentStep(string toolName, string arguments, string result)
        {
            ToolName = toolName;
            Arguments = arguments;
            Result = result;
        }

        public string ToolName { get; }

        public string Arguments { get; }

        public string Result { get; }
    }
}
