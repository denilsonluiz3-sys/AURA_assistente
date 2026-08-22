using System.Text.Json;
using AURA.Mobile.Diagnostics;

namespace AURA.Mobile.Services;

/// <summary>Prompt pronto do agente: título, descrição e texto a enviar.</summary>
public sealed class AgentPromptItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool BuiltIn { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Catálogo de prompts (built-in + personalizados em arquivo JSON).</summary>
public static class AgentPromptStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string StorePath =>
        Path.Combine(FileSystem.AppDataDirectory, "agent-prompts.json");

    public static IReadOnlyList<AgentPromptItem> BuiltIns { get; } = new List<AgentPromptItem>
    {
        new()
        {
            Id = "builtin-list",
            Title = "Listar workspace",
            Description = "Mostra a estrutura de pastas do diretório de trabalho.",
            Body = "Liste os arquivos e pastas do workspace (list_dir na raiz) e resuma o que encontrou.",
            BuiltIn = true
        },
        new()
        {
            Id = "builtin-status",
            Title = "Status do projeto",
            Description = "Resumo do projeto vinculado e do workspace ativo.",
            Body = "Descreva o status do projeto: workspace ativo, se está vinculado, quantos arquivos há e o que posso fazer agora.",
            BuiltIn = true
        },
        new()
        {
            Id = "builtin-readme",
            Title = "Ler README",
            Description = "Abre e resume o README do workspace, se existir.",
            Body = "Procure um README.md (ou similar) no workspace, leia e faça um resumo curto em português.",
            BuiltIn = true
        },
        new()
        {
            Id = "builtin-fix",
            Title = "Corrigir erro",
            Description = "Peça análise e correção de um erro que você descrever.",
            Body = "Analise o seguinte erro e proponha a correção mais segura no workspace:\n\n",
            BuiltIn = true
        },
        new()
        {
            Id = "builtin-memory",
            Title = "O que você lembra?",
            Description = "Consulta a memória persistente recente.",
            Body = "Com base na sua memória persistente, resuma o que já conversamos e o que está em andamento.",
            BuiltIn = true
        }
    };

    public static List<AgentPromptItem> LoadAll()
    {
        var list = new List<AgentPromptItem>(BuiltIns);
        try
        {
            if (!File.Exists(StorePath))
                return list;

            string json = File.ReadAllText(StorePath);
            var custom = JsonSerializer.Deserialize<List<AgentPromptItem>>(json, JsonOpts);
            if (custom == null)
                return list;

            foreach (var item in custom)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Title))
                    continue;
                item.BuiltIn = false;
                list.Add(item);
            }
        }
        catch (Exception ex)
        {
            AuraLog.Exception("AgentPromptStore.LoadAll", ex);
        }

        return list;
    }

    public static void SaveCustom(IEnumerable<AgentPromptItem> customOnly)
    {
        try
        {
            var payload = customOnly
                .Where(p => p != null && !p.BuiltIn && !string.IsNullOrWhiteSpace(p.Title))
                .Select(p =>
                {
                    p.UpdatedAtUtc = DateTimeOffset.UtcNow;
                    return p;
                })
                .ToList();

            string dir = Path.GetDirectoryName(StorePath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(StorePath, JsonSerializer.Serialize(payload, JsonOpts));
        }
        catch (Exception ex)
        {
            AuraLog.Exception("AgentPromptStore.SaveCustom", ex);
        }
    }

    public static void AddCustom(string title, string description, string body)
    {
        var all = LoadAll();
        var custom = all.Where(p => !p.BuiltIn).ToList();
        custom.Add(new AgentPromptItem
        {
            Title = title.Trim(),
            Description = (description ?? string.Empty).Trim(),
            Body = (body ?? string.Empty).Trim(),
            BuiltIn = false
        });
        SaveCustom(custom);
    }

    public static void DeleteCustom(string id)
    {
        var custom = LoadAll().Where(p => !p.BuiltIn && p.Id != id).ToList();
        SaveCustom(custom);
    }
}
