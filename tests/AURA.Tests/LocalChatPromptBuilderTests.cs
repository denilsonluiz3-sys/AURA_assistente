using AURA.AI;
using AURA.AI.UniversalAI;
using Xunit;

namespace AURA.Tests;

public sealed class LocalChatPromptBuilderTests
{
    [Fact]
    public void BuildsMessagesAndToolSchemaWithoutPathsOrSecrets()
    {
        var tool = new AgentToolDefinition
        {
            Name = "create_task",
            Description = "Cria uma tarefa local."
        };
        tool.Parameters["title"] = new AgentToolParameter
        {
            Type = "string",
            Description = "Título da tarefa."
        };
        tool.Required.Add("title");

        string prompt = LocalChatPromptBuilder.Build(
            new[] { new AgentMessage { Role = "user", Content = "Crie uma tarefa" } },
            new[] { tool });

        Assert.Contains("[USER]", prompt);
        Assert.Contains("create_task", prompt);
        Assert.Contains("tool_call", prompt);
        Assert.Contains("Crie uma tarefa", prompt);
        Assert.DoesNotContain("FileSystem.AppDataDirectory", prompt);
    }

    [Fact]
    public void IncludesPreviousToolResultForTheNextRound()
    {
        string prompt = LocalChatPromptBuilder.Build(
            new[]
            {
                new AgentMessage
                {
                    Role = "assistant",
                    ToolCalls = new List<AgentToolCall>
                    {
                        new() { Name = "list_tasks", ArgumentsJson = "{}" }
                    }
                },
                new AgentMessage { Role = "tool", ToolCallId = "call-1", Content = "tarefa pendente" }
            },
            Array.Empty<AgentToolDefinition>());

        Assert.Contains("tool_call list_tasks: {}", prompt);
        Assert.Contains("tarefa pendente", prompt);
    }
}
