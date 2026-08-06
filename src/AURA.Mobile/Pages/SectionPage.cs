namespace AURA.Mobile.Pages;

/// <summary>
/// Página de menu de uma seção do app: lista os acessos e navega
/// (PushAsync) para cada página da seção.
/// </summary>
public sealed class SectionPage : ContentPage
{
    public SectionPage(string title, params (string Label, Page Page)[] items)
    {
        Title = title;
        BackgroundColor = Color.FromArgb("#101014");

        var stack = new VerticalStackLayout { Padding = 20, Spacing = 12 };
        foreach (var (label, page) in items)
        {
            var button = new Button
            {
                Text = label,
                BackgroundColor = Color.FromArgb("#1b1b22"),
                TextColor = Color.FromArgb("#f2f2f5"),
                CornerRadius = 12,
                HeightRequest = 56,
                FontSize = 16,
                HorizontalOptions = LayoutOptions.Fill
            };
            button.Clicked += async (s, e) =>
            {
                try
                {
                    await Navigation.PushAsync(page);
                }
                catch (Exception ex)
                {
                    AuraLog.Exception("SectionPage.Navigate " + label, ex);
                }
            };
            stack.Add(button);
        }

        Content = new ScrollView { Content = stack };
    }
}
