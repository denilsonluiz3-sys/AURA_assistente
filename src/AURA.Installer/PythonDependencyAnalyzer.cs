using System.Text.RegularExpressions;

namespace AURA.Installer;

/// <summary>
/// Analisador de dependências para arquivos .py. Prioridade:
/// 1) Se existir um requirements.txt na mesma pasta do script, usa ele
///    (é a fonte mais confiável — o autor do script já disse o que precisa).
/// 2) Senão, varre os "import X" / "from X import ..." do próprio arquivo
///    e infere os pacotes pip prováveis, ignorando o que já é da stdlib.
/// </summary>
public sealed class PythonDependencyAnalyzer : IDependencyAnalyzer
{
    public ArtifactType SupportedType => ArtifactType.Python;

    // Casa "import a, b.c" e "from a.b import c" — captura só o módulo raiz de cada um.
    private static readonly Regex ImportRegex = new(
        @"^\s*(?:import\s+(?<mods>[\w\.]+(?:\s*,\s*[\w\.]+)*)|from\s+(?<from>[\w\.]+)\s+import\b)",
        RegexOptions.Multiline | RegexOptions.Compiled);

    // Nome do módulo importado -> nome real do pacote no PyPI, quando eles divergem.
    private static readonly Dictionary<string, string> KnownAliases = new(StringComparer.Ordinal)
    {
        ["cv2"] = "opencv-python",
        ["PIL"] = "Pillow",
        ["yaml"] = "PyYAML",
        ["bs4"] = "beautifulsoup4",
        ["sklearn"] = "scikit-learn",
        ["dotenv"] = "python-dotenv",
        ["serial"] = "pyserial",
        ["Crypto"] = "pycryptodome",
        ["attr"] = "attrs",
        ["dateutil"] = "python-dateutil",
        ["jwt"] = "PyJWT",
        ["docx"] = "python-docx",
        ["fitz"] = "PyMuPDF",
        ["telegram"] = "python-telegram-bot",
    };

    public async Task<DependencyReport> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var report = new DependencyReport { ArtifactType = ArtifactType.Python };

        string? directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        string? requirementsPath = directory is null ? null : Path.Combine(directory, "requirements.txt");

        if (requirementsPath is not null && File.Exists(requirementsPath))
        {
            report.HasRequirementsFile = true;
            report.RequirementsFilePath = requirementsPath;
            report.Dependencies.AddRange(await ParseRequirementsFileAsync(requirementsPath, cancellationToken));
            report.Notes.Add($"Dependências lidas de {Path.GetFileName(requirementsPath)} (fonte mais confiável).");
            return report;
        }

        string source = await File.ReadAllTextAsync(filePath, cancellationToken);
        var rootModules = ExtractRootModules(source);

        foreach (var module in rootModules)
        {
            if (PythonStdlibModules.IsStdlib(module))
            {
                continue;
            }

            if (KnownAliases.TryGetValue(module, out var packageName))
            {
                report.Dependencies.Add(packageName);
            }
            else
            {
                // Sem alias conhecido: assume que o nome do pacote pip é igual ao do módulo
                // (é o caso mais comum: requests, numpy, flask, ...), mas registra como não
                // totalmente resolvido pra deixar claro que é um palpite.
                report.Dependencies.Add(module);
                report.UnresolvedImports.Add(module);
            }
        }

        report.Dependencies = report.Dependencies.Distinct(StringComparer.Ordinal).OrderBy(d => d, StringComparer.Ordinal).ToList();
        report.Notes.Add("Sem requirements.txt na mesma pasta — dependências inferidas a partir dos imports do arquivo.");
        if (report.UnresolvedImports.Count > 0)
        {
            report.Notes.Add("Alguns pacotes foram assumidos com o mesmo nome do módulo importado; confirme antes de instalar em lote.");
        }

        return report;
    }

    private static List<string> ExtractRootModules(string source)
    {
        var modules = new List<string>();

        foreach (Match match in ImportRegex.Matches(source))
        {
            if (match.Groups["mods"].Success)
            {
                foreach (var mod in match.Groups["mods"].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    modules.Add(RootOf(mod));
                }
            }
            else if (match.Groups["from"].Success)
            {
                string fromModule = match.Groups["from"].Value;
                if (fromModule.StartsWith('.'))
                {
                    continue; // import relativo (".utils") -> não é dependência externa.
                }
                modules.Add(RootOf(fromModule));
            }
        }

        return modules.Distinct(StringComparer.Ordinal).ToList();
    }

    private static string RootOf(string dottedModule)
    {
        int dot = dottedModule.IndexOf('.');
        return dot < 0 ? dottedModule : dottedModule[..dot];
    }

    private static async Task<List<string>> ParseRequirementsFileAsync(string path, CancellationToken cancellationToken)
    {
        var result = new List<string>();
        var lines = await File.ReadAllLinesAsync(path, cancellationToken);

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith('-'))
            {
                continue; // ignora linhas vazias, comentários e flags (-r, --index-url, etc.)
            }

            // Remove especificador de versão/marcadores: "requests==2.31.0" -> "requests".
            int cutIndex = line.IndexOfAny(new[] { '=', '<', '>', '!', '~', ';', '[' });
            string packageName = cutIndex < 0 ? line : line[..cutIndex];
            packageName = packageName.Trim();

            if (packageName.Length > 0)
            {
                result.Add(packageName);
            }
        }

        return result.Distinct(StringComparer.Ordinal).OrderBy(p => p, StringComparer.Ordinal).ToList();
    }
}
