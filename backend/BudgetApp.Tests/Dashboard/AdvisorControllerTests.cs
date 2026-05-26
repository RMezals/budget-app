using Xunit;
using BudgetApp.Api.Modules.Dashboard;
using BudgetApp.Api.Modules.Dashboard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace BudgetApp.Tests.Dashboard;

public class AdvisorControllerTests
{
    private readonly Mock<IAdvisorService> _advisorServiceMock = new();
    private readonly Mock<IAiAdvisor> _claudeMock = new();
    private readonly Mock<IAiAdvisor> _ollamaMock = new();
    private readonly Mock<IAiAdvisorFactory> _factoryMock = new();
    private readonly Mock<ILogger<AdvisorController>> _loggerMock = new();

    private AdvisorController CreateController(string userId = "user1")
    {
        _advisorServiceMock
            .Setup(s => s.BuildFinancialSummaryAsync(It.IsAny<string>()))
            .ReturnsAsync("financial summary");

        // Setup factory to return the appropriate mock
        _factoryMock.Setup(f => f.GetAdvisor(AiProviders.Claude)).Returns(_claudeMock.Object);
        _factoryMock.Setup(f => f.GetAdvisor(AiProviders.Ollama)).Returns(_ollamaMock.Object);

        var controller = new AdvisorController(
            _advisorServiceMock.Object, _factoryMock.Object, _loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Items["UserId"] = userId;
        return controller;
    }

    [Fact]
    public async Task Analyse_DefaultProvider_RoutesToOllama()
    {
        _ollamaMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("tips");

        await CreateController().Analyse(new AnalyseRequest());

        _ollamaMock.Verify(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
        _claudeMock.Verify(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Analyse_ProviderClaude_RoutesToClaudeAdvisor()
    {
        _claudeMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("tips");

        await CreateController().Analyse(new AnalyseRequest("claude"));

        _claudeMock.Verify(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
        _ollamaMock.Verify(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Analyse_KnownGoals_TranslatesToHumanReadableDescriptions()
    {
        string? capturedGoals = null;
        _ollamaMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string, string?>((_, goals, _) => capturedGoals = goals)
            .ReturnsAsync("tips");

        await CreateController().Analyse(
            new AnalyseRequest("ollama", ["save_more", "pay_debt"]));

        Assert.Contains("save more money", capturedGoals);
        Assert.Contains("pay off debt", capturedGoals);
    }

    [Fact]
    public async Task Analyse_NoGoals_UsesDefaultHealthGoal()
    {
        string? capturedGoals = null;
        _ollamaMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string, string?>((_, goals, _) => capturedGoals = goals)
            .ReturnsAsync("tips");

        await CreateController().Analyse(new AnalyseRequest("ollama", null));

        Assert.Equal("improve their overall financial health", capturedGoals);
    }

    [Fact]
    public async Task Analyse_EmptyGoalsList_UsesDefaultHealthGoal()
    {
        string? capturedGoals = null;
        _ollamaMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string, string?>((_, goals, _) => capturedGoals = goals)
            .ReturnsAsync("tips");

        await CreateController().Analyse(new AnalyseRequest("ollama", []));

        Assert.Equal("improve their overall financial health", capturedGoals);
    }

    [Fact]
    public async Task Analyse_UnknownGoalKey_PassedThroughAsIs()
    {
        string? capturedGoals = null;
        _ollamaMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string, string?>((_, goals, _) => capturedGoals = goals)
            .ReturnsAsync("tips");

        await CreateController().Analyse(
            new AnalyseRequest("ollama", ["unknown_goal"]));

        Assert.Contains("unknown_goal", capturedGoals);
    }

    [Fact]
    public async Task Analyse_SuccessfulAnalysis_ReturnsOkWithProviderAndTips()
    {
        _ollamaMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync("3 great tips");

        var result = await CreateController().Analyse(new AnalyseRequest("ollama"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = ok.Value!;
        Assert.Equal("ollama", value.GetType().GetProperty("Provider")?.GetValue(value)?.ToString());
        Assert.Equal("3 great tips", value.GetType().GetProperty("Tips")?.GetValue(value)?.ToString());
    }

    [Fact]
    public async Task Analyse_AdvisorThrowsInvalidOperation_Returns503WithErrorMessage()
    {
        _ollamaMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("Service not configured."));

        var result = await CreateController().Analyse(new AnalyseRequest());

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, status.StatusCode);
    }

    [Fact]
    public async Task Analyse_PassesFinancialSummaryFromAdvisorServiceToAiAdvisor()
    {
        // CreateController registers the default It.IsAny setup first;
        // the specific "user1" setup below is registered after, so it wins in Moq.
        var controller = CreateController();
        _advisorServiceMock
            .Setup(s => s.BuildFinancialSummaryAsync("user1"))
            .ReturnsAsync("my personal summary");

        string? capturedSummary = null;
        _ollamaMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string, string?>((summary, _, _) => capturedSummary = summary)
            .ReturnsAsync("tips");

        await controller.Analyse(new AnalyseRequest());

        Assert.Equal("my personal summary", capturedSummary);
    }

    [Fact]
    public async Task Analyse_WithUserApiKey_PassesKeyToAdvisor()
    {
        string? capturedApiKey = null;
        _claudeMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string, string?>((_, _, apiKey) => capturedApiKey = apiKey)
            .ReturnsAsync("tips");

        await CreateController().Analyse(new AnalyseRequest("claude", null, "user-key-123"));

        Assert.Equal("user-key-123", capturedApiKey);
    }

    [Fact]
    public async Task Analyse_WithoutApiKey_PassesNullToAdvisor()
    {
        string? capturedApiKey = "not-null";
        _ollamaMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string, string?>((_, _, apiKey) => capturedApiKey = apiKey)
            .ReturnsAsync("tips");

        await CreateController().Analyse(new AnalyseRequest("ollama", null, null));

        Assert.Null(capturedApiKey);
    }

    [Fact]
    public async Task Analyse_ClaudeWithUserKey_UsesProvidedKey()
    {
        string? capturedApiKey = null;
        _claudeMock.Setup(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string, string?>((_, _, apiKey) => capturedApiKey = apiKey)
            .ReturnsAsync("tips");

        await CreateController().Analyse(
            new AnalyseRequest("claude", ["save_more"], "sk-ant-test-key"));

        Assert.Equal("sk-ant-test-key", capturedApiKey);
        _claudeMock.Verify(a => a.AnalyseAsync(It.IsAny<string>(), It.IsAny<string>(), "sk-ant-test-key"), Times.Once);
    }
}
