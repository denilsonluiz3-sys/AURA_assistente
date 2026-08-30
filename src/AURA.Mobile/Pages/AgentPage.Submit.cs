using System.Threading;
using AURA.AI;
using AURA.Mobile.Diagnostics;
using AURA.Mobile.Services;

namespace AURA.Mobile.Pages;

/// <summary>
/// Submit único: Editor.Completed e botão ▶ passam por aqui.
/// Evita duas respostas finais para o mesmo pedido (teclado Android).
/// </summary>
public partial class AgentPage
{
    private int _runGate;
    private long _lastSubmitTicks;
    private string? _sessionFingerprint;
    private int _activeRequestId;
    private static int _requestSeq;

    private async Task TrySubmitAsync(string source)
    {
        long now = Environment.TickCount64;
        if (_lastSubmitTicks != 0 && now - _lastSubmitTicks < 900)
        {
            AuraLog.Info($"AgentPage.Submit ignorado (debounce source={source})");
            return;
        }

        if (Interlocked.CompareExchange(ref _runGate, 1, 0) != 0)
        {
            AuraLog.Info($"AgentPage.Submit ignorado (em voo source={source})");
            return;
        }

        _lastSubmitTicks = now;
        int requestId = Interlocked.Increment(ref _requestSeq);
        _activeRequestId = requestId;
        AuraLog.Info($"AgentPage.Submit BEGIN id={requestId} source={source}");

        try
        {
            await RunAgentRequestAsync(requestId).ConfigureAwait(true);
        }
        finally
        {
            if (_activeRequestId == requestId)
                _activeRequestId = 0;
            Interlocked.Exchange(ref _runGate, 0);
            AuraLog.Info($"AgentPage.Submit END id={requestId}");

            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        RunButton.IsEnabled = true;
                        BusyIndicator.IsRunning = false;
                        BusyIndicator.IsVisible = false;
                        CommandEditor.IsEnabled = true;
                    }
                    catch { /* ignore */ }
                });
            }
            catch { /* ignore */ }
        }
    }

    private bool IsCurrentRequest(int requestId) =>
        requestId != 0 && requestId == _activeRequestId;

    private void EnsureSessionForCurrentProvider()
    {
        string fp = ($"{_client.Options.Provider}|{_client.Options.Model}|{_client.Options.BaseUrl}|{_client.Options.ApiKey?.Length ?? 0}").ToLowerInvariant();
        if (_session != null && string.Equals(_sessionFingerprint, fp, StringComparison.Ordinal))
            return;

        _session = null;
        _sessionFingerprint = fp;
        EnsureSession();
    }

    /// <summary>Corpo único da execução do agente (uma resposta final por requestId).</summary>
    private async Task RunAgentRequestAsync(int requestId)
    {
        try { CommandEditor.Unfocus(); } catch { /* ignore */ }

        string text = CommandEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            await SafeAlertAsync("Agente", "Digite uma instrução antes de enviar.");
            return;
        }

        bool wasContinue = IsContinueCommand(text);
        string resolved = ExpandContinueCommand(text);
        string? processId = null;
        _runShellCommands.Clear();
        if (!wasContinue)
            _lastUserGoal = text;

        if (_webMode)
        {
            _webMode = false;
            ApplyModeUi();
        }

        try
        {
            RememberCommand(wasContinue ? resolved : text);
            RuntimeConfig.Apply(_client);

            RunButton.IsEnabled = false;
            CommandEditor.IsEnabled = false;
            BusyIndicator.IsRunning = true;
            BusyIndicator.IsVisible = true;

            await AppendBubbleAsync(wasContinue ? resolved : text, user: true);
            CommandEditor.Text = string.Empty;

            var process = _processes.Begin(Shorten(resolved, 40), "Assistente", "Entendendo solicitação");
            processId = process.Id;
            _activeProcessId = process.Id;

            string playbookQuery = wasContinue ? resolved : text;
            bool isRepeat = !wasContinue && _lastUserQuery != null
                && string.Equals(text, _lastUserQuery, StringComparison.OrdinalIgnoreCase);

            if (isRepeat)
                _lastUserQuery = text;

            string? local = isRepeat ? null : _playbook?.TryResolveWithoutLlm(playbookQuery);
            if (!string.IsNullOrWhiteSpace(local))
            {
                if (!IsCurrentRequest(requestId)) return;
                _processes.Update(process.Id, "Playbook", "Ação local", 0.5);
                await DeliverAnswerAsync(local, process.Id, "Memória procedural", requestId);
                return;
            }

            if (!isRepeat && ShouldOrchestrate(resolved))
            {
                if (!IsCurrentRequest(requestId)) return;
                _processes.Update(process.Id, "Planejando", "Orquestrador", 0.15);
                string answer = await _orchestrator.ExecuteAsync(resolved);
                if (!IsCurrentRequest(requestId)) return;
                _playbook?.RememberFromRun(resolved, _runShellCommands, answer);
                await DeliverAnswerAsync(answer, process.Id, "OK", requestId);
                return;
            }

            _processes.Update(process.Id, "Executando", "Processando", 0.1);
            string answerFromAgent;

            bool hasCloudKey = !string.IsNullOrWhiteSpace(RuntimeConfig.ApiKey)
                || !string.IsNullOrWhiteSpace(_client.Options.ApiKey);
            bool hasLocalLlm = HasLocalLlmWithoutKey();

            if (!hasCloudKey && !hasLocalLlm)
            {
                await AppendBubbleAsync("Sem LLM local/chave — tentando web…", user: false, isTool: true);
                answerFromAgent = await WebSearchAnswer.SearchWithRefinementAsync(resolved);
            }
            else
            {
                string? readyError = RuntimeConfig.EnsureReadyForRequest(_client);
                if (readyError != null)
                {
                    if (!IsCurrentRequest(requestId)) return;
                    _processes.Fail(process.Id, readyError);
                    await AppendBubbleAsync(readyError, user: false, isError: true);
                    return;
                }

                // Reutiliza sessão se provedor/modelo não mudou (continuidade)
                EnsureSessionForCurrentProvider();
                answerFromAgent = await _session!.RunAsync(resolved);
            }

            if (!IsCurrentRequest(requestId)) return;

            _playbook?.RememberFromRun(resolved, _runShellCommands, answerFromAgent);
            await DeliverAnswerAsync(answerFromAgent, process.Id, "Resultado entregue", requestId);
            _lastUserQuery = text;

            if (ProjectAccessService.IsLinked && !ProjectAccessService.IsDirect)
            {
                int synced = await ProjectAccessService.SyncBackAsync();
                if (IsCurrentRequest(requestId))
                    await AppendBubbleAsync($"↥ Sync: {synced} arquivo(s).", user: false, isTool: true);
            }
        }
        catch (AgentLlmException ex)
        {
            if (!IsCurrentRequest(requestId)) return;
            string userMsg = FriendlyLlmError(ex);
            if (!string.IsNullOrEmpty(processId))
                _processes.Fail(processId, userMsg);
            await AppendBubbleAsync("Erro: " + userMsg, user: false, isError: true);
            AuraLog.Exception("AgentPage.RunAgentRequest", ex);
        }
        catch (Exception ex)
        {
            if (!IsCurrentRequest(requestId)) return;
            string userMsg = FriendlyLlmError(ex);
            if (!string.IsNullOrEmpty(processId))
                _processes.Fail(processId, userMsg);
            await AppendBubbleAsync("Erro: " + userMsg, user: false, isError: true);
            AuraLog.Exception("AgentPage.RunAgentRequest", ex);
        }
        finally
        {
            if (_activeProcessId == processId) _activeProcessId = null;
        }
    }
}
