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

        public ProgramsPageViewModel(
            CellProgramRegistry registry,
            CellProgramRunner runner,
            IAuraCellContextFactory contextFactory,
            ILogger logger)
        {
            _registry = registry;
            _runner = runner;
            _contextFactory = contextFactory;
            _logger = logger;
            LoadPrograms();
        }

        private void LoadPrograms()
        {
            Programs.Clear();
            foreach (var program in _registry.All)
                Programs.Add(new ProgramCardViewModel(program, _runner, _contextFactory, _logger));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public sealed class ProgramCardViewModel : INotifyPropertyChanged
    {
        private readonly IAuraCellProgram _program;
        private readonly CellProgramRunner _runner;
        private readonly IAuraCellContextFactory _contextFactory;
        private readonly ILogger _logger;
        private ProgramState _state = ProgramState.Available;
        private string? _lastResult;
        private bool _isRunning;

        public ProgramCardViewModel(
            IAuraCellProgram program,
            CellProgramRunner runner,
            IAuraCellContextFactory contextFactory,
            ILogger logger)
        {
            _program = program;
            _runner = runner;
            _contextFactory = contextFactory;
            _logger = logger;
            ExecuteCommand = new Command(async () => await ExecuteAsync(), () => CanExecute);
        }

        public string DisplayName => _program.Name.Replace("-", " ").ToUpperInvariant();

        public string Description => _program.Name switch
        {
            "device-diagnostic" => "Diagnóstico completo do dispositivo Android (bateria, rede, sensores, propriedades).",
            _ => "Programa interno controlado pela AURA."
        };

        public string CapabilitiesText =>
            _program.RequiredCapabilities.Count == 0
                ? "Sem capacidades especiais"
                : $"Requer: {string.Join(", ", _program.RequiredCapabilities)}";

        public ProgramState State
        {
            get => _state;
            private set
            {
                _state = value;
                OnChanged();
                OnChanged(nameof(StatusText));
                OnChanged(nameof(StatusColor));
                OnChanged(nameof(ActionButtonText));
                OnChanged(nameof(CanExecute));
            }
        }

        public string? LastResult
        {
            get => _lastResult;
            private set
            {
                _lastResult = value;
                OnChanged();
                OnChanged(nameof(HasResult));
                OnChanged(nameof(ResultSummary));
            }
        }

        public bool HasResult => !string.IsNullOrEmpty(LastResult);

        public string ResultSummary =>
            HasResult
                ? (LastResult!.Length > 140 ? LastResult.Substring(0, 140) + "…" : LastResult)
                : string.Empty;

        public string StatusText => State switch
        {
            ProgramState.Available => "Disponível",
            ProgramState.Executing => "Executando",
            ProgramState.Completed => "Concluído",
            ProgramState.Blocked => "Bloqueado",
            ProgramState.RequiresConfirmation => "Requer confirmação",
            ProgramState.Unavailable => "Indisponível",
            ProgramState.Error => "Erro",
            _ => "Disponível"
        };

        public Color StatusColor => State switch
        {
            ProgramState.Available => Color.FromArgb("#7a7a90"),
            ProgramState.Executing => Color.FromArgb("#f0a050"),
            ProgramState.Completed => Color.FromArgb("#3ec97a"),
            ProgramState.Blocked => Color.FromArgb("#e05560"),
            ProgramState.RequiresConfirmation => Color.FromArgb("#f0a050"),
            ProgramState.Unavailable => Color.FromArgb("#45455a"),
            ProgramState.Error => Color.FromArgb("#e05560"),
            _ => Color.FromArgb("#7a7a90")
        };

        public string ActionButtonText => State switch
        {
            ProgramState.Executing => "Executando…",
            ProgramState.Blocked => "Bloqueado",
            ProgramState.Unavailable => "Indisponível",
            _ => "Executar"
        };

        public bool CanExecute =>
            State is ProgramState.Available or ProgramState.Completed or ProgramState.Error
            && !_isRunning;

        public ICommand ExecuteCommand { get; }

        private async Task ExecuteAsync()
        {
            if (_isRunning || !CanExecute) return;

            _isRunning = true;
            State = ProgramState.Executing;
            LastResult = null;
            ((Command)ExecuteCommand).ChangeCanExecute();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            try
            {
                var context = _contextFactory.Create($"program-ui-{Guid.NewGuid():N}", cts.Token);
                var result = await _runner.RunAsync(_program, context, cts.Token);

                if (result.IsSuccess)
                {
                    State = ProgramState.Completed;
                    LastResult = FormatResult(result.Data);
                }
                else
                {
                    var err = result.Error ?? "Falha desconhecida";
                    if (err.Contains("bloquead", StringComparison.OrdinalIgnoreCase)
                        || err.Contains("blocked", StringComparison.OrdinalIgnoreCase))
                    {
                        State = ProgramState.Blocked;
                        LastResult = err;
                    }
                    else if (err.Contains("confirma", StringComparison.OrdinalIgnoreCase)
                             || err.Contains("confirmation", StringComparison.OrdinalIgnoreCase))
                    {
                        State = ProgramState.RequiresConfirmation;
                        LastResult = err;
                    }
                    else
                    {
                        State = ProgramState.Error;
                        LastResult = $"Erro: {err}";
                    }
                }
            }
            catch (OperationCanceledException)
            {
                State = ProgramState.Error;
                LastResult = "Cancelado / timeout";
            }
            catch (Exception ex)
            {
                State = ProgramState.Error;
                LastResult = $"Erro: {ex.Message}";
                _logger.Error($"Falha no programa {_program.Name}: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
                ((Command)ExecuteCommand).ChangeCanExecute();
            }
        }

        private static string FormatResult(object? data)
        {
            if (data == null) return "Sem dados";
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(
                    data,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return data.ToString() ?? "Sem dados";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public enum ProgramState
    {
        Available,
        Executing,
        Completed,
        Blocked,
        RequiresConfirmation,
        Unavailable,
        Error
    }
}
