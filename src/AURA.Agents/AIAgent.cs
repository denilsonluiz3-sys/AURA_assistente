using System;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Abstractions;
using AURA.Core.Logging;
using AURA.Core.Runtime;

namespace AURA.Agents
{
    /// <summary>
    /// Wrapper IAgent sobre AgentManager: delega AskAsync a um assistente
    /// externo (aichat, termux-ai ou opencode) rodando numa célula AURA.
    /// É o ponto de entrada unificado para LLMs externos dentro do ecossistema
    /// de IAgent — sem acoplamento direto ao cliente HTTP ou à API key.
    /// </summary>
    public sealed class AIAgent : IAgent
    {
        private readonly AgentManager _manager;
        private readonly SimulationRuntime _runtime;
        private readonly string _assistantName;
        private readonly ILogger _logger;

        public string Name => "ai:" + _assistantName;
        public string Description => "Delega ao assistente LLM externo: " + _assistantName;

        public AIAgent(AgentManager manager, SimulationRuntime runtime,
            string assistantName = "aichat", ILogger logger = null)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            _assistantName = assistantName ?? "aichat";
            _logger = logger ?? new ConsoleLogger();
        }

        public void Start() => _logger.Info("[AIAgent:" + _assistantName + "] iniciado");

        public void Stop() => _logger.Info("[AIAgent:" + _assistantName + "] parado");

        public async Task<string> AskAsync(string question, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return "Pergunta vazia.";
            }

            _logger.Info("[AIAgent:" + _assistantName + "] perguntando: " + question);

            try
            {
                return await _manager.AskAsync(_runtime, question, _assistantName);
            }
            catch (Exception ex)
            {
                _logger.Error("[AIAgent:" + _assistantName + "] erro: " + ex.Message);
                return "Erro ao consultar " + _assistantName + ": " + ex.Message;
            }
        }
    }
}
