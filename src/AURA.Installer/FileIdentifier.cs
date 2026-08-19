using System.Text.RegularExpressions;

namespace AURA.Installer;

/// <summary>
/// Identifica o tipo de um artefato combinando três sinais, do mais para o
/// menos confiável: assinatura binária (magic bytes) → extensão → conteúdo
/// textual (só para tipos sem assinatura binária confiável, como .py).
/// </summary>
public sealed class FileIdentifier : IFileIdentifier
{
    // PE (Portable Executable): usado por .dll e .exe do .NET/Windows.
    private static readonly byte[] PeSignature = { 0x4D, 0x5A }; // "MZ"

    // Local File Header do formato ZIP: base do .jar (que é um zip com estrutura própria).
    private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 }; // "PK\x03\x04"

    private static readonly Regex PythonContentHint = new(
        @"^\s*(import\s+\w|from\s+\w[\w\.]*\s+import|def\s+\w|class\s+\w|print\s*\()",
        RegexOptions.Multiline | RegexOptions.Compiled);

    public async Task<ArtifactIdentification> IdentifyAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return ArtifactIdentification.Unrecognized(filePath, $"Arquivo não encontrado: {filePath}");
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        byte[] header = await ReadHeaderAsync(filePath, 8, cancellationToken);

        // 1) Assinatura PE ("MZ") -> DLL/assembly .NET.
        if (StartsWith(header, PeSignature))
        {
            bool extensionMatches = extension == ".dll";
            var result = new ArtifactIdentification
            {
                FilePath = filePath,
                Type = ArtifactType.DotNetAssembly,
                Confidence = extensionMatches ? 1.0 : 0.7
            };
            result.Notes.Add("Assinatura PE (\"MZ\") detectada nos primeiros bytes.");
            if (!extensionMatches)
            {
                result.Notes.Add($"Extensão do arquivo é \"{extension}\", não \".dll\" — confiança reduzida.");
            }
            return result;
        }

        // 2) Assinatura ZIP ("PK\x03\x04") -> candidato a .jar (jar é um zip).
        if (StartsWith(header, ZipSignature))
        {
            bool extensionMatches = extension == ".jar";
            var result = new ArtifactIdentification
            {
                FilePath = filePath,
                Type = ArtifactType.JarJava,
                Confidence = extensionMatches ? 1.0 : 0.4
            };
            result.Notes.Add("Assinatura ZIP (\"PK\\x03\\x04\") detectada — jar é um contêiner zip.");
            if (!extensionMatches)
            {
                result.Notes.Add($"Extensão do arquivo é \"{extension}\", não \".jar\" — pode ser um .zip comum, confiança reduzida.");
            }
            return result;
        }

        // 3) Sem assinatura binária conhecida: .py é texto puro, então usamos extensão + conteúdo.
        if (extension == ".py")
        {
            string content = await ReadLeadingTextAsync(filePath, maxChars: 4096, cancellationToken);
            bool looksLikePython = PythonContentHint.IsMatch(content);

            var result = new ArtifactIdentification
            {
                FilePath = filePath,
                Type = ArtifactType.Python,
                Confidence = looksLikePython ? 1.0 : 0.6
            };
            result.Notes.Add("Extensão \".py\".");
            result.Notes.Add(looksLikePython
                ? "Conteúdo inicial contém padrões típicos de Python (import/def/class/print)."
                : "Conteúdo inicial não confirmou padrões típicos de Python — extensão foi o único sinal.");
            return result;
        }

        return ArtifactIdentification.Unrecognized(
            filePath,
            $"Nenhuma assinatura binária conhecida e extensão \"{extension}\" não é suportada ainda (só .py, .jar, .dll nesta etapa).");
    }

    private static bool StartsWith(byte[] header, byte[] signature)
    {
        if (header.Length < signature.Length) return false;
        for (int i = 0; i < signature.Length; i++)
        {
            if (header[i] != signature[i]) return false;
        }
        return true;
    }

    private static async Task<byte[]> ReadHeaderAsync(string filePath, int byteCount, CancellationToken cancellationToken)
    {
        var buffer = new byte[byteCount];
        await using var stream = File.OpenRead(filePath);
        int read = await stream.ReadAsync(buffer.AsMemory(0, byteCount), cancellationToken);
        return read == byteCount ? buffer : buffer[..read];
    }

    private static async Task<string> ReadLeadingTextAsync(string filePath, int maxChars, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(filePath);
        var buffer = new char[maxChars];
        int read = await reader.ReadAsync(buffer.AsMemory(0, maxChars), cancellationToken);
        return new string(buffer, 0, read);
    }
}
