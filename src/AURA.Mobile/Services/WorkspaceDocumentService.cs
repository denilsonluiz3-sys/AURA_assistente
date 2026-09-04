using AURA.Mobile.Diagnostics;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;

namespace AURA.Mobile.Services;

/// <summary>
/// Documento layer do workspace. Mantém Word (.docx) e PDF dentro do mesmo
/// diretório controlado pelo AgentWorkspace, sem criar um filesystem paralelo.
/// </summary>
public sealed class WorkspaceDocumentService
{
    public string WorkspaceRoot => AgentWorkspace.ActiveRoot;

    public IReadOnlyList<string> ListDocuments()
    {
        if (!Directory.Exists(WorkspaceRoot)) return Array.Empty<string>();
        return Directory.EnumerateFiles(WorkspaceRoot, "*", SearchOption.AllDirectories)
            .Where(IsSupportedDocument)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<string> ImportAsync(FileResult file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        string name = Path.GetFileName(file.FileName);
        if (!IsSupportedDocument(name))
            throw new InvalidOperationException("O Workspace aceita .docx e .pdf nesta versão.");
        Directory.CreateDirectory(WorkspaceRoot);
        string destination = GetSafePath(name);

        // FileResult.OpenReadAsync() no MAUI não aceita CancellationToken.
        await using Stream source = await file.OpenReadAsync();
        await using FileStream target = new(destination, FileMode.Create, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(target, cancellationToken);
        return destination;
    }

    public string ReadWord(string path)
    {
        string safePath = Resolve(path);
        using WordprocessingDocument document = WordprocessingDocument.Open(safePath, false);
        Body? body = document.MainDocumentPart?.Document?.Body;
        if (body is null) return string.Empty;
        var paragraphs = body.Elements<Paragraph>()
            .Select(p => string.Concat(p.Descendants<Text>().Select(t => t.Text ?? string.Empty)));
        return string.Join(Environment.NewLine, paragraphs);
    }

    public void SaveWord(string path, string text)
    {
        string safePath = Resolve(path);
        using WordprocessingDocument document = WordprocessingDocument.Open(safePath, true);
        MainDocumentPart main = document.MainDocumentPart
            ?? throw new InvalidDataException("DOCX sem MainDocumentPart.");
        Body body = main.Document.Body ?? main.Document.AppendChild(new Body());
        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var existing = body.Elements<Paragraph>().ToList();
        int count = Math.Max(lines.Length, existing.Count);

        for (int i = 0; i < count; i++)
        {
            Paragraph paragraph;
            if (i < existing.Count)
            {
                paragraph = existing[i];
                foreach (Run run in paragraph.Elements<Run>().ToList()) run.Remove();
            }
            else
            {
                paragraph = new Paragraph();
                body.Append(paragraph);
            }

            if (i < lines.Length && lines[i].Length > 0)
                paragraph.AppendChild(new Run(new Text(lines[i])
                { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }));
        }

        while (body.Elements<Paragraph>().Count() > lines.Length)
            body.Elements<Paragraph>().Last().Remove();
        main.Document.Save();
    }

    public void DeletePdfPage(string path, int zeroBasedPage)
    {
        string safePath = Resolve(path);
        using PdfDocument document = PdfReader.Open(safePath, PdfDocumentOpenMode.Modify);
        ValidatePage(document, zeroBasedPage);
        if (document.PageCount <= 1)
            throw new InvalidOperationException("Um PDF precisa manter pelo menos uma página.");
        document.Pages.RemoveAt(zeroBasedPage);
        document.Save(safePath);
    }

    public void RotatePdfPage(string path, int zeroBasedPage, int degrees = 90)
    {
        string safePath = Resolve(path);
        using PdfDocument document = PdfReader.Open(safePath, PdfDocumentOpenMode.Modify);
        ValidatePage(document, zeroBasedPage);
        int normalized = ((degrees % 360) + 360) % 360;
        document.Pages[zeroBasedPage].Rotate = (document.Pages[zeroBasedPage].Rotate + normalized) % 360;
        document.Save(safePath);
    }

    public void AddBlankPdfPage(string path)
    {
        string safePath = Resolve(path);
        using PdfDocument document = PdfReader.Open(safePath, PdfDocumentOpenMode.Modify);
        document.AddPage();
        document.Save(safePath);
    }

    public string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Caminho vazio.", nameof(path));
        string full = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(WorkspaceRoot, path));
        string root = Path.GetFullPath(WorkspaceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("O documento está fora do Workspace.");
        if (!File.Exists(full)) throw new FileNotFoundException("Documento não encontrado no Workspace.", full);
        return full;
    }

    public static bool IsSupportedDocument(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".docx", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    private string GetSafePath(string fileName)
    {
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        string candidate = Path.Combine(WorkspaceRoot, fileName);
        int suffix = 1;
        while (File.Exists(candidate)) candidate = Path.Combine(WorkspaceRoot, $"{baseName} ({suffix++}){extension}");
        return candidate;
    }

    private static void ValidatePage(PdfDocument document, int page)
    {
        if (page < 0 || page >= document.PageCount)
            throw new ArgumentOutOfRangeException(nameof(page), "Página PDF inválida.");
    }
}
