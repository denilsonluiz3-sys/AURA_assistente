using System.Text;
using AURA.AI.UniversalAI;
using AURA.Mobile.Diagnostics;
using AURA.Mobile.Services;

namespace AURA.Mobile.Pages;

public partial class WorkspacePage : ContentPage
{
    private readonly WorkspaceDocumentService _documents;
    private readonly IUniversalAiClient _ai;
    private readonly List<string> _paths = new();
    private string? _currentPath;
    private string? _lastAgentAnswer;
    private bool _agentBusy;

    private const string SystemPrompt =
        "Você é assistente do Workspace da AURA. " +
        "Responda em português, de forma direta. " +
        "O usuário já abriu um arquivo; use só o conteúdo fornecido. " +
        "Não peça caminhos, não invente comandos shell, não sugira tools. " +
        "Se o pedido for reescrita, devolva o texto completo pronto para colar.";

    public WorkspacePage(WorkspaceDocumentService documents, IUniversalAiClient ai)
    {
        InitializeComponent();
        _documents = documents;
        _ai = ai;
        RefreshDocuments();
        UpdateEmptyHint();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshDocuments();
    }

    private void RefreshDocuments()
    {
        _paths.Clear();
        _paths.AddRange(_documents.ListDocuments());
        DocumentPicker.ItemsSource = _paths.Select(DisplayName).ToList();
        StatusLabel.Text = $"{_paths.Count} documento(s)";
        if (_currentPath is not null)
        {
            int index = _paths.FindIndex(p => string.Equals(p, _currentPath, StringComparison.OrdinalIgnoreCase));
            if (index >= 0) DocumentPicker.SelectedIndex = index;
        }
        UpdateEmptyHint();
    }

    private void UpdateEmptyHint()
    {
        bool hasDoc = _currentPath is not null;
        EmptyHint.IsVisible = !hasDoc;
        WordEditor.IsVisible = hasDoc && IsWord(_currentPath);
        PdfEditor.IsVisible = hasDoc && IsPdf(_currentPath);
    }

