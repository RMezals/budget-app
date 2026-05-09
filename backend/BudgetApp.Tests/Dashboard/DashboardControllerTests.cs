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
    private static DashboardController CreateController(IDashboardService service, string userId = "user1")
    {
        var controller = new DashboardController(service);
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
        var service = new Mock<IDashboardService>();
        service.Setup(s => s.GetSummaryAsync("user1")).ReturnsAsync(expected);

        var result = await CreateController(service.Object).GetSummary();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetSummary_PassesUserIdFromHttpContextToService()
    {
        var service = new Mock<IDashboardService>();
        service.Setup(s => s.GetSummaryAsync("abc-123")).ReturnsAsync(new DashboardSummary());

        await CreateController(service.Object, "abc-123").GetSummary();

        service.Verify(s => s.GetSummaryAsync("abc-123"), Times.Once);
    }

    [Fact]
    public async Task GetSummary_MissingUserId_ThrowsUnauthorizedAccessException()
    {
        var controller = new DashboardController(new Mock<IDashboardService>().Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()   // UserId not set
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetSummary());
    }
}
