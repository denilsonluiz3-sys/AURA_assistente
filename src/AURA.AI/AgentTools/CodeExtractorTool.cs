using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Abstractions;
using AURA.AI.UniversalAI;

namespace AURA.AI
{
    /// <summary>Pesquisa na web e extrai código usando opcionalmente o cliente universal.</summary>
    public sealed class CodeExtractorTool : AgentTool
    {
        private readonly IUniversalAiClient? _client;
        private readonly IWebSearch _webSearch;
        private const string SystemPrompt = "Você é um assistente especializado em extrair código de exemplos da web. Receba o conteúdo de uma página web e o pedido do usuário, e retorne APENAS o código necessário para executar a tarefa, sem explicações extras. Responda apenas com o código, sem markdown.";

        public CodeExtractorTool(IWebSearch webSearch, IUniversalAiClient? client = null)
        {
            _webSearch = webSearch ?? throw new ArgumentNullException(nameof(webSearch));
            _client = client;
        }

        public override AgentToolDefinition Definition => new AgentToolDefinition
        {
            Name = "extract_code",
            Description = "Pesquisa na web e extrai o código necessário para executar uma tarefa.",
            Parameters =
            {
                ["task"] = new AgentToolParameter { Type = "string", Description = "Descrição da tarefa a ser executada" },
                ["language"] = new AgentToolParameter { Type = "string", Description = "Linguagem de programação (python, bash, csharp)" }
            },
            Required = { "task" }
        };

        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string task = "";
            string language = "python";
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("task", out JsonElement t)) task = t.GetString() ?? "";
                if (root.TryGetProperty("language", out JsonElement l)) language = l.GetString() ?? "python";
            }
            if (string.IsNullOrWhiteSpace(task)) return "ERRO: task vazia.";

            string searchResults = await _webSearch.SearchWithRefinementAsync($"{task} {language} exemplo código", ct);
            if (string.IsNullOrWhiteSpace(searchResults)) return "ERRO: nenhum resultado encontrado.";

            string code = await ExtractCodeWithAIAsync(task, searchResults, language, ct);
            if (string.IsNullOrWhiteSpace(code)) code = ExtractCodeWithHeuristics(searchResults, language);
            if (string.IsNullOrWhiteSpace(code)) return "ERRO: não foi possível extrair código.";
            return $"```{language}\n{code}\n```\n\nExecute com: {(language == "python" ? "python3" : language)}";
        }

        private async Task<string> ExtractCodeWithAIAsync(string task, string content, string language, CancellationToken ct)
        {
            if (_client == null || string.IsNullOrWhiteSpace(_client.Options.ApiKey)) return string.Empty;
            try
            {
                string prompt = $"Tarefa: {task}\nLinguagem: {language}\n\nConteúdo:\n{content}\n\nExtraia APENAS o código:";
                string result = await _client.ChatAsync(prompt, systemPrompt: SystemPrompt, ct: ct);
                if (string.IsNullOrWhiteSpace(result)) return string.Empty;
                result = Regex.Replace(result, @"```\w*", "");
                result = Regex.Replace(result, @"```", "");
                return result.Trim();
            }
            catch { return string.Empty; }
        }

        private static string ExtractCodeWithHeuristics(string content, string language)
        {
            Match match = Regex.Match(content, $@"```{Regex.Escape(language)}\s*([\s\S]*?)```", RegexOptions.Multiline);
            return match.Success && match.Groups[1].Success ? match.Groups[1].Value.Trim() : string.Empty;
        }
    }
}