using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AURA.AI;
using AURA.AI.Providers;
using Xunit;

namespace AURA.Tests;

public class ApiKeyProviderResolverTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        private HttpRequestMessage? _lastRequest;

        public HttpRequestMessage? LastRequest => _lastRequest;

        public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken _)
        {
            _lastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }

    private static ProviderHealthResult RunProbe(HttpStatusCode status, out bool sentXApiKey)
    {
        var handler = new FakeHandler(_ =>
            new HttpResponseMessage(status) { Content = new StringContent("{}") });

        using var http = new HttpClient(handler);
        var resolver = new ApiKeyProviderResolver();
        var credential = new ProviderCredential("sk-or-test-key", allowProbe: true)
        {
            PreferredProviderName = "OpenRouter"
        };

        ProviderHealthResult result = resolver.ValidateAsync(credential, http).GetAwaiter().GetResult();
        sentXApiKey = handler.LastRequest?.Headers.Contains("Authorization") == true;
        return result;
    }

    [Fact]
    public void Detect_OpenRouterPrefix_ResolvesOpenRouter()
    {
        var resolver = new ApiKeyProviderResolver();
        ProviderDetectionResult r = resolver.Detect(new ProviderCredential("sk-or-abc123"));

        Assert.True(r.IsConclusive);
        Assert.Equal("OpenRouter", r.Provider!.Name);
        Assert.Equal(ProviderDetectionSource.KeyFormat, r.Source);
    }

    [Fact]
    public void Detect_GroqPrefix_ResolvesGroq()
    {
        var resolver = new ApiKeyProviderResolver();
        ProviderDetectionResult r = resolver.Detect(new ProviderCredential("gsk_abc123"));

        Assert.True(r.IsConclusive);
        Assert.Equal("Groq (grátis)", r.Provider!.Name);
    }

    [Fact]
    public void Detect_GeminiPrefix_ResolvesGemini()
    {
        var resolver = new ApiKeyProviderResolver();
        ProviderDetectionResult r = resolver.Detect(new ProviderCredential("AIzaSyD1234567890abc"));

        Assert.True(r.IsConclusive);
        Assert.Equal("Google Gemini", r.Provider!.Name);
    }

    [Fact]
    public void Detect_GeminiNewFormatPrefix_ResolvesGemini()
    {
        var resolver = new ApiKeyProviderResolver();
        ProviderDetectionResult r = resolver.Detect(new ProviderCredential("AQ.Ab8RN6fakekeyforunit-tests00000000000000000"));

        Assert.True(r.IsConclusive);
        Assert.Equal("Google Gemini", r.Provider!.Name);
    }

    [Fact]
    public void Detect_UnknownPrefix_IsInconclusive_ButNotRejected()
    {
        // Regra 2: não rejeitar chave só porque o prefixo é desconhecido.
        var resolver = new ApiKeyProviderResolver();
        ProviderDetectionResult r = resolver.Detect(new ProviderCredential("xyz-unknown-prefix-9"));

        Assert.False(r.IsConclusive);
        Assert.Null(r.Provider);
        Assert.True(r.Candidates.Count > 0);
    }

    [Fact]
    public void Detect_UnknownPrefix_WithPreferredContext_UsesContext()
    {
        // Regra 6: quando o formato não identifica, usa o contexto/configuração.
        var resolver = new ApiKeyProviderResolver();
        var credential = new ProviderCredential("custom-key-without-prefix")
        {
            PreferredProviderName = "Google Gemini"
        };
        ProviderDetectionResult r = resolver.Detect(credential);

        Assert.True(r.IsConclusive);
        Assert.Equal("Google Gemini", r.Provider!.Name);
    }

    [Fact]
    public void Detect_EmptyKey_IsInconclusive()
    {
        var resolver = new ApiKeyProviderResolver();
        ProviderDetectionResult r = resolver.Detect(new ProviderCredential(""));

        Assert.False(r.IsConclusive);
        Assert.Null(r.Provider);
    }

    [Fact]
    public void Validate_WithoutAllowProbe_DoesNotSendKey()
    {
        // Regra 7: nunca enviar a chave a terceiros sem autorização explícita.
        var handler = new FakeHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));
        using var http = new HttpClient(handler);
        var resolver = new ApiKeyProviderResolver();
        var credential = new ProviderCredential("x9-unknown-format-zzz", allowProbe: false);

        ProviderHealthResult result = resolver.ValidateAsync(credential, http).GetAwaiter().GetResult();

        Assert.Equal(ProviderHealthStatus.UnknownProvider, result.Status);
        Assert.Null(handler.LastRequest); // nada foi enviado à rede
    }

    [Fact]
    public void Validate_ValidKey_ReturnsValid()
    {
        var resolver = new ApiKeyProviderResolver();
        var credential = new ProviderCredential("sk-or-secret-key", allowProbe: true)
        {
            PreferredProviderName = "OpenRouter"
        };
        using var http = new HttpClient(new FakeHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"data\":[]}") }));

        ProviderHealthResult r = resolver.ValidateAsync(credential, http).GetAwaiter().GetResult();

        Assert.Equal(ProviderHealthStatus.Valid, r.Status);
        Assert.Equal("OpenRouter", r.Provider!.Name);
    }

    [Fact]
    public void Validate_Unauthorized_ReturnsUnauthorized()
    {
        ProviderHealthResult r = RunProbe(HttpStatusCode.Unauthorized, out bool sentXApiKey);

        Assert.Equal(ProviderHealthStatus.Unauthorized, r.Status);
        Assert.True(sentXApiKey, "Chave deveria ir no header de autenticação");
        Assert.DoesNotContain("sk-or-test-key", r.Message);
    }

    [Fact]
    public void Validate_InsufficientCredits_OnQuota()
    {
        ProviderHealthResult r = RunProbe((HttpStatusCode)429, out _);

        Assert.Equal(ProviderHealthStatus.InsufficientCredits, r.Status);
    }

    [Fact]
    public void Validate_ServerError_ReturnsProviderUnavailable()
    {
        ProviderHealthResult r = RunProbe(HttpStatusCode.InternalServerError, out _);

        Assert.Equal(ProviderHealthStatus.ProviderUnavailable, r.Status);
    }

    [Fact]
    public void Validate_NetworkError_ReturnsProviderUnavailable()
    {
        var handler = new FakeHandler(_ => throw new HttpRequestException("boom"));
        using var http = new HttpClient(handler);
        var resolver = new ApiKeyProviderResolver();
        var credential = new ProviderCredential("sk-or-net-fail", allowProbe: true)
        {
            PreferredProviderName = "OpenRouter"
        };

        ProviderHealthResult r = resolver.ValidateAsync(credential, http).GetAwaiter().GetResult();

        Assert.Equal(ProviderHealthStatus.ProviderUnavailable, r.Status);
    }

    [Fact]
    public void Probe_AmbiguousKey_FindsProvider()
    {
        // Regra 6: formato ambíguo (sk- casa OpenAI e DeepSeek) + AllowProbe
        // testa os compatíveis até achar o que valida.
        var handler = new FakeHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"data\":[]}") });
        using var http = new HttpClient(handler);
        var resolver = new ApiKeyProviderResolver();
        var credential = new ProviderCredential("sk-amb-123", allowProbe: true);

        ProviderDetectionResult r = resolver.ResolveAsync(credential, http).GetAwaiter().GetResult();

        Assert.True(r.IsConclusive);
        Assert.NotNull(r.Provider);
    }

    [Fact]
    public void Resolve_ConclusiveByFormat_ValidatesAndAppliesClient()
    {
        var handler = new FakeHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"data\":[]}") });
        using var http = new HttpClient(handler);
        var resolver = new ApiKeyProviderResolver();
        var credential = new ProviderCredential("sk-or-apply-key", allowProbe: true);

        ProviderDetectionResult result = resolver.ResolveAsync(credential, http).GetAwaiter().GetResult();
        Assert.True(result.IsConclusive);

        var client = new OpenRouterClient(new OpenRouterOptions { ApiKey = "sk-or-apply-key" });
        resolver.ApplyToClient(client, result);

        Assert.Equal("https://openrouter.ai/api/v1/chat/completions", client.Options.BaseUrl);
        Assert.Equal("Authorization", client.Options.AuthHeaderName);
        Assert.Equal("Bearer ", client.Options.AuthScheme);
        Assert.Equal(AiApiFormat.OpenAICompletions, client.Options.ApiFormat);
        Assert.False(string.IsNullOrWhiteSpace(client.Options.Model));
    }

    [Fact]
    public void ApplyToClient_KeepsNeverLoggingTheKey()
    {
        var client = new OpenRouterClient(new OpenRouterOptions { ApiKey = "sk-secret-x123" });
        var resolver = new ApiKeyProviderResolver();
        ProviderDetectionResult result = resolver.Detect(new ProviderCredential("sk-or-k"));

        string serialized = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.DoesNotContain("sk-secret-x123", serialized);
        Assert.DoesNotContain("sk-or-k", serialized);
    }
}
