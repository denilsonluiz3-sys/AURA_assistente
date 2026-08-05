using System.Collections.ObjectModel;
using AURA.Memory;

namespace AURA.Mobile.Pages;

public partial class MemoryPage : ContentPage
{
    private readonly MemoryStore _memoryStore;
    public ObservableCollection<MemoryEntry> Entries { get; } = new();

    public MemoryPage(MemoryStore memoryStore)
    {
        InitializeComponent();
        _memoryStore = memoryStore;
        EntriesView.ItemsSource = Entries;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var entries = await Task.Run(() => _memoryStore.Read(64));
        Entries.Clear();
        foreach (var entry in entries)
        {
            Entries.Add(entry);
        }

        if (Entries.Count == 0)
        {
            Entries.Add(new MemoryEntry { Role = "AURA", Text = "Nenhuma memória registrada ainda." });
        }
    }

    private async void OnClearClicked(object sender, EventArgs e)
    {
        bool confirmed = await DisplayAlert("Limpar memória", "Apagar todo o histórico persistido?", "Apagar", "Cancelar");
        if (!confirmed)
        {
            return;
        }

        await Task.Run(() => _memoryStore.Clear());
        await RefreshAsync();
    }
}
