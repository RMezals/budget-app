using Xunit;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using BudgetApp.Api.Modules.Transactions.Services;
using Moq;

namespace BudgetApp.Tests.Transactions;

public class BudgetServiceTests
{
    private readonly Mock<IBudgetRepository> _budgetRepoMock = new();
    private readonly Mock<ITransactionRepository> _txRepoMock = new();
    private readonly BudgetService _service;

    public BudgetServiceTests()
    {
        _service = new BudgetService(_budgetRepoMock.Object, _txRepoMock.Object);
    }

    [Fact]
    public async Task GetUsageAsync_CorrectlySumsExpensesAndTransformsToAbsoluteValues()
    {
        string userId = "user1";
        int year = 2026;
        int month = 5;
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var mockBudgets = new List<Budget>
        {
            new() { UserId = userId, Category = "Food", LimitAmount = 100m, Date = monthStart }
        };

        var mockTransactions = new List<Transaction>
        {
            new() { UserId = userId, Category = "Food", Amount = -30.00m, Date = monthStart.AddDays(2) },
            new() { UserId = userId, Category = "Food", Amount = -15.50m, Date = monthStart.AddDays(5) },
            new() { UserId = userId, Category = "Food", Amount = 100.00m, Date = monthStart.AddDays(6) },
            new() { UserId = userId, Category = "Rent", Amount = -500.00m, Date = monthStart.AddDays(1) }
        };

        _budgetRepoMock.Setup(r => r.GetByMonthAsync(userId, monthStart)).ReturnsAsync(mockBudgets);
        _txRepoMock.Setup(r => r.GetByMonthAsync(userId, monthStart, monthEnd)).ReturnsAsync(mockTransactions);

        var result = await _service.GetUsageAsync(userId, year, month);

        Assert.Single(result);
        var foodSpending = result.First();
        
        Assert.Equal("Food", foodSpending.Category);
        Assert.Equal(100m, foodSpending.Limit);
        Assert.Equal(45.50m, foodSpending.Spent); 
    }
}