using AURA.Core.Events;
using AURA.Core.Launchers;
using AURA.Core.Runtime;
using Cell = AURA.Core.Runtime.Cell;

namespace AURA.Mobile.Pages;

public partial class CellsPage : ContentPage
{
    private readonly SimulationRuntime _runtime;
    private readonly Runner _runner;
    private readonly RunPage _runPage;
    private readonly EventBus _events;
    private bool _loaded;
    private bool _subscribed;

    public CellsPage(SimulationRuntime runtime, Runner runner, RunPage runPage, EventBus events)
    {
        InitializeComponent();
        _runtime = runtime;
        _runner = runner;
        _runPage = runPage;
        _events = events;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SubscribeRuntimeEvents();

        if (!_loaded)
        {
            _loaded = true;
            try
            {
                await _runtime.LoadFromStoreAsync();
            }
            catch (Exception ex)
            {
                AuraLog.Exception("CellsPage.LoadFromStore", ex);
            }
        }

        Refresh();
    }

    protected override void OnDisappearing()
    {
        UnsubscribeRuntimeEvents();
        base.OnDisappearing();
    }

    private void SubscribeRuntimeEvents()
    {
        if (_subscribed)
        {
            return;
        }

        _events.Subscribe<CellStateChangedEvent>(OnCellStateChanged);
        _subscribed = true;
    }

    private void UnsubscribeRuntimeEvents()
    {
        if (!_subscribed)
        {
            return;
        }

        _events.Unsubscribe<CellStateChangedEvent>(OnCellStateChanged);
        _subscribed = false;
    }

    private void OnCellStateChanged(CellStateChangedEvent _)
    {
        MainThread.BeginInvokeOnMainThread(Refresh);
    }

    private void Refresh()
    {
        CellsView.ItemsSource = _runtime.Cells
            .OrderBy(c => c.Id)
            .ToList();
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Cell cell)
        {
            return;
        }

        try
        {
            await _runtime.StartCellAsync(cell.Id);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
        }

        Refresh();
    }

    private async void OnStopClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Cell cell)
        {
            return;
        }

        _runtime.StopCell(cell.Id);
        Refresh();
    }

    private async void OnPauseClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Cell cell)
        {
            return;
        }

        _runtime.PauseCell(cell.Id);
        Refresh();
    }

    private async void OnResumeClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Cell cell)
        {
            return;
        }

        _runtime.ResumeCell(cell.Id);
        Refresh();
    }

    private async void OnLogClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Cell cell)
        {
            return;
        }

        string log = _runtime.ReadCellLog(cell.Id, 300);
        await DisplayAlert("Log: " + cell.Id, log, "Fechar");
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not Cell cell)
        {
            return;
        }

        bool confirm = await DisplayAlertAsync(
            "Excluir célula",
            "Excluir '" + cell.Id + "' e todos os seus dados?",
            "Excluir",
            "Cancelar");

        if (!confirm)
        {
            return;
        }

        _runtime.DeleteCell(cell.Id);
        Refresh();
    }

    private async void OnNewClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(_runPage);
    }

    private void OnRefreshClicked(object sender, EventArgs e)
    {
        Refresh();
    }
}