    private async void OnImportClicked(object? sender, EventArgs e)
    {
        try
        {
            FileResult? file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Importar para o Workspace" });
            if (file is null) return;
            string path = await _documents.ImportAsync(file);
            RefreshDocuments();
            int index = _paths.FindIndex(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            if (index >= 0) DocumentPicker.SelectedIndex = index;
        }
        catch (Exception ex)
        {
            AuraLog.Exception("WorkspacePage.Import", ex);
            await DisplayAlert("Workspace", ex.Message, "OK");
        }
    }

    private void OnRefreshClicked(object? sender, EventArgs e) => RefreshDocuments();

    private void OnDocumentSelected(object? sender, EventArgs e)
    {
        int index = DocumentPicker.SelectedIndex;
        if (index < 0 || index >= _paths.Count) return;
        _currentPath = _paths[index];
        OpenDocument(_currentPath);
    }

    private void OpenDocument(string path)
    {
        bool word = IsWord(path);
        WordEditor.IsVisible = word;
        PdfEditor.IsVisible = !word && IsPdf(path);
        EmptyHint.IsVisible = false;

        if (word)
        {
            WordTextEditor.Text = _documents.ReadWord(path);
            StatusLabel.Text = "Word aberto";
            return;
        }

        if (IsPdf(path))
        {
            string absolute = Path.GetFullPath(path);
            PdfViewer.Source = absolute.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                ? absolute
                : "file://" + absolute;
            StatusLabel.Text = "PDF aberto";
        }
    }

    private async void OnSaveWordClicked(object? sender, EventArgs e)
    {
        if (_currentPath is null || !IsWord(_currentPath)) return;
        try
        {
            _documents.SaveWord(_currentPath, WordTextEditor.Text ?? string.Empty);
            StatusLabel.Text = "Word salvo";
        }
        catch (Exception ex)
        {
            AuraLog.Exception("WorkspacePage.SaveWord", ex);
            await DisplayAlert("Word", ex.Message, "OK");
        }
    }

    private async void OnRotatePdfClicked(object? sender, EventArgs e)
    {
        if (!EnsurePdf(out string path)) return;
        try
        {
            int page = GetCurrentPdfPage();
            _documents.RotatePdfPage(path, page);
            ReloadPdf(path);
            StatusLabel.Text = "Página girada e salva";
        }
        catch (Exception ex)
        {
            AuraLog.Exception("WorkspacePage.RotatePdf", ex);
            await DisplayAlert("PDF", ex.Message, "OK");
        }
    }

    private async void OnAddPdfPageClicked(object? sender, EventArgs e)
    {
        if (!EnsurePdf(out string path)) return;
        try
        {
            _documents.AddBlankPdfPage(path);
            ReloadPdf(path);
            StatusLabel.Text = "Página adicionada";
        }
        catch (Exception ex)
        {
            AuraLog.Exception("WorkspacePage.AddPdfPage", ex);
            await DisplayAlert("PDF", ex.Message, "OK");
        }
    }

    private async void OnDeletePdfPageClicked(object? sender, EventArgs e)
    {
        if (!EnsurePdf(out string path)) return;
        try
        {
            int page = GetCurrentPdfPage();
            _documents.DeletePdfPage(path, page);
            ReloadPdf(path);
            StatusLabel.Text = "Página excluída";
        }
        catch (Exception ex)
        {
            AuraLog.Exception("WorkspacePage.DeletePdfPage", ex);
            await DisplayAlert("PDF", ex.Message, "OK");
        }
    }

    // ------------------------------------------------------------------
    // Agente: atalhos + texto livre (sem tools, com contexto do arquivo)
    // ------------------------------------------------------------------

    private void OnChipResumir(object? sender, EventArgs e) =>
        IntentEditor.Text = "Resuma o conteúdo em português, em até 5 bullets claros.";

    private void OnChipCorrigir(object? sender, EventArgs e) =>
        IntentEditor.Text = "Corrija gramática e clareza. Devolva o texto completo corrigido.";

    private void OnChipSimplificar(object? sender, EventArgs e) =>
        IntentEditor.Text = "Simplifique a linguagem, sem perder o sentido. Devolva o texto completo.";

    private void OnChipExplicar(object? sender, EventArgs e) =>
        IntentEditor.Text = "Explique o conteúdo em linguagem simples, para leigo.";

    private async void OnSendToAgentClicked(object? sender, EventArgs e)
    {
        if (_agentBusy) return;

        string intent = (IntentEditor.Text ?? string.Empty).Trim();
        if (intent.Length == 0)
        {
            await DisplayAlert("Agente", "Escreva o que você quer fazer ou use um atalho.", "OK");
            return;
        }

        if (_currentPath is null)
        {
            await DisplayAlert("Agente", "Selecione ou importe um documento antes.", "OK");
            return;
        }

        _agentBusy = true;
        SendAgentButton.IsEnabled = false;
        StatusLabel.Text = "Agente pensando…";
        AgentResponseEditor.Text = "";
        ApplyWordButton.IsVisible = false;
        _lastAgentAnswer = null;

        try
        {
            string prompt = BuildAgentPrompt(intent);
            string answer = await _ai.ChatAsync(prompt, systemPrompt: SystemPrompt);
            _lastAgentAnswer = answer?.Trim() ?? string.Empty;
            AgentResponseEditor.Text = string.IsNullOrWhiteSpace(_lastAgentAnswer)
                ? "(sem resposta)"
                : _lastAgentAnswer;
            ApplyWordButton.IsVisible = IsWord(_currentPath) && !string.IsNullOrWhiteSpace(_lastAgentAnswer);
            StatusLabel.Text = "Resposta do agente pronta";
        }
        catch (Exception ex)
        {
            AuraLog.Exception("WorkspacePage.SendToAgent", ex);
            AgentResponseEditor.Text = "Erro: " + ex.Message;
            StatusLabel.Text = "Falha no agente";
            await DisplayAlert("Agente", ex.Message, "OK");
        }
        finally
        {
            _agentBusy = false;
            SendAgentButton.IsEnabled = true;
        }
    }

    private async void OnApplyAgentToWordClicked(object? sender, EventArgs e)
    {
        if (_currentPath is null || !IsWord(_currentPath) || string.IsNullOrWhiteSpace(_lastAgentAnswer))
            return;

        bool ok = await DisplayAlert(
            "Aplicar no Word",
            "Substituir o texto do documento pela resposta do agente?",
            "Aplicar",
            "Cancelar");
        if (!ok) return;

        try
        {
            WordTextEditor.Text = _lastAgentAnswer;
            _documents.SaveWord(_currentPath, _lastAgentAnswer!);
            StatusLabel.Text = "Resposta aplicada e salva no Word";
        }
        catch (Exception ex)
        {
            AuraLog.Exception("WorkspacePage.ApplyAgentToWord", ex);
            await DisplayAlert("Word", ex.Message, "OK");
        }
    }

    private string BuildAgentPrompt(string intent)
    {
        var sb = new StringBuilder();
        sb.AppendLine("### Arquivo atual");
        sb.AppendLine(DisplayName(_currentPath!));
        sb.AppendLine();
        sb.AppendLine("### Conteúdo");

        if (IsWord(_currentPath))
        {
            string body = WordTextEditor.Text ?? _documents.ReadWord(_currentPath!);
            sb.AppendLine(Truncate(body, 12000));
        }
        else if (IsPdf(_currentPath))
        {
            sb.AppendLine("(PDF aberto na interface; texto embutido não foi extraído nesta versão.)");
            sb.AppendLine("Caminho: " + _currentPath);
        }
        else
        {
            sb.AppendLine("(tipo de arquivo sem pré-visualização de texto)");
        }

        sb.AppendLine();
        sb.AppendLine("### Pedido do usuário");
        sb.AppendLine(intent);
        return sb.ToString();
    }

    private bool EnsurePdf(out string path)
    {
        path = _currentPath ?? string.Empty;
        return IsPdf(path);
    }

    private void ReloadPdf(string path)
    {
        string absolute = Path.GetFullPath(path);
        PdfViewer.Source = null;
        PdfViewer.Source = absolute.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            ? absolute
            : "file://" + absolute;
    }

    private int GetCurrentPdfPage()
    {
        try
        {
            var prop = PdfViewer.GetType().GetProperty("CurrentPage");
            if (prop?.GetValue(PdfViewer) is int page && page >= 0)
                return page;
        }
        catch
        {
            // ignore
        }
        return 0;
    }

    private static bool IsWord(string? path) =>
        path is not null && path.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);

    private static bool IsPdf(string? path) =>
        path is not null && path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

    private static string DisplayName(string path) =>
        Path.GetRelativePath(AgentWorkspace.ActiveRoot, path);

    private static string Truncate(string text, int max)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= max) return text ?? string.Empty;
        return text[..max] + "\n… [conteúdo truncado]";
    }
}
