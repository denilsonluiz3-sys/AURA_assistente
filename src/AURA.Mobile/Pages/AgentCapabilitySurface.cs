using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace AURA.Mobile.Pages;

/// <summary>
/// Superfície transitória para exibir uma capacidade usada pelo Agente.
/// Não executa ferramentas: apenas apresenta estado/saída do mesmo motor que
/// o Agente já utiliza. Pode ser aberta durante uma operação e fechada depois.
/// </summary>
public sealed class AgentCapabilitySurface : ContentView
{
    private readonly Label _title;
    private readonly Label _state;
    private readonly Editor _output;
    private readonly Button _close;

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
            HorizontalOptions = LayoutOptions.End
        };
        _close.Clicked += (_, _) => Hide();

        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Add(_title, 0, 0);
        header.Add(_close, 1, 0);

        var frame = new Border
        {
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children = { header, _state, _output }
            }
        };

        Content = frame;
    }

    public void Show(string capability, string state = "executando")
    {
        _title.Text = capability;
        _state.Text = state;
        _output.Text = string.Empty;
        IsVisible = true;
    }

    public void AppendOutput(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        _output.Text += text;
    }

    public void Complete(string state = "concluído")
    {
        _state.Text = state;
    }

    public void Fail(string state = "falhou")
    {
        _state.Text = state;
    }

    public void Hide()
    {
        IsVisible = false;
        _output.Text = string.Empty;
    }
}
