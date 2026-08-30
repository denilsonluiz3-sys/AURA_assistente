using System;
using System.Collections.Generic;

namespace AURA.Abstractions.Execution
{
    /// <summary>
    /// Descreve um comando a ser executado por um IToolExecutor.
    /// <para>
    /// CorrelationId liga explicitamente uma execução à superfície visual que
    /// a acompanha. É opcional para preservar compatibilidade com chamadas
    /// existentes que não precisam de apresentação em tempo real.
    /// </para>
    /// </summary>
    public sealed class ExecutionRequest
    {
        public string Command { get; set; } = string.Empty;

        public List<string> Arguments { get; set; } = new List<string>();

        /// <summary>Diretório de trabalho do processo. Null = diretório atual.</summary>
        public string WorkingDirectory { get; set; }

        /// <summary>Variáveis de ambiente adicionais aplicadas ao processo.</summary>
        public IDictionary<string, string>? EnvironmentVariables { get; set; }

        /// <summary>Timeout da execução. Null = sem timeout.</summary>
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Identificador estável da execução no ProcessRegistry.
        /// Quando preenchido, a saída incremental pode ser roteada sem depender
        /// apenas de cwd, nome do executável ou heurísticas.
        /// </summary>
        public string? CorrelationId { get; set; }
    }
}
