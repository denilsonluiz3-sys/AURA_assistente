using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AURA.Core.Abstractions;

namespace AURA.AI
{
    /// <summary>
    /// Pesquisa na web e extrai o código necessário para executar uma tarefa.
    /// Usa o LLM quando há chave de API; senão, fallback heurístico.
    /// </summary>
    public sealed class CodeExtractorTool : AgentTool
    {
        private readonly OpenRouterClient? _client;
        private readonly IWebSearch _webSearch;
        private const string SystemPrompt =
            "Você é um assistente especializado em extrair código de exemplos da web. " +
            "Receba o conteúdo de uma página web e o pedido do usuário, e retorne APENAS o código necessário " +
            "para executar a tarefa, sem explicações extras. " +
            "Responda apenas com o código, sem markdown.";

        public CodeExtractorTool(IWebSearch webSearch, OpenRouterClient? client = null)
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
                ["task"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Descrição da tarefa a ser executada"
                },
                ["language"] = new AgentToolParameter
                {
                    Type = "string",
                    Description = "Linguagem de programação (python, bash, csharp)"
                }
            },
            Required = { "task" }
        };

        public override async Task<string> ExecuteAsync(string argumentsJson, CancellationToken ct = default)
        {
            string task = "";
            string language = "python";
            using (JsonDocument doc = JsonDocument.Parse(argumentsJson))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("task", out var t)) task = t.GetString() ?? "";
                if (root.TryGetProperty("language", out var l)) language = l.GetString() ?? "python";
            }

            if (string.IsNullOrWhiteSpace(task))
                return "ERRO: task vazia.";

            string searchQuery = $"{task} {language} exemplo código";
            string searchResults = await _webSearch.SearchWithRefinementAsync(searchQuery, ct);

            if (string.IsNullOrWhiteSpace(searchResults))
                return "ERRO: nenhum resultado encontrado.";

            string code = await ExtractCodeWithAIAsync(task, searchResults, language, ct);
            if (string.IsNullOrWhiteSpace(code))
                code = ExtractCodeWithHeuristics(searchResults, language);

            if (string.IsNullOrWhiteSpace(code))
                return "ERRO: não foi possível extrair código.";

            return $"```{language}\n{code}\n```\n\nExecute com: {(language == "python" ? "python3" : language)}";
        }

        private async Task<string> ExtractCodeWithAIAsync(string task, string content, string language, CancellationToken ct)
        {
            if (_client == null || string.IsNullOrEmpty(_client.Options.ApiKey))
                return string.Empty;

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

        private string ExtractCodeWithHeuristics(string content, string language)
        {
            string pattern = $@"```{language}\s*([\s\S]*?)```";
            var match = Regex.Match(content, pattern, RegexOptions.Multiline);
            if (match.Success && match.Groups[1].Success)
                return match.Groups[1].Value.Trim();
            return string.Empty;
        }
    }
}