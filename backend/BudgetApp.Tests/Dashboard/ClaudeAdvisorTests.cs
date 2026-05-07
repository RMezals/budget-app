using Xunit;
using BudgetApp.Api.Modules.Dashboard.Services;
using BudgetApp.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BudgetApp.Tests.Dashboard;

public class ClaudeAdvisorTests
{
    private static IConfiguration BuildConfig(
        string? apiKey = "test-key",
        string? model = null,
        string? maxTokens = null)
    {
        var values = new Dictionary<string, string?> { ["Anthropic:ApiKey"] = apiKey };
        if (model != null)     values["Anthropic:Model"]     = model;
        if (maxTokens != null) values["Anthropic:MaxTokens"] = maxTokens;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static IHttpClientFactory CreateFactory(string responseJson, Action<HttpRequestMessage>? inspect = null)
    {
        var client = new HttpClient(new FakeHttpHandler(responseJson, inspect));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        return factory.Object;
    }

    [Fact]
    public async Task AnalyseAsync_ParsesTextFromAnthropicResponse()
    {
        const string json = """{"content":[{"type":"text","text":"Tip 1. Tip 2. Tip 3."}]}""";

        var result = await new ClaudeAdvisor(CreateFactory(json), BuildConfig())
            .AnalyseAsync("financial summary", "save more money");

        Assert.Equal("Tip 1. Tip 2. Tip 3.", result);
    }

    [Fact]
    public async Task AnalyseAsync_MissingApiKey_ThrowsInvalidOperationException()
    {
        var advisor = new ClaudeAdvisor(new Mock<IHttpClientFactory>().Object, BuildConfig(apiKey: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => advisor.AnalyseAsync("summary", "goals"));
    }

    [Fact]
    public async Task AnalyseAsync_SendsCorrectUrlAndAuthHeaders()
    {
        const string json = """{"content":[{"type":"text","text":"tips"}]}""";
        HttpRequestMessage? captured = null;

        await new ClaudeAdvisor(CreateFactory(json, req => captured = req), BuildConfig("my-key"))
            .AnalyseAsync("summary", "invest");

        Assert.NotNull(captured);
        Assert.Equal("https://api.anthropic.com/v1/messages", captured!.RequestUri?.ToString());
        Assert.Contains("my-key", captured.Headers.GetValues("x-api-key"));
        Assert.Contains("2023-06-01", captured.Headers.GetValues("anthropic-version"));
    }

    [Fact]
    public async Task AnalyseAsync_UsesConfiguredModelAndMaxTokensInPayload()
    {
        const string json = """{"content":[{"type":"text","text":"tips"}]}""";
        string? capturedBody = null;

        await new ClaudeAdvisor(
                CreateFactory(json, req => capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult()),
                BuildConfig("key", model: "claude-opus-4-7", maxTokens: "512"))
            .AnalyseAsync("summary", "goals");

        Assert.Contains("claude-opus-4-7", capturedBody);
        Assert.Contains("512", capturedBody);
    }

    [Fact]
    public async Task AnalyseAsync_DefaultModel_UsedWhenModelNotConfigured()
    {
        const string json = """{"content":[{"type":"text","text":"tips"}]}""";
        string? capturedBody = null;

        await new ClaudeAdvisor(
                CreateFactory(json, req => capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult()),
                BuildConfig())   // no model override
            .AnalyseAsync("summary", "goals");

        Assert.Contains("claude-haiku-4-5", capturedBody);
    }
}
