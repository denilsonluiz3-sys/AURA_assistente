using System;
using System.Threading;
using System.Threading.Tasks;
using AURA.Abstractions.Execution;
using AURA.Core.Abstractions;
using AURA.Core.Logging;

namespace AURA.Agents
{
    /// <summary>
    /// Agent de automação: interpreta perguntas simples como comandos shell
    /// e retorna a saída. Exemplos: "liste arquivos em ~/AURA", "mostre a
    /// versão do dotnet". Não usa LLM — executa diretamente via ShellExecutor.
    /// Útil para tarefas determinísticas e scripts de diagnóstico.
    /// </summary>
    public sealed class AutomationAgent : IAgent
    {
        private readonly IToolExecutor _shell;
        private readonly ILogger _logger;

        public string Name => "automation";
        public string Description => "Executa comandos shell determinísticos (sem LLM)";

        public AutomationAgent(IToolExecutor shellExecutor, ILogger logger = null)
        {
            _shell = shellExecutor ?? throw new ArgumentNullException(nameof(shellExecutor));
            _logger = logger ?? new ConsoleLogger();
        }

        public void Start() => _logger.Info("[AutomationAgent] iniciado");

        public void Stop() => _logger.Info("[AutomationAgent] parado");

        public async Task<string> AskAsync(string question, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return "Comando vazio.";
            }

            if (!_shell.IsAvailable())
            {
                return "Shell não disponível neste ambiente.";
            }

            _logger.Info("[AutomationAgent] executando: " + question);

            var request = new ExecutionRequest
            {
                Command = question,
                Timeout = TimeSpan.FromSeconds(30)
            };

            try
            {
                ExecutionResult result = await _shell.ExecuteAsync(request, ct);
                string output = result.CombineOutput();
                return string.IsNullOrWhiteSpace(output)
                    ? "(sem saída, exit " + result.ExitCode + ")"
                    : output.TrimEnd();
            }
            catch (Exception ex)
            {
                _logger.Error("[AutomationAgent] erro: " + ex.Message);
                return "Erro ao executar: " + ex.Message;
            }
        }
    }
}
