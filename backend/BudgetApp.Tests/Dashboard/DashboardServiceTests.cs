using Xunit;
using BudgetApp.Api.Modules.Dashboard.Services;
using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using Moq;

namespace BudgetApp.Tests.Dashboard;

public class DashboardServiceTests
{
    private readonly Mock<IPortfolioService> _portfolioMock = new();
    private readonly Mock<ITransactionRepository> _txMock = new();
    private readonly Mock<IBudgetRepository> _budgetMock = new();
    private readonly Mock<ISavingsGoalRepository> _goalMock = new();

    private DashboardService CreateSut() =>
        new(_portfolioMock.Object, _txMock.Object, _budgetMock.Object, _goalMock.Object);

    private void SetupEmptyPortfolio(NetWorthSnapshot? snapshot = null)
    {
        _portfolioMock.Setup(p => p.GetAllAsync(It.IsAny<string>()))
            .ReturnsAsync((new List<Asset>(), new List<Liability>()));
        _portfolioMock.Setup(p => p.ComputeNetWorth(
                It.IsAny<List<Asset>>(), It.IsAny<List<Liability>>(), It.IsAny<DateTime>()))
            .Returns(snapshot ?? new NetWorthSnapshot(0, 0, 0));
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsNetWorthFromPortfolio()
    {
        SetupEmptyPortfolio(new NetWorthSnapshot(TotalAssets: 5000m, TotalLiabilities: 1000m, NetWorth: 4000m));
        _txMock.Setup(t => t.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _budgetMock.Setup(b => b.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _goalMock.Setup(g => g.GetActiveByUserAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var result = await CreateSut().GetSummaryAsync("user1");

        Assert.Equal(4000m, result.NetWorth);
        Assert.Equal(5000m, result.TotalInvested);
    }

    [Fact]
    public async Task GetSummaryAsync_SumsIncomeAndExpensesSeparately()
    {
        SetupEmptyPortfolio();
        _txMock.Setup(t => t.GetByMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([
                new Transaction { Amount = 2000m },
                new Transaction { Amount = 500m },
                new Transaction { Amount = -300m },
                new Transaction { Amount = -150m }
            ]);
        _budgetMock.Setup(b => b.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _goalMock.Setup(g => g.GetActiveByUserAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var result = await CreateSut().GetSummaryAsync("user1");

        Assert.Equal(2500m, result.MonthlyIncome);
        Assert.Equal(450m, result.MonthlyExpenses);
    }

    [Fact]
    public async Task GetSummaryAsync_CalculatesBudgetSpentPerCategory()
    {
        SetupEmptyPortfolio();
        _txMock.Setup(t => t.GetByMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([
                new Transaction { Amount = -80m, Category = "Food" },
                new Transaction { Amount = -20m, Category = "Food" },
                new Transaction { Amount = 500m, Category = "Food" },   // income – must NOT count
                new Transaction { Amount = -50m, Category = "Transport" }
            ]);
        _budgetMock.Setup(b => b.GetByMonthAsync("user1", It.IsAny<DateTime>()))
            .ReturnsAsync([
                new Budget { Category = "Food", LimitAmount = 200m },
                new Budget { Category = "Transport", LimitAmount = 100m }
            ]);
        _goalMock.Setup(g => g.GetActiveByUserAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var result = await CreateSut().GetSummaryAsync("user1");

        var food = result.BudgetUsage.Single(b => b.Category == "Food");
        Assert.Equal(200m, food.Limit);
        Assert.Equal(100m, food.Spent);
        Assert.Equal(50m, food.UsagePercent);

        var transport = result.BudgetUsage.Single(b => b.Category == "Transport");
        Assert.Equal(50m, transport.Spent);
    }

    [Fact]
    public async Task GetSummaryAsync_BudgetWithNoMatchingTransactions_HasZeroSpent()
    {
        SetupEmptyPortfolio();
        _txMock.Setup(t => t.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([new Transaction { Amount = -100m, Category = "Entertainment" }]);
        _budgetMock.Setup(b => b.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync([new Budget { Category = "Food", LimitAmount = 300m }]);
        _goalMock.Setup(g => g.GetActiveByUserAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var result = await CreateSut().GetSummaryAsync("user1");

        Assert.Equal(0m, result.BudgetUsage.Single().Spent);
    }

    [Fact]
    public async Task GetSummaryAsync_TotalSavedIsSumOfActiveGoalCurrentAmounts()
    {
        SetupEmptyPortfolio();
        _txMock.Setup(t => t.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _budgetMock.Setup(b => b.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _goalMock.Setup(g => g.GetActiveByUserAsync("user1"))
            .ReturnsAsync([
                new SavingsGoal { Id = "g1", Name = "Vacation", CurrentAmount = 300m, TargetAmount = 1000m },
                new SavingsGoal { Id = "g2", Name = "Car", CurrentAmount = 700m, TargetAmount = 5000m }
            ]);

        var result = await CreateSut().GetSummaryAsync("user1");

        Assert.Equal(1000m, result.TotalSaved);
        Assert.Equal(2, result.ActiveGoals.Count);
        Assert.Equal(30m, result.ActiveGoals.Single(g => g.Name == "Vacation").PercentReached);
    }

    [Fact]
    public async Task GetSummaryAsync_EmptyData_ReturnsAllZeros()
    {
        SetupEmptyPortfolio();
        _txMock.Setup(t => t.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _budgetMock.Setup(b => b.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _goalMock.Setup(g => g.GetActiveByUserAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var result = await CreateSut().GetSummaryAsync("user1");

        Assert.Equal(0m, result.NetWorth);
        Assert.Equal(0m, result.MonthlyIncome);
        Assert.Equal(0m, result.MonthlyExpenses);
        Assert.Empty(result.BudgetUsage);
        Assert.Empty(result.ActiveGoals);
    }
}
