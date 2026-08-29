namespace AURA.Mobile.Controls;

/// <summary>
/// Desenha forma de onda (cima) e espectro de magnitude (baixo) em um GraphicsView.
/// </summary>
public sealed class SpectrumDrawable : IDrawable
{
    public float[] Waveform { get; set; } = Array.Empty<float>();
    public float[] Spectrum { get; set; } = Array.Empty<float>();
    public string Title { get; set; } = "Sinal";
    public string Subtitle { get; set; } = string.Empty;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Color.FromArgb("#0c0c12");
        canvas.FillRectangle(dirtyRect);

        float pad = 12;
        float midY = dirtyRect.Height * 0.48f;
        float waveBottom = midY - 8;
        float specTop = midY + 8;

        // Título
        canvas.FontColor = Color.FromArgb("#c8d0e8");
        canvas.FontSize = 13;
        canvas.DrawString(Title, pad, 4, dirtyRect.Width - pad * 2, 18, HorizontalAlignment.Left, VerticalAlignment.Top);

        if (!string.IsNullOrEmpty(Subtitle))
        {
            canvas.FontColor = Color.FromArgb("#7a8499");
            canvas.FontSize = 11;
            canvas.DrawString(Subtitle, pad, 22, dirtyRect.Width - pad * 2, 16, HorizontalAlignment.Left, VerticalAlignment.Top);
        }

        float waveTop = 42;
        DrawWaveform(canvas, pad, waveTop, dirtyRect.Width - pad * 2, waveBottom - waveTop);
        DrawSpectrum(canvas, pad, specTop, dirtyRect.Width - pad * 2, dirtyRect.Height - specTop - pad);
    }

    private void DrawWaveform(ICanvas canvas, float x, float y, float w, float h)
    {
        canvas.StrokeColor = Color.FromArgb("#1a2030");
        canvas.StrokeSize = 1;
        canvas.DrawRectangle(x, y, w, h);

        // linha zero
        canvas.StrokeColor = Color.FromArgb("#2a3348");
        canvas.DrawLine(x, y + h / 2, x + w, y + h / 2);

        if (Waveform.Length < 2)
        {
            canvas.FontColor = Color.FromArgb("#5a6478");
            canvas.FontSize = 11;
            canvas.DrawString("sem amostras", x, y + h / 2 - 8, w, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
            return;
        }

        float min = Waveform.Min();
        float max = Waveform.Max();
        float range = Math.Max(1e-6f, max - min);

        canvas.StrokeColor = Color.FromArgb("#4da3ff");
        canvas.StrokeSize = 1.5f;
        var path = new PathF();
        for (int i = 0; i < Waveform.Length; i++)
        {
            float px = x + (i / (float)(Waveform.Length - 1)) * w;
            float norm = (Waveform[i] - min) / range;
            float py = y + h - norm * h;
            if (i == 0) path.MoveTo(px, py);
            else path.LineTo(px, py);
        }
        canvas.DrawPath(path);

        canvas.FontColor = Color.FromArgb("#6a7488");
        canvas.FontSize = 10;
        canvas.DrawString("onda", x + 4, y + 2, 40, 12, HorizontalAlignment.Left, VerticalAlignment.Top);
    }

    private void DrawSpectrum(ICanvas canvas, float x, float y, float w, float h)
    {
        canvas.StrokeColor = Color.FromArgb("#1a2030");
        canvas.StrokeSize = 1;
        canvas.DrawRectangle(x, y, w, h);

        if (Spectrum.Length == 0)
        {
            canvas.FontColor = Color.FromArgb("#5a6478");
            canvas.FontSize = 11;
            canvas.DrawString("sem espectro", x, y + h / 2 - 8, w, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
            return;
        }

        float peak = Math.Max(1e-6f, Spectrum.Max());
        int n = Spectrum.Length;
        float barW = Math.Max(1f, w / n - 1f);

        for (int i = 0; i < n; i++)
        {
            float mag = Spectrum[i] / peak;
            float bh = mag * (h - 4);
            float bx = x + i * (w / n);
            float by = y + h - bh - 2;

            // gradiente simples por altura
            canvas.FillColor = mag > 0.7f
                ? Color.FromArgb("#ff6b4a")
                : mag > 0.35f
                    ? Color.FromArgb("#f0c040")
                    : Color.FromArgb("#3dcf8e");
            canvas.FillRectangle(bx, by, barW, bh);
        }

        canvas.FontColor = Color.FromArgb("#6a7488");
        canvas.FontSize = 10;
        canvas.DrawString("espectro", x + 4, y + 2, 50, 12, HorizontalAlignment.Left, VerticalAlignment.Top);
    }
}
