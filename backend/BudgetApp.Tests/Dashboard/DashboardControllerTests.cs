using Xunit;
using BudgetApp.Api.Modules.Dashboard;
using BudgetApp.Api.Modules.Dashboard.Models;
using BudgetApp.Api.Modules.Dashboard.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BudgetApp.Tests.Dashboard;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _dashboardMock = new();
    private readonly Mock<ISpendingTrendService> _trendMock = new();

    private DashboardController CreateController(string userId = "user1")
    {
        var controller = new DashboardController(_dashboardMock.Object, _trendMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Items["UserId"] = userId;
        return controller;
    }

    [Fact]
    public async Task GetSummary_ReturnsOkWithDashboardSummary()
    {
        var expected = new DashboardSummary { NetWorth = 5000m, MonthlyIncome = 2000m };
        _dashboardMock.Setup(s => s.GetSummaryAsync("user1")).ReturnsAsync(expected);

        var result = await CreateController().GetSummary();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetSummary_PassesUserIdFromHttpContextToService()
    {
        _dashboardMock.Setup(s => s.GetSummaryAsync("abc-123")).ReturnsAsync(new DashboardSummary());

        await CreateController("abc-123").GetSummary();

        _dashboardMock.Verify(s => s.GetSummaryAsync("abc-123"), Times.Once);
    }

    [Fact]
    public async Task GetSummary_MissingUserId_ThrowsUnauthorizedAccessException()
    {
        var controller = new DashboardController(_dashboardMock.Object, _trendMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // UserId not set
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetSummary());
    }

    [Fact]
    public async Task GetSpendingTrend_ReturnsOkWithTrendPoints()
    {
        var expected = new List<SpendingTrendPoint>
        {
            new() { Month = "2025-05", Expenses = new() { ["Food"] = 150m } },
        };
        _trendMock.Setup(s => s.GetSpendingTrendAsync("user1", 12)).ReturnsAsync(expected);

        var result = await CreateController().GetSpendingTrend(12);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetSpendingTrend_PassesUserIdAndMonthsToService()
    {
        _trendMock.Setup(s => s.GetSpendingTrendAsync("user1", 6))
                  .ReturnsAsync(new List<SpendingTrendPoint>());

        await CreateController().GetSpendingTrend(6);

        _trendMock.Verify(s => s.GetSpendingTrendAsync("user1", 6), Times.Once);
    }
}
