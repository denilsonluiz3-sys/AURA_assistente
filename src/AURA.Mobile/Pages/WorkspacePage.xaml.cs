using AURA.Mobile.Diagnostics;
using AURA.Mobile.Services;

namespace AURA.Mobile.Pages;

public partial class WorkspacePage : ContentPage
{
    private readonly WorkspaceDocumentService _documents;
    private readonly List<string> _paths = new();
    private string? _currentPath;

    public WorkspacePage(WorkspaceDocumentService documents)
    {
        InitializeComponent();
        _documents = documents;
        RefreshDocuments();
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
        bool word = path.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
        WordEditor.IsVisible = word;
        PdfEditor.IsVisible = !word;
        if (word)
        {
            WordTextEditor.Text = _documents.ReadWord(path);
            StatusLabel.Text = "Word aberto";
            return;
        }

        // MauiNativePdfView aceita string (file path / file://) via conversão implícita.
        // Evita MauiNativePdfView.PdfSource, que não resolve no pacote 1.1.1 neste target.
        string absolute = Path.GetFullPath(path);
        PdfViewer.Source = absolute.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            ? absolute
            : "file://" + absolute;
        StatusLabel.Text = "PDF aberto";
    }

    private async void OnSaveWordClicked(object? sender, EventArgs e)
    {
        if (_currentPath is null || !_currentPath.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)) return;
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

    private bool EnsurePdf(out string path)
    {
        path = _currentPath ?? string.Empty;
        return path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
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
        // PdfView.CurrentPage pode não existir em todas as versões do pacote.
        // Fallback seguro para a primeira página (0).
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

    private static string DisplayName(string path) => Path.GetRelativePath(AgentWorkspace.ActiveRoot, path);
}
