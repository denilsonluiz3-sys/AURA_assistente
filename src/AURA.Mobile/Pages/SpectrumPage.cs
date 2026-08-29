using AURA.Abstractions;
using AURA.Mobile.Controls;
using AURA.Mobile.Services;

namespace AURA.Mobile.Pages;

/// <summary>
/// Visualização de sinais: forma de onda + espectro.
/// Fontes: magnetômetro (amostragem), demo 50/60 Hz, ou buffer externo.
/// </summary>
public sealed class SpectrumPage : ContentPage
{
    private readonly IAndroidCapabilityService? _android;
    private readonly SpectrumDrawable _drawable = new();
    private readonly GraphicsView _canvas;
    private readonly Label _status;
    private readonly Picker _sourcePicker;
    private IDispatcherTimer? _timer;
    private bool _running;

    public SpectrumPage(IAndroidCapabilityService? android = null)
    {
        _android = android;
        Title = "Espectro";
        BackgroundColor = Color.FromArgb("#0c0c12");

        _sourcePicker = new Picker
        {
            Title = "Fonte",
            ItemsSource = new List<string> { "Magnetômetro", "Demo 50 Hz", "Demo 60 Hz", "Demo áudio (seno)" },
            SelectedIndex = 0,
            TextColor = Color.FromArgb("#e8e8f0"),
            BackgroundColor = Color.FromArgb("#16161f")
        };

        var startBtn = new Button
        {
            Text = "▶ Iniciar",
            BackgroundColor = Color.FromArgb("#2a6df4"),
            TextColor = Colors.White
        };
        startBtn.Clicked += (_, _) => Start();

        var stopBtn = new Button
        {
            Text = "■ Parar",
            BackgroundColor = Color.FromArgb("#3a2030"),
            TextColor = Color.FromArgb("#f0c0c4")
        };
        stopBtn.Clicked += (_, _) => Stop();

        _status = new Label
        {
            Text = "Escolha a fonte e toque Iniciar.\nPython embutido: use executor python no Agente para scripts.",
            TextColor = Color.FromArgb("#9aa3b5"),
            FontSize = 12,
            LineBreakMode = LineBreakMode.WordWrap
        };

        _canvas = new GraphicsView
        {
            Drawable = _drawable,
            HeightRequest = 360,
            BackgroundColor = Color.FromArgb("#0c0c12")
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 12,
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = "Onda · Espectro",
                        FontSize = 18,
                        TextColor = Color.FromArgb("#e8e8f0"),
                        FontAttributes = FontAttributes.Bold
                    },
                    new Label
                    {
                        Text = "Magnetômetro (rede 50/60 Hz), demo seno ou buffer. Áudio real = próxima etapa (microfone).",
                        FontSize = 12,
                        TextColor = Color.FromArgb("#7a8499")
                    },
                    _sourcePicker,
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { startBtn, stopBtn }
                    },
                    _canvas,
                    _status
                }
            }
        };
    }

    protected override void OnDisappearing()
    {
        Stop();
        base.OnDisappearing();
    }

    private void Start()
    {
        Stop();
        _running = true;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(400);
        _timer.Tick += (_, _) => RefreshFrame();
        _timer.Start();
        RefreshFrame();
    }

    private void Stop()
    {
        _running = false;
        if (_timer is not null)
        {
            _timer.Stop();
            _timer = null;
        }
    }

    private void RefreshFrame()
    {
        if (!_running) return;

        try
        {
            string source = _sourcePicker.SelectedItem as string ?? "Demo 50 Hz";
            float[] wave;
            float sampleRate = 100f;
            string title = source;

            if (source.StartsWith("Magnetômetro", StringComparison.OrdinalIgnoreCase))
            {
#if ANDROID
                if (_android is AndroidCapabilityService acs)
                {
                    var samples = acs.SampleMagnetometerMagnitudes(durationMs: 350);
                    if (samples.Length > 4)
                    {
                        wave = samples;
                        sampleRate = samples.Length / 0.35f;
                        title = "Magnetômetro";
                    }
                    else
                    {
                        wave = SignalAnalysis.DemoSine(128, 50, 100);
                        title = "Magnetômetro (fallback demo)";
                        _status.Text = _android.GetMagnetometer() + "\nAmostragem curta vazia — usando demo.";
                    }
                }
                else
                {
                    wave = SignalAnalysis.DemoSine(128, 50, 100);
                    title = "Sem serviço Android";
                }
#else
                wave = SignalAnalysis.DemoSine(128, 50, 100);
                title = "Magnetômetro (só Android)";
#endif
            }
            else if (source.Contains("60"))
            {
                wave = SignalAnalysis.DemoSine(128, 60, 100);
                sampleRate = 100;
            }
            else if (source.Contains("áudio", StringComparison.OrdinalIgnoreCase))
            {
                // mistura de harmônicos como proxy visual de áudio
                wave = new float[160];
                for (int i = 0; i < wave.Length; i++)
                {
                    double t = i / 160.0;
                    wave[i] = (float)(
                        0.6 * Math.Sin(2 * Math.PI * 220 * t) +
                        0.3 * Math.Sin(2 * Math.PI * 440 * t) +
                        0.15 * Math.Sin(2 * Math.PI * 880 * t));
                }
                sampleRate = 160;
            }
            else
            {
                wave = SignalAnalysis.DemoSine(128, 50, 100);
                sampleRate = 100;
            }

            var spectrum = SignalAnalysis.MagnitudeSpectrum(wave, maxBins: 48);
            float? hz = SignalAnalysis.EstimateHzZeroCrossing(wave, sampleRate);
            string? hint = SignalAnalysis.HintMainsHz(hz);

            _drawable.Waveform = wave;
            _drawable.Spectrum = spectrum;
            _drawable.Title = title;
            _drawable.Subtitle = hint is not null
                ? $"pico estimado: {hint} · {wave.Length} amostras"
                : $"{wave.Length} amostras · sr≈{sampleRate:F0} Hz";

            _status.Text = _drawable.Subtitle +
                (source.StartsWith("Magnetômetro") && _android is not null
                    ? "\n" + _android.GetMagnetometer()
                    : "");

            _canvas.Invalidate();
        }
        catch (Exception ex)
        {
            _status.Text = "Erro: " + ex.Message;
        }
    }
}
