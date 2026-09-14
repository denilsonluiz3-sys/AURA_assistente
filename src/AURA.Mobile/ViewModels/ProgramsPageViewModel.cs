using System;
using System.Collections.Generic;
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
        private string _urlInput = string.Empty;
        private string _selectorInput = string.Empty;
        private string _textInput = string.Empty;
        private string _numberInput = string.Empty;

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
            "device-diagnostic" => "Lê bateria, rede, sensores e propriedades reais do Android.",
            "browser-open" => "Abre uma URL no navegador interno da AURA.",
            "browser-read" => "Lê o texto e a árvore DOM da página atualmente aberta.",
            "browser-click" => "Clica em um elemento usando um seletor CSS.",
            "browser-type" => "Preenche um campo usando seletor CSS e texto.",
            "browser-scroll" => "Rola a página atual pelo número de pixels informado.",
            "browser-wait" => "Aguarda a página por um intervalo definido.",
            "browser-back" => "Volta uma página no histórico do navegador.",
            "browser-forward" => "Avança uma página no histórico do navegador.",
            "browser-screenshot" => "Salva uma captura da página atual no workspace.",
            _ => "Programa interno controlado pela AURA."
        };

        public bool NeedsUrl => _program.Name == "browser-open";
        public bool NeedsSelector => _program.Name is "browser-read" or "browser-click" or "browser-type";
        public bool NeedsText => _program.Name == "browser-type";
        public bool NeedsNumber => _program.Name is "browser-scroll" or "browser-wait";
        public string UrlInput { get => _urlInput; set { _urlInput = value ?? string.Empty; OnChanged(); } }
        public string SelectorInput { get => _selectorInput; set { _selectorInput = value ?? string.Empty; OnChanged(); } }
        public string TextInput { get => _textInput; set { _textInput = value ?? string.Empty; OnChanged(); } }
        public string NumberInput { get => _numberInput; set { _numberInput = value ?? string.Empty; OnChanged(); } }
        public string ArgumentHint => _program.Name switch
        {
            "browser-open" => "URL https://…",
            "browser-read" => "Seletor CSS opcional (vazio = página inteira)",
            "browser-click" => "Seletor CSS, ex.: button[type=submit]",
            "browser-type" => "Seletor CSS + texto abaixo",
            "browser-scroll" => "Pixels, ex.: 600 ou -400",
            "browser-wait" => "Milissegundos, ex.: 1000",
            _ => "Nenhum parâmetro necessário"
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

        private Dictionary<string, string>? BuildArguments(out string? validationError)
        {
            validationError = null;
            var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (NeedsUrl)
            {
                if (!Uri.TryCreate(UrlInput.Trim(), UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                { validationError = "Informe uma URL http:// ou https:// válida."; return null; }
                args["url"] = uri.ToString();
            }
            if (_program.Name is "browser-click" or "browser-type")
            {
                if (string.IsNullOrWhiteSpace(SelectorInput)) { validationError = "Informe o seletor CSS do elemento."; return null; }
                args["selector"] = SelectorInput.Trim();
            }
            else if (_program.Name == "browser-read" && !string.IsNullOrWhiteSpace(SelectorInput))
                args["selector"] = SelectorInput.Trim();
            if (NeedsText)
            {
                if (string.IsNullOrWhiteSpace(TextInput)) { validationError = "Informe o texto a preencher."; return null; }
                args["text"] = TextInput;
            }
            if (NeedsNumber)
            {
                if (!int.TryParse(NumberInput, out var number)) { validationError = "Informe um número inteiro válido."; return null; }
                args[_program.Name == "browser-scroll" ? "pixels" : "milliseconds"] = number.ToString();
            }
            return args;
        }

        private async Task ExecuteAsync()
        {
            if (_isRunning || !CanExecute) return;
            var arguments = BuildArguments(out var validationError);
            if (arguments == null)
            {
                State = ProgramState.Error;
                LastResult = validationError;
                return;
            }

            _isRunning = true;
            State = ProgramState.Executing;
            LastResult = null;
            ((Command)ExecuteCommand).ChangeCanExecute();

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            try
            {
                var context = _contextFactory.Create($"program-ui-{Guid.NewGuid():N}", cts.Token, arguments);
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
