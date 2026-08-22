using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Speech;
using Android.OS;

namespace AURA.Mobile.Speech
{
    /// <summary>
    /// STT via Android SpeechRecognizer (on-device / serviço do sistema).
    /// Deve ser acionado a partir da thread principal.
    /// </summary>
    public sealed class AndroidSpeechRecognitionService : ISpeechRecognitionService, IDisposable
    {
        private readonly object _lock = new();
        private SpeechRecognizer? _recognizer;
        private RecognitionListenerImpl? _listener;
        private TaskCompletionSource<string?>? _pending;
        private bool _disposed;

        public bool IsAvailable
        {
            get
            {
                try
                {
                    var ctx = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
                               ?? (Context?)Android.App.Application.Context;
                    return ctx != null && SpeechRecognizer.IsRecognitionAvailable(ctx);
                }
                catch
                {
                    return false;
                }
            }
        }

        public async Task<string?> ListenAsync(CancellationToken ct = default)
        {
            if (_disposed) return null;

            var status = await Permissions.RequestAsync<Permissions.Microphone>().ConfigureAwait(true);
            if (status != PermissionStatus.Granted)
            {
                AuraLog.Info("STT: permissão de microfone negada");
                return null;
            }

            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null)
            {
                AuraLog.Info("STT: sem Activity");
                return null;
            }

            if (!SpeechRecognizer.IsRecognitionAvailable(activity))
            {
                AuraLog.Info("STT: reconhecimento indisponível neste dispositivo");
                return null;
            }

            Cancel();

            var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_lock) { _pending = tcs; }

            void StartOnMain()
            {
                try
                {
                    _listener = new RecognitionListenerImpl(
                        onResults: text => Complete(text),
                        onError: code =>
                        {
                            AuraLog.Info($"STT error code={code}");
                            Complete(null);
                        });

                    _recognizer = SpeechRecognizer.CreateSpeechRecognizer(activity);
                    _recognizer.SetRecognitionListener(_listener);

                    var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
                    intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
                    intent.PutExtra(RecognizerIntent.ExtraLanguage, "pt-BR");
                    intent.PutExtra(RecognizerIntent.ExtraLanguagePreference, "pt-BR");
                    intent.PutExtra(RecognizerIntent.ExtraPartialResults, false);
                    intent.PutExtra(RecognizerIntent.ExtraMaxResults, 1);
                    intent.PutExtra(RecognizerIntent.ExtraCallingPackage, activity.PackageName);

                    _recognizer.StartListening(intent);
                    AuraLog.Info("STT: escutando…");
                }
                catch (Exception ex)
                {
                    AuraLog.Exception("STT.StartListening", ex);
                    Complete(null);
                }
            }

            if (Looper.MainLooper != null && Looper.MainLooper.IsCurrentThread)
                StartOnMain();
            else
                activity.RunOnUiThread(StartOnMain);

            using (ct.Register(() =>
            {
                Cancel();
                tcs.TrySetResult(null);
            }))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }

        public void Cancel()
        {
            try
            {
                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                void StopOnMain()
                {
                    try { _recognizer?.StopListening(); } catch { }
                    try { _recognizer?.Cancel(); } catch { }
                    try { _recognizer?.Destroy(); } catch { }
                    _recognizer = null;
                    _listener = null;
                }

                if (activity != null && Looper.MainLooper != null && !Looper.MainLooper.IsCurrentThread)
                    activity.RunOnUiThread(StopOnMain);
                else
                    StopOnMain();
            }
            catch { }

            lock (_lock)
            {
                _pending?.TrySetResult(null);
                _pending = null;
            }
        }

        private void Complete(string? text)
        {
            TaskCompletionSource<string?>? tcs;
            lock (_lock)
            {
                tcs = _pending;
                _pending = null;
            }

            try
            {
                _recognizer?.Destroy();
            }
            catch { }
            _recognizer = null;
            _listener = null;

            tcs?.TrySetResult(string.IsNullOrWhiteSpace(text) ? null : text.Trim());
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Cancel();
        }

        private sealed class RecognitionListenerImpl : Java.Lang.Object, IRecognitionListener
        {
            private readonly Action<string?> _onResults;
            private readonly Action<SpeechRecognizerError> _onError;

            public RecognitionListenerImpl(Action<string?> onResults, Action<SpeechRecognizerError> onError)
            {
                _onResults = onResults;
                _onError = onError;
            }

            public void OnReadyForSpeech(Bundle? params) { }
            public void OnBeginningOfSpeech() { }
            public void OnRmsChanged(float rmsdB) { }
            public void OnBufferReceived(byte[]? buffer) { }
            public void OnEndOfSpeech() { }
            public void OnEvent(int eventType, Bundle? params) { }
            public void OnPartialResults(Bundle? partialResults) { }

            public void OnError(SpeechRecognizerError error) => _onError(error);

            public void OnResults(Bundle? results)
            {
                string? text = null;
                try
                {
                    var list = results?.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
                    if (list != null && list.Count > 0)
                        text = list[0];
                }
                catch (Exception ex)
                {
                    AuraLog.Exception("STT.OnResults", ex);
                }
                _onResults(text);
            }
        }
    }
}
