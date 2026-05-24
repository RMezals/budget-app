using Xunit;
using BudgetApp.Api.Modules.Dashboard.Services;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace BudgetApp.Tests.Dashboard;

public class SpendingTrendServiceTests
{
    private readonly Mock<ITransactionRepository> _txMock = new();
    private readonly Mock<ILogger<SpendingTrendService>> _loggerMock = new();

    private SpendingTrendService CreateSut() =>
        new(_txMock.Object, _loggerMock.Object);

    private void SetupTransactions(params Transaction[] txs) =>
        _txMock.Setup(t => t.GetByRangeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
               .ReturnsAsync(txs.ToList());

    [Fact]
    public async Task GetSpendingTrendAsync_ReturnsOnePointPerMonth()
    {
        SetupTransactions();

        var result = await CreateSut().GetSpendingTrendAsync("user1", 12);

        Assert.Equal(12, result.Count);
    }

    [Fact]
    public async Task GetSpendingTrendAsync_AllMonthsPresent_EvenWithNoData()
    {
        SetupTransactions();
        var now = DateTime.UtcNow;

        var result = await CreateSut().GetSpendingTrendAsync("user1", 6);

        // First month must be 5 months before the current month
        var expectedFirst = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-5)
            .ToString("yyyy-MM");

        Assert.Equal(expectedFirst, result[0].Month);
        Assert.Equal(now.ToString("yyyy-MM"), result[5].Month);
        Assert.All(result, p => Assert.Empty(p.Expenses));
    }

    [Fact]
    public async Task GetSpendingTrendAsync_GroupsExpensesByCategory()
    {
        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 15, 0, 0, 0, DateTimeKind.Utc);

        SetupTransactions(
            new Transaction { Amount = -100m, Category = "Food", Date = thisMonth },
            new Transaction { Amount = -50m, Category = "Food", Date = thisMonth },
            new Transaction { Amount = -200m, Category = "Housing", Date = thisMonth });

        var result = await CreateSut().GetSpendingTrendAsync("user1", 12);

        var current = result.Single(p => p.Month == thisMonth.ToString("yyyy-MM"));
        Assert.Equal(150m, current.Expenses["Food"]);
        Assert.Equal(200m, current.Expenses["Housing"]);
    }

    [Fact]
    public async Task GetSpendingTrendAsync_ExcludesIncomeTransactions()
    {
        var now = DateTime.UtcNow;
        SetupTransactions(
            new Transaction { Amount = 5000m, Category = "Salary", Date = now },
            new Transaction { Amount = 500m, Category = "Freelance", Date = now });

        var result = await CreateSut().GetSpendingTrendAsync("user1", 12);

        Assert.All(result, p => Assert.Empty(p.Expenses));
    }

    [Fact]
    public async Task GetSpendingTrendAsync_ExpensesAreStoredAsPositiveValues()
    {
        var now = DateTime.UtcNow;
        SetupTransactions(
            new Transaction { Amount = -350m, Category = "Transport", Date = now });

        var result = await CreateSut().GetSpendingTrendAsync("user1", 1);

        Assert.Equal(350m, result[0].Expenses["Transport"]);
    }

    [Fact]
    public async Task GetSpendingTrendAsync_TransactionsInDifferentMonthsAreSeparated()
    {
        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 10, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = thisMonth.AddMonths(-1);

        SetupTransactions(
            new Transaction { Amount = -100m, Category = "Food", Date = thisMonth },
            new Transaction { Amount = -200m, Category = "Food", Date = lastMonth });

        var result = await CreateSut().GetSpendingTrendAsync("user1", 2);

        Assert.Equal(100m, result[1].Expenses["Food"]); // current month
        Assert.Equal(200m, result[0].Expenses["Food"]); // previous month
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSpendingTrendAsync_InvalidUserId_ThrowsArgumentException(string? userId)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            CreateSut().GetSpendingTrendAsync(userId!, 12));
    }

    [Fact]
    public async Task GetSpendingTrendAsync_MonthsAboveMax_ClampsTo24()
    {
        SetupTransactions();

        var result = await CreateSut().GetSpendingTrendAsync("user1", 100);

        Assert.Equal(24, result.Count);
    }

    [Fact]
    public async Task GetSpendingTrendAsync_ZeroMonths_ClampsTo1()
    {
        SetupTransactions();

        var result = await CreateSut().GetSpendingTrendAsync("user1", 0);

        Assert.Single(result);
    }
}
