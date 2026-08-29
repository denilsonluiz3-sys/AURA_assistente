namespace AURA.Mobile.Services;

/// <summary>
/// Análise leve de sinais (sem dependências externas).
/// </summary>
public static class SignalAnalysis
{
    /// <summary>Magnitude do espectro (DFT real, bins 0..N/2-1).</summary>
    public static float[] MagnitudeSpectrum(float[] samples, int maxBins = 64)
    {
        if (samples.Length < 4)
            return Array.Empty<float>();

        int n = samples.Length;
        // média removida
        float mean = samples.Average();
        var x = new float[n];
        for (int i = 0; i < n; i++)
            x[i] = samples[i] - mean;

        int bins = Math.Min(maxBins, n / 2);
        var mag = new float[bins];
        for (int k = 0; k < bins; k++)
        {
            double re = 0, im = 0;
            for (int t = 0; t < n; t++)
            {
                double angle = 2.0 * Math.PI * k * t / n;
                re += x[t] * Math.Cos(angle);
                im -= x[t] * Math.Sin(angle);
            }
            mag[k] = (float)Math.Sqrt(re * re + im * im) / n;
        }
        return mag;
    }

    /// <summary>Estimativa de frequência dominante por cruzamentos de zero (Hz).</summary>
    public static float? EstimateHzZeroCrossing(float[] samples, float sampleRateHz)
    {
        if (samples.Length < 8 || sampleRateHz <= 0)
            return null;

        float mean = samples.Average();
        int crossings = 0;
        for (int i = 1; i < samples.Length; i++)
        {
            float a = samples[i - 1] - mean;
            float b = samples[i] - mean;
            if ((a <= 0 && b > 0) || (a >= 0 && b < 0))
                crossings++;
        }

        float durationSec = samples.Length / sampleRateHz;
        if (durationSec <= 0)
            return null;

        // 2 cruzamentos por ciclo
        float hz = crossings / (2f * durationSec);
        if (hz < 1f || hz > sampleRateHz / 2f)
            return null;
        return hz;
    }

    /// <summary>Sugere 50 ou 60 Hz se estiver perto (rede elétrica).</summary>
    public static string? HintMainsHz(float? hz)
    {
        if (hz is null) return null;
        if (Math.Abs(hz.Value - 50f) < 8f) return "~50 Hz (possível rede)";
        if (Math.Abs(hz.Value - 60f) < 8f) return "~60 Hz (possível rede)";
        return $"{hz.Value:F1} Hz";
    }

    /// <summary>Gera seno de demonstração (quando não há sensor).</summary>
    public static float[] DemoSine(int n, float hz, float sampleRate, float amp = 1f)
    {
        var s = new float[n];
        for (int i = 0; i < n; i++)
            s[i] = amp * (float)Math.Sin(2 * Math.PI * hz * i / sampleRate);
        return s;
    }
}
