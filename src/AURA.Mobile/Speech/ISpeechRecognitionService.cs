using System.Threading;
using System.Threading.Tasks;

namespace AURA.Mobile.Speech
{
    /// <summary>
    /// Reconhecimento de fala (STT) on-device via motor nativo da plataforma.
    /// </summary>
    public interface ISpeechRecognitionService
    {
        bool IsAvailable { get; }

        /// <summary>
        /// Escuta o microfone e devolve o texto reconhecido (pt-BR preferencial).
        /// Retorna null se cancelado, sem resultado ou indisponível.
        /// </summary>
        Task<string?> ListenAsync(CancellationToken ct = default);

        void Cancel();
    }
}
