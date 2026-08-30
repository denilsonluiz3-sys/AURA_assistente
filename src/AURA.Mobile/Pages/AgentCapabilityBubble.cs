using System.Text;
using Microsoft.Maui.Controls;

namespace AURA.Mobile.Pages;

/// <summary>
/// Inline execution cell shown in the Agent conversation.
/// It represents one capability execution and receives incremental stdout/stderr.
/// </summary>
public sealed class AgentCapabilityBubble : Border
{
    private readonly Label _statusLabel;
    private readonly Label _outputLabel;
    private readonly StringBuilder _output = new();
    private readonly string _correlationId;
    private readonly string _title;
    private bool _completed;

    public AgentCapabilityBubble(string correlationId, string title)
    {
        _correlationId = correlationId ?? string.Empty;
        _title = string.IsNullOrWhiteSpace(title) ? "AURA" : title;

        Padding = new Thickness(12, 8);
        Margin = new Thickness(8, 4);
        StrokeThickness = 1;
        Stroke = Colors.Gray;
        BackgroundColor = Color.FromArgb("#151922");
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) };

        var titleLabel = new Label
        {
            Text = _title,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation
        };

        _statusLabel = new Label
        {
            Text = "executando…",
            FontSize = 11,
            Opacity = 0.75
        };

        _outputLabel = new Label
        {
            FontSize = 12,
            LineBreakMode = LineBreakMode.WordWrap,
            IsVisible = false
        };

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        header.Add(titleLabel, 0, 0);
        header.Add(_statusLabel, 1, 0);

        var layout = new VerticalStackLayout { Spacing = 5 };
        layout.Children.Add(header);
        layout.Children.Add(_outputLabel);

        Content = layout;
        AutomationId = $"capability-{_correlationId}";
    }

    public string CorrelationId => _correlationId;

    public void SetStatus(string status)
    {
        if (_completed)
            return;

        _statusLabel.Text = string.IsNullOrWhiteSpace(status)
            ? "executando…"
            : status;
    }

    public void AppendOutput(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        const int maxOutputLength = 16000;

        _output.Append(text);
        if (_output.Length > maxOutputLength)
        {
            var trimmed = _output.ToString();
            _output.Clear();
            _output.Append(trimmed[^maxOutputLength..]);
        }

        _outputLabel.Text = _output.ToString();
        _outputLabel.IsVisible = true;
    }

    public void Complete(bool success, string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(message))
            AppendOutput(message);

        _completed = true;
        _statusLabel.Text = success ? "concluído" : "falhou";
    }
}
