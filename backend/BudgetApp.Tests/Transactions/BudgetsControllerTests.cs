using Xunit;
using BudgetApp.Api.Modules.Transactions;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using BudgetApp.Api.Modules.Transactions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BudgetApp.Tests.Transactions;

public class BudgetsControllerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<IBudgetService> _budgetServiceMock = new();

    private BudgetsController CreateController(string userId = "user1")
    {
        var controller = new BudgetsController(_budgetRepoMock.Object, _budgetServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Items["UserId"] = userId;
        return controller;
    }

    [Fact]
    public async Task GetByMonth_ReturnsOkWithBudgets()
    {
        int year = 2026;
        int month = 5;
        var expectedMonthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var mockBudgets = new List<Budget> 
        { 
            new() { Id = "b1", UserId = "user1", Category = "Food", LimitAmount = 300m, Date = expectedMonthStart } 
        };

        _budgetRepoMock.Setup(r => r.GetByMonthAsync("user1", expectedMonthStart))
            .ReturnsAsync(mockBudgets);

        var result = await CreateController("user1").GetByMonth(year, month);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedBudgets = Assert.IsAssignableFrom<List<Budget>>(okResult.Value);
        Assert.Single(returnedBudgets);
    }

    [Fact]
    public async Task Upsert_ReturnsNoContent_WhenCategoryIsValid()
    {
        string validCategory = Categories.Expense.FirstOrDefault() ?? "Food";
        var request = new UpsertBudgetRequest(2026, 5, validCategory, 500m);
        var expectedMonthStart = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await CreateController("user1").Upsert(request);

        Assert.IsType<NoContentResult>(result);
        _budgetRepoMock.Verify(r => r.UpsertAsync("user1", validCategory, expectedMonthStart, 500m), Times.Once);
    }

    [Fact]
    public async Task Upsert_ReturnsBadRequest_WhenCategoryIsInvalid()
    {
        var request = new UpsertBudgetRequest(2026, 5, "InvalidCategoryName", 500m);

        var result = await CreateController("user1").Upsert(request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        _budgetRepoMock.Verify(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task GetUsage_ReturnsOkWithCalculatedPercentages()
    {
        int year = 2026;
        int month = 5;
        var mockSpending = new List<BudgetSpending>
        {
            new("Food", 200m, 50m),
            new("Transport", 0m, 20m)
        };

        _budgetServiceMock.Setup(s => s.GetUsageAsync("user1", year, month))
            .ReturnsAsync(mockSpending);

        var result = await CreateController("user1").GetUsage(year, month);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var usageList = Assert.IsAssignableFrom<IEnumerable<BudgetUsageResponse>>(okResult.Value).ToList();

        Assert.Equal(2, usageList.Count);
        
        Assert.Equal("Food", usageList[0].Category);
        Assert.Equal(150m, usageList[0].Remaining);
        Assert.Equal(25m, usageList[0].UsagePercent);

        Assert.Equal("Transport", usageList[1].Category);
        Assert.Equal(0m, usageList[1].UsagePercent);
    }
}