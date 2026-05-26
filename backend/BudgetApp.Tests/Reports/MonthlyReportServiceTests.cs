using Xunit;
using BudgetApp.Api.Modules.Reports.Services;
using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using Moq;

namespace BudgetApp.Tests.Reports;

public class MonthlyReportServiceTests
{
    private readonly Mock<ITransactionRepository> _txMock = new();
    private readonly Mock<ISavingsGoalRepository> _goalMock = new();
    private readonly Mock<IGoalContributionRepository> _contributionMock = new();
    private readonly Mock<IPortfolioService> _portfolioMock = new();

    public MonthlyReportServiceTests()
    {
        _txMock.Setup(t => t.GetByMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
               .ReturnsAsync([]);
        _goalMock.Setup(g => g.GetByUserAsync(It.IsAny<string>()))
                 .ReturnsAsync([]);
        _contributionMock.Setup(c => c.GetByUserAndMonthAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync([]);
        _portfolioMock.Setup(p => p.GetAllAsync(It.IsAny<string>()))
                      .ReturnsAsync((new List<Asset>(), new List<Liability>()));
        _portfolioMock.Setup(p => p.ComputeNetWorth(It.IsAny<List<Asset>>(), It.IsAny<List<Liability>>(), It.IsAny<DateTime>()))
                      .Returns(new NetWorthSnapshot(0, 0, 0));
    }

    private MonthlyReportService CreateSut() =>
        new(_txMock.Object, _goalMock.Object, _contributionMock.Object, _portfolioMock.Object);

    [Fact]
    public async Task GetMonthlyReportAsync_SetsYearAndMonth()
    {
        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 3);

        Assert.Equal(2024, result.Year);
        Assert.Equal(3, result.Month);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_SumsIncomeCorrectly()
    {
        _txMock.Setup(t => t.GetByMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
               .ReturnsAsync([
                   new Transaction { Amount = 2000m, Category = "Salary" },
                   new Transaction { Amount = 500m, Category = "Freelance" },
                   new Transaction { Amount = -300m, Category = "Food" }
               ]);

        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        Assert.Equal(2500m, result.TotalIncome);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_SumsExpensesAsPositiveValue()
    {
        _txMock.Setup(t => t.GetByMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
               .ReturnsAsync([
                   new Transaction { Amount = 2000m, Category = "Salary" },
                   new Transaction { Amount = -300m, Category = "Food" },
                   new Transaction { Amount = -150m, Category = "Transport" }
               ]);

        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        Assert.Equal(450m, result.TotalExpenses);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_GroupsExpensesByCategory()
    {
        _txMock.Setup(t => t.GetByMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
               .ReturnsAsync([
                   new Transaction { Amount = -80m, Category = "Food" },
                   new Transaction { Amount = -20m, Category = "Food" },
                   new Transaction { Amount = -50m, Category = "Transport" }
               ]);

        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        Assert.Equal(100m, result.ExpensesByCategory["Food"]);
        Assert.Equal(50m, result.ExpensesByCategory["Transport"]);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_IncomeTransactionsDoNotAppearInExpensesByCategory()
    {
        _txMock.Setup(t => t.GetByMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
               .ReturnsAsync([
                   new Transaction { Amount = 3000m, Category = "Salary" },
                   new Transaction { Amount = -100m, Category = "Food" }
               ]);

        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        Assert.False(result.ExpensesByCategory.ContainsKey("Salary"));
        Assert.Single(result.ExpensesByCategory);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_GroupsIncomeByCategory()
    {
        _txMock.Setup(t => t.GetByMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
               .ReturnsAsync([
                   new Transaction { Amount = 3000m, Category = "Salary" },
                   new Transaction { Amount = 500m, Category = "Freelance" },
                   new Transaction { Amount = -100m, Category = "Food" }
               ]);

        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        Assert.Equal(3000m, result.IncomeByCategory["Salary"]);
        Assert.Equal(500m, result.IncomeByCategory["Freelance"]);
        Assert.False(result.IncomeByCategory.ContainsKey("Food"));
    }

    [Fact]
    public async Task GetMonthlyReportAsync_SplitsContributionDepositsAndWithdrawals()
    {
        _goalMock.Setup(g => g.GetByUserAsync("user1"))
                 .ReturnsAsync([new SavingsGoal { Id = "g1", Name = "Vacation" }]);
        _contributionMock.Setup(c => c.GetByUserAndMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync([
                             new GoalContribution { GoalId = "g1", Amount = 200m },
                             new GoalContribution { GoalId = "g1", Amount = 100m },
                             new GoalContribution { GoalId = "g1", Amount = -50m }
                         ]);

        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        var summary = result.SavingsContributions.Single();
        Assert.Equal(300m, summary.TotalDeposited);
        Assert.Equal(50m, summary.TotalWithdrawn);
        Assert.Equal(250m, summary.NetContribution);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_ContributionCount_IncludesDepositsAndWithdrawals()
    {
        _goalMock.Setup(g => g.GetByUserAsync("user1"))
                 .ReturnsAsync([new SavingsGoal { Id = "g1", Name = "Emergency Fund" }]);
        _contributionMock.Setup(c => c.GetByUserAndMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync([
                             new GoalContribution { GoalId = "g1", Amount = 100m },
                             new GoalContribution { GoalId = "g1", Amount = -30m },
                             new GoalContribution { GoalId = "g1", Amount = 50m }
                         ]);

        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        Assert.Equal(3, result.SavingsContributions.Single().ContributionCount);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_OrdersContributionsByNetContributionDescending()
    {
        _goalMock.Setup(g => g.GetByUserAsync("user1"))
                 .ReturnsAsync([
                     new SavingsGoal { Id = "g1", Name = "Car" },
                     new SavingsGoal { Id = "g2", Name = "House" },
                     new SavingsGoal { Id = "g3", Name = "Vacation" }
                 ]);
        _contributionMock.Setup(c => c.GetByUserAndMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync([
                             new GoalContribution { GoalId = "g1", Amount = 100m },   // net 100
                             new GoalContribution { GoalId = "g2", Amount = 500m },   // net 500
                             new GoalContribution { GoalId = "g3", Amount = 250m }    // net 250
                         ]);

        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        Assert.Equal("House", result.SavingsContributions[0].GoalName);
        Assert.Equal("Vacation", result.SavingsContributions[1].GoalName);
        Assert.Equal("Car", result.SavingsContributions[2].GoalName);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_ResolvesGoalNameFromGoalList()
    {
        _goalMock.Setup(g => g.GetByUserAsync("user1"))
                 .ReturnsAsync([new SavingsGoal { Id = "g1", Name = "Emergency Fund" }]);
        _contributionMock.Setup(c => c.GetByUserAndMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync([new GoalContribution { GoalId = "g1", Amount = 100m }]);

        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        Assert.Equal("Emergency Fund", result.SavingsContributions.Single().GoalName);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_FallsBackToUnknownGoalWhenGoalNotFound()
    {
        _goalMock.Setup(g => g.GetByUserAsync("user1")).ReturnsAsync([]);
        _contributionMock.Setup(c => c.GetByUserAndMonthAsync("user1", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync([new GoalContribution { GoalId = "deleted-goal", Amount = 50m }]);

        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        Assert.Equal("Unknown Goal", result.SavingsContributions.Single().GoalName);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_PortfolioChange_PastMonth_UsesMonthEndMinusOneSecondAsEndReference()
    {
        var monthStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedEndRef = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(-1);

        _portfolioMock.Setup(p => p.ComputeNetWorth(It.IsAny<List<Asset>>(), It.IsAny<List<Liability>>(), monthStart))
                      .Returns(new NetWorthSnapshot(TotalAssets: 1000m, TotalLiabilities: 0, NetWorth: 1000m));
        _portfolioMock.Setup(p => p.ComputeNetWorth(It.IsAny<List<Asset>>(), It.IsAny<List<Liability>>(), expectedEndRef))
                      .Returns(new NetWorthSnapshot(TotalAssets: 1200m, TotalLiabilities: 0, NetWorth: 1200m));

        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        Assert.Equal(1000m, result.PortfolioChange.StartValue);
        Assert.Equal(1200m, result.PortfolioChange.EndValue);
    }

    [Fact]
    public async Task GetMonthlyReportAsync_PortfolioChange_CurrentMonth_UsesNowAsEndReference()
    {
        var now = DateTime.UtcNow;
        var capturedDates = new List<DateTime>();

        _portfolioMock.Setup(p => p.ComputeNetWorth(It.IsAny<List<Asset>>(), It.IsAny<List<Liability>>(), It.IsAny<DateTime>()))
                      .Callback<List<Asset>, List<Liability>, DateTime>((_, _, dt) => capturedDates.Add(dt))
                      .Returns(new NetWorthSnapshot(0, 0, 0));

        await CreateSut().GetMonthlyReportAsync("user1", now.Year, now.Month);

        // capturedDates[0] = monthStart, capturedDates[1] = endReference
        var endReference = capturedDates[1];
        var monthEnd = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        Assert.True(endReference < monthEnd, "End reference must be before month end for current month");
        Assert.True(endReference >= now.AddSeconds(-5), "End reference must be close to now");
    }

    [Fact]
    public async Task GetMonthlyReportAsync_EmptyData_ReturnsZerosAndEmptyCollections()
    {
        var result = await CreateSut().GetMonthlyReportAsync("user1", 2024, 1);

        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(0m, result.TotalExpenses);
        Assert.Empty(result.ExpensesByCategory);
        Assert.Empty(result.IncomeByCategory);
        Assert.Empty(result.SavingsContributions);
        Assert.Equal(0m, result.PortfolioChange.StartValue);
        Assert.Equal(0m, result.PortfolioChange.EndValue);
    }
}
