using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AURA.Abstractions;
using AURA.Agents.Programs;
using AURA.Core.Logging;
using Microsoft.Maui.Controls;

namespace AURA.Mobile.ViewModels
{
    public sealed class ProgramsPageViewModel : INotifyPropertyChanged
    {
        private readonly CellProgramRegistry _registry;
        private readonly CellProgramRunner _runner;
        private readonly IAuraCellContextFactory _contextFactory;
        private readonly ILogger _logger;
        public ObservableCollection<ProgramCardViewModel> Programs { get; } = new();
        public ProgramsPageViewModel(CellProgramRegistry registry, CellProgramRunner runner, IAuraCellContextFactory contextFactory, ILogger logger) { _registry = registry; _runner = runner; _contextFactory = contextFactory; _logger = logger; LoadPrograms(); }
        private void LoadPrograms() { Programs.Clear(); foreach (var program in _registry.All) Programs.Add(new ProgramCardViewModel(program, _runner, _contextFactory, _logger)); }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    public sealed class ProgramCardViewModel : INotifyPropertyChanged
    {
        private readonly IAuraCellProgram _program;
        private readonly CellProgramRunner _runner;
        private readonly IAuraCellContextFactory _contextFactory;
        private readonly ILogger _logger;
        private ProgramState _state = ProgramState.Ready;
        private string? _lastResult;
        private bool _isRunning;
        public ProgramCardViewModel(IAuraCellProgram program, CellProgramRunner runner, IAuraCellContextFactory contextFactory, ILogger logger) { _program = program; _runner = runner; _contextFactory = contextFactory; _logger = logger; ExecuteCommand = new Command(async () => await ExecuteAsync(), () => !_isRunning); }
        public string DisplayName => _program.Name.Replace("-", " ").ToUpperInvariant();
        public string Description => "Programa interno controlado";
        public string CapabilitiesText => $"Requer: {string.Join(", ", _program.RequiredCapabilities)}";
        public ProgramState State { get => _state; private set { _state = value; OnChanged(); OnChanged(nameof(StatusText)); OnChanged(nameof(StatusColor)); OnChanged(nameof(ActionButtonText)); OnChanged(nameof(CanExecute)); } }
        public string? LastResult { get => _lastResult; private set { _lastResult = value; OnChanged(); OnChanged(nameof(HasResult)); OnChanged(nameof(ResultSummary)); } }
        public bool HasResult => !string.IsNullOrEmpty(LastResult);
        public string ResultSummary => HasResult ? (LastResult!.Length > 120 ? LastResult.Substring(0, 120) + "…" : LastResult) : string.Empty;
        public string StatusText => State switch { ProgramState.Ready => "Pronto", ProgramState.Running => "Executando…", ProgramState.Success => "Concluído", ProgramState.Failed => "Falhou", ProgramState.Cancelled => "Cancelado", _ => "Pronto" };
        public Color StatusColor => State switch { ProgramState.Ready => Color.FromArgb("#7a7a90"), ProgramState.Running => Color.FromArgb("#f5b85a"), ProgramState.Success => Color.FromArgb("#6cdb9a"), ProgramState.Failed => Color.FromArgb("#e05560"), _ => Color.FromArgb("#7a7a90") };
        public string ActionButtonText => State == ProgramState.Running ? "Executando…" : "Executar";
        public bool CanExecute => State != ProgramState.Running;
        public ICommand ExecuteCommand { get; }
        private async Task ExecuteAsync() { if (_isRunning) return; _isRunning = true; State = ProgramState.Running; LastResult = null; using var cts = new CancellationTokenSource(); try { var context = _contextFactory.Create($"program-ui-{Guid.NewGuid():N}", cts.Token); var result = await _runner.RunAsync(_program, context, cts.Token); if (result.IsSuccess) { State = ProgramState.Success; LastResult = FormatResult(result.Data); } else { State = ProgramState.Failed; LastResult = $"Erro: {result.Error}"; } } catch (OperationCanceledException) { State = ProgramState.Cancelled; LastResult = "Cancelado"; } catch (Exception ex) { State = ProgramState.Failed; LastResult = $"Erro: {ex.Message}"; _logger.Error($"Falha no programa {_program.Name}: {ex.Message}"); } finally { _isRunning = false; OnChanged(nameof(CanExecute)); OnChanged(nameof(ActionButtonText)); } }
        private static string FormatResult(object? data) => data == null ? "Sem dados" : System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public enum ProgramState { Ready, Running, Success, Failed, Cancelled }
}
