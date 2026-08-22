using System;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using AURA.Mobile.Speech;
using Microsoft.Extensions.DependencyInjection;
using Button = Android.Widget.Button;
using Color = Android.Graphics.Color;

namespace AURA.Mobile.Platforms.Android
{
    /// <summary>
    /// FAB de voz (STT + TTS). Canto superior direito.
    /// Toque: escuta comando → orquestrador; se já ativo, cancela.
    /// </summary>
    public static class VoiceFloatingButton
    {
        private static Button? _fab;
        private static bool _attached;
        private static VoiceAssistantService? _subscribed;

        public static void Attach(Activity activity)
        {
            if (_attached)
                return;
            _attached = true;

            if (activity?.Window == null || activity.Window.DecorView is not ViewGroup decor)
                return;

            float density = activity.Resources?.DisplayMetrics?.Density ?? 1f;
            int Dp(int v) => (int)(v * density + 0.5f);

            var fab = new Button(activity)
            {
                Text = "🎤",
                TextSize = 18,
            };
            fab.SetAllCaps(false);
            fab.SetBackgroundDrawable(CreateCircle(Color.ParseColor("#4f8aff"), Color.White));
            fab.SetTextColor(Color.White);
            fab.Gravity = GravityFlags.Center;
            fab.Elevation = Dp(6);

            int size = Dp(48);
            var lp = new FrameLayout.LayoutParams(size, size)
            {
                Gravity = GravityFlags.Top | GravityFlags.End,
                RightMargin = Dp(14),
                TopMargin = Dp(52),
            };
            fab.LayoutParameters = lp;
            fab.Click += OnFabClicked;
            decor.AddView(fab);
            _fab = fab;

            TrySubscribeListening();

            AuraLog.Info("VoiceFloatingButton.Attach OK (STT+TTS, top-end)");
        }

        public static void Detach()
        {
            if (_subscribed != null)
            {
                _subscribed.ListeningChanged -= OnListeningChanged;
                _subscribed = null;
            }

            if (_fab?.Parent is ViewGroup parent)
            {
                _fab.Click -= OnFabClicked;
                parent.RemoveView(_fab);
            }

            _fab = null;
            _attached = false;
        }

        private static void TrySubscribeListening()
        {
            try
            {
                var services = IPlatformApplication.Current?.Services;
                var voice = services?.GetService<VoiceAssistantService>();
                if (voice == null || ReferenceEquals(_subscribed, voice)) return;
                if (_subscribed != null)
                    _subscribed.ListeningChanged -= OnListeningChanged;
                _subscribed = voice;
                voice.ListeningChanged += OnListeningChanged;
            }
            catch { }
        }

        private static void OnListeningChanged(bool listening)
        {
            var fab = _fab;
            if (fab == null) return;
            try
            {
                fab.Post(() =>
                {
                    fab.Text = listening ? "⏺" : "🎤";
                    fab.SetBackgroundDrawable(CreateCircle(
                        Color.ParseColor(listening ? "#e05560" : "#4f8aff"),
                        Color.White));
                });
            }
            catch { }
        }

        private static async void OnFabClicked(object? sender, EventArgs e)
        {
            try
            {
                TrySubscribeListening();
                var services = IPlatformApplication.Current?.Services;
                var voice = services?.GetService<VoiceAssistantService>();
                if (voice == null)
                    return;
                await voice.ToggleAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AuraLog.Exception("VoiceFloatingButton.Click", ex);
            }
        }

        private static GradientDrawable CreateCircle(Color fill, Color border)
        {
            var d = new GradientDrawable();
            d.SetShape(ShapeType.Oval);
            d.SetColor(fill);
            d.SetStroke(3, border);
            return d;
        }
    }
}
