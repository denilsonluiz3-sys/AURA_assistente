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
    /// FAB de voz. Fica no canto SUPERIOR direito para não cobrir
    /// o botão Enviar do Chat nem a bottom bar.
    /// </summary>
    public static class VoiceFloatingButton
    {
        private static Button? _fab;
        private static bool _attached;

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
                Text = "🔊",
                TextSize = 18,
            };
            fab.SetAllCaps(false);
            fab.SetBackgroundDrawable(CreateCircle(Color.ParseColor("#4f8aff"), Color.White));
            fab.SetTextColor(Color.White);
            fab.Gravity = GravityFlags.Center;
            fab.Elevation = Dp(6);

            int size = Dp(48);
            // TOPO direito — longe do Editor/Enviar e da tab bar
            var lp = new FrameLayout.LayoutParams(size, size)
            {
                Gravity = GravityFlags.Top | GravityFlags.End,
                RightMargin = Dp(14),
                TopMargin = Dp(52), // abaixo da status/action bar
            };
            fab.LayoutParameters = lp;
            fab.Click += OnFabClicked;
            decor.AddView(fab);
            _fab = fab;

            AuraLog.Info("VoiceFloatingButton.Attach OK (top-end)");
        }

        public static void Detach()
        {
            if (_fab?.Parent is ViewGroup parent)
            {
                _fab.Click -= OnFabClicked;
                parent.RemoveView(_fab);
            }

            _fab = null;
            _attached = false;
        }

        private static async void OnFabClicked(object? sender, EventArgs e)
        {
            try
            {
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
