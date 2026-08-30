using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AURA.Mobile.Pages;

/// <summary>
/// Superfície transitória para exibir uma capacidade usada pelo Agente.
/// A execução continua pertencendo ao motor existente; esta classe é apenas UI.
/// </summary>
public sealed class AgentCapabilitySurface : ContentView
{
    private readonly Label _title;
    private readonly Label _state;
    private readonly Editor _output;
    private readonly ActivityIndicator _busy;
    private readonly Button _close;
    private readonly Border _frame;

    public event EventHandler? Closed;

    public bool AutoHideOnComplete { get; set; }

    public AgentCapabilitySurface()
    {
        IsVisible = false;
        Padding = new Thickness(12, 8);

        _title = new Label
        {
            FontAttributes = FontAttributes.Bold,
            FontSize = 14
        };

        _state = new Label
        {
            FontSize = 12,
            Opacity = 0.75
        };

        _busy = new ActivityIndicator
        {
            IsVisible = false,
            IsRunning = false,
            WidthRequest = 18,
            HeightRequest = 18,
            VerticalOptions = LayoutOptions.Center
        };

        _output = new Editor
        {
            IsReadOnly = true,
            AutoSize = EditorAutoSizeOption.TextChanges,
            MinimumHeightRequest = 48,
            MaximumHeightRequest = 220,
            FontFamily = DeviceInfo.Platform == DevicePlatform.Android ? "monospace" : null
        };

        _close = new Button
        {
            Text = "Fechar",
            HorizontalOptions = LayoutOptions.End,
            Padding = new Thickness(10, 4)
        };
        _close.Clicked += (_, _) => Hide();

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Add(_title, 0, 0);
        header.Add(_busy, 1, 0);
        header.Add(_close, 2, 0);

        _frame = new Border
        {
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children = { header, _state, _output }
            }
        };

        Content = _frame;
    }

    public void Show(string capability, string state = "executando")
    {
        _title.Text = capability;
        _state.Text = state;
        _output.Text = string.Empty;
        _busy.IsVisible = true;
        _busy.IsRunning = true;
        IsVisible = true;
    }

    public void AppendOutput(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        _output.Text += text;
        _output.CursorPosition = _output.Text.Length;
    }

    public void SetOutput(string text)
    {
        _output.Text = text ?? string.Empty;
        _output.CursorPosition = _output.Text.Length;
    }

    public void Complete(string state = "concluído")
    {
        _state.Text = state;
        _busy.IsRunning = false;
        _busy.IsVisible = false;
        if (AutoHideOnComplete)
            Hide();
    }

    public void Fail(string state = "falhou")
    {
        _state.Text = state;
        _busy.IsRunning = false;
        _busy.IsVisible = false;
    }

    public void Hide()
    {
        _busy.IsRunning = false;
        _busy.IsVisible = false;
        IsVisible = false;
        _output.Text = string.Empty;
        Closed?.Invoke(this, EventArgs.Empty);
    }
}
