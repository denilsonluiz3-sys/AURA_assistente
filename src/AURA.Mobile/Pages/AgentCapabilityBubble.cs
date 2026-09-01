using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace AURA.Mobile.Pages;

/// <summary>
/// Célula visual de uma execução específica, identificada por CorrelationId.
/// Permite múltiplas execuções independentes no fluxo da conversa.
/// </summary>
public sealed class AgentCapabilityBubble : ContentView
{
    public string CorrelationId { get; }
    public bool IsFinished { get; private set; }

    private readonly Label _title;
    private readonly Label _status;
    private readonly Editor _output;

    public AgentCapabilityBubble(
        string correlationId,
        string title,
        string status = "Executando...")
    {
        CorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString("N")
            : correlationId;

        _title = new Label
        {
            Text = title,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#8fb3ff")
        };

        _status = new Label
        {
            Text = status,
            FontSize = 10,
            Opacity = 0.85,
            TextColor = Color.FromArgb("#8a9bb8")
        };

        _output = new Editor
        {
            IsReadOnly = true,
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 0,
            MaximumHeightRequest = 220,
            FontSize = 11,
            BackgroundColor = Colors.Transparent,
            IsVisible = false,
            Text = string.Empty
        };

        var card = new Border
        {
            BackgroundColor = Color.FromArgb("#0f1420"),
            Stroke = Color.FromArgb("#242438"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 2),
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(10)
            },
            HorizontalOptions = LayoutOptions.Fill,
            Content = new VerticalStackLayout
            {
                Spacing = 3,
                Children =
                {
                    _title,
                    _status,
                    _output
                }
            }
        };

        Content = card;
    }

    public void SetTitle(string title)
    {
        if (IsFinished || string.IsNullOrWhiteSpace(title))
            return;

        _title.Text = title;
    }

    public void SetStatus(string status)
    {
        if (IsFinished || string.IsNullOrWhiteSpace(status))
            return;

        _status.Text = status;
    }

    public void AppendOutput(string text)
    {
        if (IsFinished || string.IsNullOrEmpty(text))
            return;

        _output.IsVisible = true;
        _output.Text += text;
    }

    public void Complete(bool success, string? finalMessage = null)
    {
        if (IsFinished)
            return;

        IsFinished = true;

        if (!string.IsNullOrEmpty(finalMessage))
        {
            _output.IsVisible = true;
            _output.Text = finalMessage;
        }

        _status.Text = success ? "Concluído" : "Falhou";
        _status.TextColor = success
            ? Color.FromArgb("#7fd99a")
            : Color.FromArgb("#f0c0c4");
    }
}
