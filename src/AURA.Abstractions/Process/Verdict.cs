namespace AURA.Abstractions.Process
{
    /// <summary>Sentença/decisão de mérito do processo.</summary>
    public enum VerdictKind
    {
        Procedente,
        Improcedente,
        Parcial,
        Acordo,
        Liminar,
        Revelia
    }

    public sealed class Verdict
    {
        public VerdictKind Kind { get; set; }

        public string Reason { get; set; } = string.Empty;

        /// <summary>Resumo composto da fase decisória (pesquisa + execução).</summary>
        public string Summary { get; set; } = string.Empty;
    }
}