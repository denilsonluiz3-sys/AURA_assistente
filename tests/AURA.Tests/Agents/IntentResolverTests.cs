using AURA.Agents;

using Xunit;

namespace AURA.Tests.Agents;

public sealed class IntentResolverTests
{
    private readonly HeuristicIntentResolver _resolver = new();

    [Theory]
    [InlineData("pesquise como instalar python", "search")]
    [InlineData("execute /tmp/test.sh", "execute")]
    [InlineData("crie um arquivo teste.txt", "create_file")]
    [InlineData("liste os arquivos", "list_files")]
    [InlineData("qual a bateria", "android_battery")]
    [InlineData("mostre os sensores", "android_sensor")]
    [InlineData("qual minha localização gps", "android_location")]
    [InlineData("abra a camera", "android_camera")]
    public void Resolve_ShouldMapKnownCommands(string command, string expectedIntent)
    {
        IntentResult result = _resolver.Resolve(command);
        Assert.Equal(expectedIntent, result.Intent);
        Assert.True(result.Confidence > 0.5);
    }

    [Fact]
    public void Resolve_ShouldFallbackToConversation()
    {
        IntentResult result = _resolver.Resolve("olá aura");
        Assert.Equal("conversar", result.Intent);
    }
}
