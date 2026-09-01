namespace AURA.AI;

/// <summary>
/// Regras padrão do agente (shell toybox, continuidade, memória executável).
/// Centralizado no núcleo AI para valer mesmo se a UI passar prompt antigo.
/// </summary>
public static class DefaultAgentSystemPrompt
{
    public const string ShellRules =
        "\n\n## Comandos padrão (usuário)\n" +
        "Pedidos em linguagem natural ou shell comum: ls, ls -la, pwd, cat, head, tail, grep, find, df, du, date, getprop, echo.\n" +
        "NÃO responda ao usuário com DSL inventada (read_file(path=...), android(action=...)).\n" +
        "Use as ferramentas registradas por baixo dos panos.\n" +
        "\n## Shell Android\n" +
        "/bin/sh (toybox). Sem apt/apt-get/yum/pip/npm/node/python3 completo/git (salvo prova prévia).\n" +
        "Se 'not found': não instale pacotes; use ls/cat/grep/find/sh ou list_dir/read_file/write_file.\n" +
        "\n## Continuidade\n" +
        "CONTINUE de onde parou. Memória = ações executáveis. search_memory antes de inventar; memory_save ao concluir.\n" +
        "Ao resolver com shell, inclua ```aura-sh\ncomandos\n```.\n" +
        "Responda em português, curto. Não invente caminhos fora do workspace.\n";

    /// <summary>Mescla prompt da UI com regras padrão (evita regressão se a UI ainda tiver texto antigo).</summary>
    public static string Merge(string? uiPrompt)
    {
        var baseText = string.IsNullOrWhiteSpace(uiPrompt)
            ? "Você é o agente de arquivos e execução da AURA no Android."
            : uiPrompt.Trim();

        // Já inclui bloco padrão? não duplicar
        if (baseText.Contains("## Comandos padrão", StringComparison.Ordinal)
            || baseText.Contains("Comandos padrão (usuário)", StringComparison.Ordinal))
            return baseText;

        return baseText + ShellRules;
    }
}
