using System;
using System.Linq;
using AURA.Core.Configuration;
using AURA.Modules;

namespace AURA.Mobile.Pages;

public partial class ModulesPage : ContentPage
{
    private readonly AuraConfiguration _settings;
    private readonly ModulesConfiguration _modules;

    public ModulesPage(AuraConfiguration settings, ModulesConfiguration modules)
    {
        InitializeComponent();
        _settings = settings;
        _modules = modules;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ModulesView.ItemsSource = await Task.Run(() => ModuleCatalog.GetAll());

        if (_settings != null && _modules?.Modules != null)
        {
            string[] enabled = new[]
            {
                ("Windows", _modules.Modules.Windows),
                ("AI", _modules.Modules.AI),
                ("Automation", _modules.Modules.Automation),
                ("Memory", _modules.Modules.Memory),
                ("Plugins", _modules.Modules.Plugins)
            }.Where(m => m.Item2).Select(m => m.Item1).ToArray();

            ConfigSummaryLabel.Text = "Tema: " + _settings.Theme +
                "  |  Módulos: " + (enabled.Length == 0 ? "(nenhum)" : string.Join(", ", enabled));
        }
    }
}
