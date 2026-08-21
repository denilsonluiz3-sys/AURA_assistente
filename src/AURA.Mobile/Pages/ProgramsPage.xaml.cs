using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AURA.Abstractions;
using AURA.Agents;
using AURA.Agents.Programs;
using AURA.Core.Logging;

namespace AURA.Mobile.Pages;

public partial class ProgramsPage : ContentPage
{
    private readonly CellProgramRegistry _registry;
    private readonly CellProgramRunner _runner;
    private readonly IAuraCellContextFactory _contextFactory;
    private readonly PolicyGuard _policyGuard;

    public ObservableCollection<ProgramViewModel> Programs { get; } = new();

    public ProgramsPage(
        CellProgramRegistry registry,
        CellProgramRunner runner,
        IAuraCellContextFactory contextFactory,
        PolicyGuard policyGuard)
    {
        InitializeComponent();
        _registry = registry;
        _runner = runner;
        _contextFactory = contextFactory;
        _policyGuard = policyGuard;
        ProgramsView.ItemsSource = Programs;
        LoadPrograms();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadPrograms();
    }

    private void LoadPrograms()
    {
        Programs.Clear();
        foreach (var program in _registry.All.OrderBy(p => p.Name))
            Programs.Add(new ProgramViewModel(program));
    }

    private async void OnExecuteClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ProgramViewModel vm || vm.IsRunning)
            return;

        var auth = _policyGuard.Authorize(vm.Program.RequiredCapabilities, "ui:" + vm.Name);
        if (auth.Decision == AuthorizationDecision.Blocked)
        {
            vm.SetFailure(auth.Message);
            return;
        }

        CancellationToken ct = CancellationToken.None;
        vm.SetRunning();
        try
        {
            string cellId = $"program-ui-{Guid.NewGuid():N}";
            var context = _contextFactory.Create(cellId, ct);
            var result = await _runner.RunAsync(vm.Program, context, ct);

            if (result.IsSuccess)
                vm.SetSuccess(result.Data);
            else
                vm.SetFailure(result.Error ?? "Falha sem mensagem.");
        }
        catch (OperationCanceledException)
        {
            vm.SetCancelled();
        }
        catch (Exception ex)
        {
            vm.SetFailure(ex.Message);
        }
    }

    private async void OnDetailsClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is not ProgramViewModel vm || vm.RawResult == null)
            return;

        await DisplayAlert("Dados técnicos — " + vm.Name, vm.RawResult, "Fechar");
    }
}

public enum ProgramState
{
    Ready,
    Running,
    Success,
    Failed,
    Cancelled
}

public sealed class ProgramViewModel : INotifyPropertyChanged
{
    public IAuraCellProgram Program { get; }
    public string Name => Program.Name;
    public string CapabilitiesText => string.Join(" • ", Program.RequiredCapabilities);

    private ProgramState _state = ProgramState.Ready;
    private string _lastResult = "Pronto para execução";
    private string? _rawResult;

    public ProgramState State { get => _state; private set => Set(ref _state, value); }
    public string LastResult { get => _lastResult; private set => Set(ref _lastResult, value); }
    public string? RawResult { get => _rawResult; private set => Set(ref _rawResult, value); }
    public bool IsRunning => State == ProgramState.Running;
    public bool CanExecute => !IsRunning;
    public bool HasResult => RawResult != null;
    public string ExecuteText => IsRunning ? "Executando..." : "Executar";
    public string StateText => State switch
    {
        ProgramState.Running => "Executando",
        ProgramState.Success => "Concluído",
        ProgramState.Failed => "Falhou",
        ProgramState.Cancelled => "Cancelado",
        _ => "Pronto"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    public ProgramViewModel(IAuraCellProgram program) => Program = program;

    public void SetRunning()
    {
        RawResult = null;
        State = ProgramState.Running;
        LastResult = "Executando programa interno controlado...";
        Notify(nameof(IsRunning), nameof(CanExecute), nameof(ExecuteText), nameof(StateText), nameof(HasResult));
    }

    public void SetSuccess(object? data)
    {
        RawResult = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        LastResult = BuildPreview(data);
        State = ProgramState.Success;
        Notify(nameof(IsRunning), nameof(CanExecute), nameof(ExecuteText), nameof(StateText), nameof(HasResult));
    }

    public void SetFailure(string error)
    {
        RawResult = null;
        LastResult = "Erro: " + error;
        State = ProgramState.Failed;
        Notify(nameof(IsRunning), nameof(CanExecute), nameof(ExecuteText), nameof(StateText), nameof(HasResult));
    }

    public void SetCancelled()
    {
        LastResult = "Execução cancelada.";
        State = ProgramState.Cancelled;
        Notify(nameof(IsRunning), nameof(CanExecute), nameof(ExecuteText), nameof(StateText));
    }

    private static string BuildPreview(object? data) =>
        data == null ? "Concluído sem dados." : JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private void Notify(params string[] names)
    {
        foreach (var name in names)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
