namespace AURA.Mobile.Services;

/// <summary>
/// "Prompt geral" offline: o usuário só informa o objetivo;
/// esta tabela devolve os comandos Android (toybox) em bloco aura-sh.
/// Não usa API key nem busca web — a web não gera comandos confiáveis.
/// </summary>
public static class LocalCommandRecipes
{
    /// <summary>
    /// null = nenhuma receita; string = resposta com ```aura-sh``` para o app executar.
    /// </summary>
    public static string? TryResolve(string objective)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return null;

        string q = Normalize(objective);

        // Listar arquivos / workspace
        if (MatchAny(q, "listar arquivo", "liste arquivo", "listar o workspace",
                "liste o workspace", "arquivos do workspace", "arquivos no workspace",
                "ls workspace", "mostrar arquivo", "mostre arquivo", "ver arquivo",
                "conteudo da pasta", "conteúdo da pasta", "list dir", "listdir"))
        {
            return Recipe("Listar arquivos do workspace",
                "ls -la\n" +
                "pwd");
        }

        // Espaço em disco
        if (MatchAny(q, "espaco em disco", "espaço em disco", "espaco livre",
                "espaço livre", "df ", "armazenamento", "storage"))
        {
            return Recipe("Espaço em disco",
                "df -h");
        }

        // Memória RAM
        if (MatchAny(q, "memoria ram", "memória ram", "uso de memoria",
                "uso de memória", "free -m", "quanto de ram"))
        {
            return Recipe("Memória RAM",
                "cat /proc/meminfo | head -20");
        }

        // Data/hora
        if (MatchAny(q, "que horas", "que dia", "data e hora", "data atual", "hora atual"))
        {
            return Recipe("Data e hora",
                "date");
        }

        // Processos
        if (MatchAny(q, "listar processo", "liste processo", "processos em execucao",
                "processos em execução", "ps -a"))
        {
            return Recipe("Processos",
                "ps -A | head -40");
        }

        // Propriedades do sistema
        if (MatchAny(q, "propriedades do sistema", "getprop", "info do dispositivo",
                "modelo do celular", "versao do android", "versão do android"))
        {
            return Recipe("Propriedades do sistema",
                "getprop ro.product.model\n" +
                "getprop ro.build.version.release\n" +
                "getprop ro.product.manufacturer");
        }

        // Logs recentes do app em Download/AURA
        if (MatchAny(q, "logs recentes", "ultimos logs", "últimos logs",
                "listar logs", "logs do aura"))
        {
            return Recipe("Logs recentes em Download/AURA",
                "ls -lt /sdcard/Download/AURA/aura_*.log.txt 2>/dev/null | head -15");
        }

        // Limpar process-log corrompido (pedido comum)
        if (MatchAny(q, "limpar process-log", "apagar process-log", "remover process-log"))
        {
            return Recipe("Remover process-log se existir",
                "rm -f process-log.json\n" +
                "rm -f /data/user/0/com.aura.genesis/files/process-log.json 2>/dev/null\n" +
                "echo process-log removido ou inexistente");
        }

        // Status resumido do ambiente
        if (MatchAny(q, "status do sistema", "diagnostico rapido", "diagnóstico rápido",
                "status do aparelho", "relatorio rapido", "relatório rápido"))
        {
            return Recipe("Diagnóstico rápido",
                "echo === PWD ===\n" +
                "pwd\n" +
                "echo === DISCO ===\n" +
                "df -h /sdcard 2>/dev/null | tail -1\n" +
                "echo === ANDROID ===\n" +
                "getprop ro.build.version.release\n" +
                "getprop ro.product.model");
        }

        return null;
    }

    private static string Recipe(string title, string shellScript)
    {
        return
            "[receita local · sem IA · sem API]\n" +
            title + "\n\n" +
            "```aura-sh\n" +
            shellScript.TrimEnd() + "\n" +
            "```";
    }

    private static string Normalize(string s)
    {
        s = s.Trim().ToLowerInvariant();
        // remove pontuação leve
        foreach (char c in new[] { '?', '!', '.', ',', ';', ':', '"', '\'' })
            s = s.Replace(c, ' ');
        while (s.Contains("  ", StringComparison.Ordinal))
            s = s.Replace("  ", " ", StringComparison.Ordinal);
        return s.Trim();
    }

    private static bool MatchAny(string q, params string[] phrases)
    {
        foreach (string p in phrases)
        {
            if (q.Contains(p, StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
