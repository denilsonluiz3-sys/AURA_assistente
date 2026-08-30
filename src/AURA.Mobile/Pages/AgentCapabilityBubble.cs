using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace AURA.Mobile.Pages;

/// <summary>
/// Inline execution card rendered in the Agent conversation.
/// </summary>
public sealed class AgentCapabilityBubble : Border
{
    private readonly Label _statusLabel;
    private readonly Label _outputLabel;

    public string CorrelationId { get; }

    public AgentCapabilityBubble(string correlationId, string title)
    {
        CorrelationId = correlationId;

        var titleLabel = new Label
        {
            Text = title,
            FontAttributes = FontAttributes.Bold,
            FontSize = 14
        };

        _statusLabel = new Label
        {
            Text = "Executando...",
            FontSize = 12
        };

        _outputLabel = new Label
        {
            FontSize = 12,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var content = new VerticalStackLayout
        {
            Spacing = 4,
            Children = { titleLabel, _statusLabel, _outputLabel }
        };

        Content = content;
        Padding = new Thickness(12, 8);
        Stroke = Colors.Gray;
        StrokeThickness = 1;
        StrokeShape = new RoundRectangle
        {
            CornerRadius = new CornerRadius(12)
        };
    }

    public void SetStatus(string status)
    {
        _statusLabel.Text = status;
    }

    public void AppendOutput(string output)
    {
        if (string.IsNullOrEmpty(output))
            return;

        _outputLabel.Text += output;
    }

    public void Complete(bool success, string? result = null)
    {
        _statusLabel.Text = success ? "Concluído" : "Falhou";
        if (!string.IsNullOrEmpty(result))
            _outputLabel.Text = result;
    }
}
