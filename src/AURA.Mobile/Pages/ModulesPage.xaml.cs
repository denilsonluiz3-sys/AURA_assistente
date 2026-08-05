using AURA.Modules;

namespace AURA.Mobile.Pages;

public partial class ModulesPage : ContentPage
{
    public ModulesPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ModulesView.ItemsSource = await Task.Run(() => ModuleCatalog.GetAll());
    }
}
