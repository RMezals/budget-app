using System.Globalization;
using Xunit;
using BudgetApp.Api.Modules.Dashboard.Services;
using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace BudgetApp.Tests.Dashboard;

public class AdvisorServiceTests
{
    private readonly Mock<IPortfolioService> _portfolioMock = new();
    private readonly Mock<ITransactionRepository> _txMock = new();
    private readonly Mock<IBudgetRepository> _budgetMock = new();
    private readonly Mock<ISavingsGoalRepository> _goalMock = new();
    private readonly Mock<ILogger<AdvisorService>> _loggerMock = new();

    public AdvisorServiceTests()
    {
        // AdvisorService uses :F2 which is culture-sensitive; pin to invariant
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        // Setup default portfolio data
        _portfolioMock.Setup(p => p.GetAllAsync(It.IsAny<string>()))
            .ReturnsAsync((new List<BudgetApp.Api.Modules.Portfolio.Models.Asset>(),
                          new List<BudgetApp.Api.Modules.Portfolio.Models.Liability>()));
        _portfolioMock.Setup(p => p.ComputeNetWorth(It.IsAny<List<BudgetApp.Api.Modules.Portfolio.Models.Asset>>(),
                                                      It.IsAny<List<BudgetApp.Api.Modules.Portfolio.Models.Liability>>(),
                                                      It.IsAny<DateTime>()))
            .Returns(new NetWorthSnapshot(0, 0, 0));
    }

    private AdvisorService CreateSut() =>
        new(_portfolioMock.Object, _txMock.Object, _budgetMock.Object, _goalMock.Object, _loggerMock.Object);

    [Fact]
    public async Task BuildFinancialSummaryAsync_IncludesFormattedIncomeAndExpenses()
    {
        _txMock.Setup(t => t.GetByMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([
                new Transaction { Amount = 3000m },
                new Transaction { Amount = -500m, Category = "Food" }
            ]);
        _budgetMock.Setup(b => b.GetByMonthAsync("user1", It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _goalMock.Setup(g => g.GetActiveByUserAsync("user1"))
            .ReturnsAsync([]);

        var summary = await CreateSut().BuildFinancialSummaryAsync("user1");

        Assert.Contains("Net worth: 0.00", summary);
        Assert.Contains("Monthly income: 3000.00", summary);
        Assert.Contains("Monthly expenses: 500.00", summary);
    }

    [Fact]
    public async Task BuildFinancialSummaryAsync_IncludesPortfolioData()
    {
        _portfolioMock.Setup(p => p.ComputeNetWorth(It.IsAny<List<BudgetApp.Api.Modules.Portfolio.Models.Asset>>(),
                                                      It.IsAny<List<BudgetApp.Api.Modules.Portfolio.Models.Liability>>(),
                                                      It.IsAny<DateTime>()))
            .Returns(new NetWorthSnapshot(15000m, 5000m, 10000m));
        _txMock.Setup(t => t.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _budgetMock.Setup(b => b.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _goalMock.Setup(g => g.GetActiveByUserAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var summary = await CreateSut().BuildFinancialSummaryAsync("user1");

        Assert.Contains("Net worth: 10000.00", summary);
        Assert.Contains("Total assets: 15000.00", summary);
        Assert.Contains("Total liabilities: 5000.00", summary);
    }

    [Fact]
    public async Task BuildFinancialSummaryAsync_GroupsSpendingByCategory()
    {
        _txMock.Setup(t => t.GetByMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([
                new Transaction { Amount = -100m, Category = "Food" },
                new Transaction { Amount = -50m,  Category = "Food" },
                new Transaction { Amount = -200m, Category = "Rent" }
            ]);
        _budgetMock.Setup(b => b.GetByMonthAsync("user1", It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _goalMock.Setup(g => g.GetActiveByUserAsync("user1"))
            .ReturnsAsync([]);

        var summary = await CreateSut().BuildFinancialSummaryAsync("user1");

        Assert.Contains("Food: 150.00", summary);
        Assert.Contains("Rent: 200.00", summary);
    }

    [Fact]
    public async Task BuildFinancialSummaryAsync_IncludesGoalAndBudgetCounts()
    {
        _txMock.Setup(t => t.GetByMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _budgetMock.Setup(b => b.GetByMonthAsync("user1", It.IsAny<DateTime>()))
            .ReturnsAsync([new Budget { Category = "Food", LimitAmount = 300m }]);
        _goalMock.Setup(g => g.GetActiveByUserAsync("user1"))
            .ReturnsAsync([
                new SavingsGoal { Name = "Emergency Fund", CurrentAmount = 500m, TargetAmount = 2000m },
                new SavingsGoal { Name = "Vacation",       CurrentAmount = 100m, TargetAmount = 1000m }
            ]);

        var summary = await CreateSut().BuildFinancialSummaryAsync("user1");

        Assert.Contains("Active savings goals: 2", summary);
        Assert.Contains("Budget limits set: 1", summary);
        Assert.Contains("Emergency Fund (500.00/2000.00)", summary);
        Assert.Contains("Vacation (100.00/1000.00)", summary);
    }

    [Fact]
    public async Task BuildFinancialSummaryAsync_EmptyData_ShowsZeroTotals()
    {
        _txMock.Setup(t => t.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _budgetMock.Setup(b => b.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync([]);
        _goalMock.Setup(g => g.GetActiveByUserAsync(It.IsAny<string>()))
            .ReturnsAsync([]);

        var summary = await CreateSut().BuildFinancialSummaryAsync("user1");

        Assert.Contains("Monthly income: 0.00", summary);
        Assert.Contains("Monthly expenses: 0.00", summary);
        Assert.Contains("Active savings goals: 0", summary);
        Assert.Contains("Budget limits set: 0", summary);
    }
}
