using AURA.Core.Launchers;
using AURA.Core.Runtime;
using Cell = AURA.Core.Runtime.Cell;

namespace AURA.Mobile.Pages;

public partial class RunPage : ContentPage
{
    private readonly SimulationRuntime _runtime;
    private readonly Runner _runner;
    private string? _filePath;

    public RunPage(SimulationRuntime runtime, Runner runner)
    {
        InitializeComponent();
        _runtime = runtime;
        _runner = runner;
    }

    private async void OnCopyClicked(object sender, EventArgs e)
    {
        string text = ResultLabel.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return;

        await Clipboard.Default.SetTextAsync(text);
        string original = CopyButton.Text;
        CopyButton.Text = "✓ Copiado";
        await Task.Delay(1500);
        CopyButton.Text = original;
    }

    private async void OnPickClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Escolha um programa para rodar em célula"
            });

            if (result == null)
                return;

            _filePath = result.FullPath;
            FileLabel.Text = "Arquivo: " + _filePath;
            UpdateLauncherInfo();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro", ex.Message, "OK");
            AuraLog.Exception("RunPage.Pick", ex);
        }
    }

    private void UpdateLauncherInfo()
    {
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            LauncherLabel.Text = string.Empty;
            return;
        }

        var launcher = _runner.ResolveLauncher(_filePath);
        if (launcher == null)
        {
            string supported = string.Join(", ",
                _runner.Launchers.SelectMany(l => l.SupportedExtensions));
            LauncherLabel.Text = "Sem launcher para esta extensão. Suportados: " + supported;
        }
        else
        {
            LauncherLabel.Text = "Launcher: " + launcher.GetType().Name;
        }
    }

    private async void OnRunClicked(object sender, EventArgs e)
    {
        string exe = SanitizePathOrExe(ExeEntry.Text);
        string id = SanitizeCellId(CellIdEntry.Text);
        string args = ArgsEntry.Text?.Trim() ?? string.Empty;

        // args de várias linhas / scripts não cabem no campo executável
        if (!string.IsNullOrEmpty(exe) && (exe.Contains('\n') || exe.StartsWith("#!") || exe.Length > 512))
        {
            ResultLabel.Text = "Executável inválido: use um caminho curto (sem script multilinha). " +
                "Para scripts, escolha o arquivo pelo seletor.";
            return;
        }

        var limits = new ResourceLimits();
        if (long.TryParse(MemEntry.Text, out long mb) && mb > 0)
            limits.MemoryLimitMb = mb;

        if (string.IsNullOrWhiteSpace(exe) && string.IsNullOrWhiteSpace(_filePath))
        {
            ResultLabel.Text = "Escolha um arquivo ou informe um executável.";
            return;
        }

        BusyIndicator.IsRunning = true;
        BusyIndicator.IsVisible = true;

        try
        {
            Cell cell;
            if (!string.IsNullOrWhiteSpace(exe))
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    id = Path.GetFileNameWithoutExtension(exe) + "-" +
                        Guid.NewGuid().ToString("N").Substring(0, 6);
                    id = SanitizeCellId(id);
                }

                cell = _runtime.CreateCell(id, exe, args,
                    workingDirectory: FileSystem.AppDataDirectory,
                    limits: limits.IsEmpty ? null : limits);
                await _runtime.StartCellAsync(cell.Id);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(id))
                    id = Path.GetFileNameWithoutExtension(_filePath!) + "-" +
                        Guid.NewGuid().ToString("N").Substring(0, 6);

                id = SanitizeCellId(id);
                cell = await _runner.RunAsync(_runtime, id, _filePath!, args,
                    limits: limits.IsEmpty ? null : limits);
            }

            ResultLabel.Text =
                $"Célula '{cell.Id}' criada e iniciada (pid {cell.ProcessId}). " +
                "Gerencie na aba Células.";
            AuraLog.Info("RunPage: célula iniciada " + cell.Id + " (" + (cell.AppPath ?? "") + ")");
        }
        catch (Exception ex)
        {
            ResultLabel.Text = "Erro: " + (ex.Message.Length > 400 ? ex.Message[..400] + "…" : ex.Message);
            AuraLog.Exception("RunPage.Run", ex);
        }
        finally
        {
            BusyIndicator.IsRunning = false;
            BusyIndicator.IsVisible = false;
        }
    }

    /// <summary>Remove quebras de linha e caracteres ilegais de path; evita PathTooLong.</summary>
    private static string SanitizePathOrExe(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string s = raw.Trim().Replace("\0", string.Empty);
        // uma linha só
        int nl = s.IndexOfAny(new[] { '\r', '\n' });
        if (nl >= 0)
            s = s.Substring(0, nl).Trim();

        foreach (char c in Path.GetInvalidPathChars())
            s = s.Replace(c.ToString(), string.Empty);

        if (s.Length > 512)
            s = s.Substring(0, 512);

        return s;
    }

    private static string SanitizeCellId(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (char c in raw.Trim())
        {
            if (char.IsLetterOrDigit(c) || c is '-' or '_' or '.')
                sb.Append(c);
        }

        string id = sb.ToString();
        if (id.Length > 64)
            id = id.Substring(0, 64);
        if (string.IsNullOrEmpty(id) || id is "null" or "undefined")
            id = "cell-" + Guid.NewGuid().ToString("N")[..8];

        return id;
    }
}
