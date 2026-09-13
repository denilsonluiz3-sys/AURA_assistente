using AURA.Mobile.Services;
using AURA.Mobile.Speech;
using Microsoft.Maui.Storage;

namespace AURA.Mobile.Pages;

public partial class AgentPage
{
    private readonly List<AuraAttachmentRef> _pendingAttachments = new();

    private AttachmentStore? AttachmentStorage => Handler?.MauiContext?.Services.GetService<AttachmentStore>();
    private ISpeechRecognitionService? SpeechRecognition => Handler?.MauiContext?.Services.GetService<ISpeechRecognitionService>();

    private bool HasPendingAttachments() => _pendingAttachments.Count > 0;

    private void ClearPendingAttachments()
    {
        _pendingAttachments.Clear();
        PendingAttachmentLabel.Text = string.Empty;
        PendingAttachmentLabel.IsVisible = false;
    }

    private string PrepareCommandWithPendingAttachments(string text)
    {
        if (_pendingAttachments.Count == 0) return text;
        string context = AttachmentStore.BuildAgentContext(_pendingAttachments);
        // O anexo só é removido da fila depois que a mensagem foi aceita pelo fluxo
        // do agente. Se houver falha antes disso, ele continua pronto para reenvio.
        return string.IsNullOrWhiteSpace(text) ? context : text + Environment.NewLine + Environment.NewLine + context;
    }

    private async void OnAttachmentClicked(object? sender, EventArgs e)
    {
        try
        {
            var store = AttachmentStorage;
            if (store == null)
            {
                await SafeAlertAsync("Anexo", "O armazenamento de anexos ainda não está disponível.");
                return;
            }

            FileResult? file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Escolha um arquivo para a AURA"
            });
            if (file == null) return;

            AttachButton.IsEnabled = false;
            AuraAttachmentRef item = await store.ImportAsync(file);
            _pendingAttachments.Add(item);
            PendingAttachmentLabel.Text = _pendingAttachments.Count == 1
                ? $"📎 {item.FileName} pronto para análise"
                : $"📎 {_pendingAttachments.Count} anexos prontos para análise";
            PendingAttachmentLabel.IsVisible = true;
            CommandEditor.Focus();
        }
        catch (Exception ex)
        {
            AuraLog.Exception("AgentPage.ImportAttachment", ex);
            await SafeAlertAsync("Importar arquivo", ex.Message);
        }
        finally
        {
            AttachButton.IsEnabled = true;
        }
    }

    private async void OnMicrophoneClicked(object? sender, EventArgs e)
    {
        try
        {
            var recognition = SpeechRecognition;
            if (recognition == null || !recognition.IsAvailable)
            {
                await SafeAlertAsync("Microfone", "O reconhecimento de voz não está disponível neste aparelho.");
                return;
            }

            MicrophoneButton.IsEnabled = false;
            MicrophoneButton.Text = "…";
            string? text = await recognition.ListenAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                string current = CommandEditor.Text?.Trim() ?? string.Empty;
                CommandEditor.Text = string.IsNullOrWhiteSpace(current) ? text : current + " " + text;
                CommandEditor.Focus();
            }
        }
        catch (Exception ex)
        {
            AuraLog.Exception("AgentPage.Microphone", ex);
            await SafeAlertAsync("Microfone", "Não foi possível transcrever a fala.");
        }
        finally
        {
            MicrophoneButton.Text = "🎙";
            MicrophoneButton.IsEnabled = true;
        }
    }
}
