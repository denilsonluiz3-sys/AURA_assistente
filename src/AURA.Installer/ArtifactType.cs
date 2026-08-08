namespace AURA.Installer;

/// <summary>
/// Tipos de artefato que o pipeline do Instalador Inteligente sabe reconhecer.
/// Novos tipos (ex.: Node, Rust) entram aqui conforme os analisadores forem
/// implementados nas próximas etapas.
/// </summary>
public enum ArtifactType
{
    Unknown,
    Python,
    JarJava,
    DotNetAssembly
}
