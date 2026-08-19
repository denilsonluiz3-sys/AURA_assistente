namespace AURA.Abstractions.Process
{
    /// <summary>
    /// Fases de um processo jurídico. O núcleo AURA percorre essas fases
    /// como uma máquina de estados com decisões if/else em cada etapa.
    /// </summary>
    public enum LegalPhase
    {
        PreLitigation = 0,
        Knowledge = 1,
        Decision = 2,
        Appeal = 3,
        Execution = 4,
        Archived = 5
    }

    /// <summary>
    /// Estado vivo de um processo jurídico conduzido pelo núcleo AURA.
    /// Carrega os dados do pedido e as decisões tomadas em cada fase.
    /// </summary>
    public sealed class ProcessState
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Command { get; set; } = string.Empty;

        /// <summary>Classificação do pedido (ex.: cobrança, liminar, execução).</summary>
        public string ActionType { get; set; } = string.Empty;

        public string Debtor { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        /// <summary>Pré-processual: houve acordo/conciliação?</summary>
        public bool Agreement { get; set; }

        /// <summary>Pré-processual: liminar/antecipação deferida?</summary>
        public bool InjunctionGranted { get; set; }

        /// <summary>Conhecimento: réu revel (não contestou)?</summary>
        public bool DefaultJudgment { get; set; }

        /// <summary>Conhecimento: precisa de perícia/apoio técnico?</summary>
        public bool NeedExpert { get; set; }

        public Verdict? Verdict { get; set; }

        /// <summary>Decisão transitada em julgado?</summary>
        public bool IsFinal { get; set; }

        /// <summary>Execução: houve penhora/bloqueio?</summary>
        public bool Seized { get; set; }

        /// <summary>Processo encerrado/arquivado?</summary>
        public bool IsTerminated { get; set; }

        public LegalPhase Phase { get; set; } = LegalPhase.PreLitigation;

        /// <summary>Histórico das decisões tomadas ao longo das fases.</summary>
        public List<string> History { get; } = new();

        public void AddDecision(string decision) => History.Add(decision);
    }
}