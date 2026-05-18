using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Savings.Services;
using Moq;
using Xunit;

namespace BudgetApp.Tests.Savings;

public class SavingsServiceTests
{
    private readonly Mock<ISavingsGoalRepository> _goalMock = new();
    private readonly Mock<IGoalContributionRepository> _contributionMock = new();

    private SavingsService CreateSut() => new(_goalMock.Object, _contributionMock.Object);

    [Fact]
    public async Task AddContributionAsync_WithdrawalUsesComputedContributionBalance()
    {
        _goalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync(new SavingsGoal
            {
                Id = "g1",
                UserId = "user1",
                Name = "Trip",
                TargetAmount = 500m,
                CurrentAmount = 999m,
                Status = GoalStatus.Active
            });
        _contributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 100m },
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = -25m }
            ]);

        var contribution = await CreateSut().AddContributionAsync(
            "g1",
            "user1",
            -50m,
            new DateTime(2026, 5, 14),
            "Withdrawal",
            "Emergency cash");

        Assert.Equal(-50m, contribution.Amount);
        Assert.Equal(25m, contribution.BalanceAfter);
        Assert.Equal("Withdrawal", contribution.Reason);
        Assert.Equal("Emergency cash", contribution.Description);
        _goalMock.Verify(g => g.UpdateBalanceAsync("g1", "user1", 25m, null), Times.Once);
    }

    [Fact]
    public async Task AddContributionAsync_RejectsWithdrawalAboveComputedContributionBalance()
    {
        _goalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync(new SavingsGoal
            {
                Id = "g1",
                UserId = "user1",
                Name = "Trip",
                TargetAmount = 500m,
                CurrentAmount = 999m,
                Status = GoalStatus.Active
            });
        _contributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 100m }
            ]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().AddContributionAsync(
                "g1",
                "user1",
                -150m,
                new DateTime(2026, 5, 14),
                null,
                null));

        _contributionMock.Verify(c => c.InsertAsync(It.IsAny<GoalContribution>()), Times.Never);
        _goalMock.Verify(g => g.UpdateBalanceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<GoalStatus?>()), Times.Never);
    }

    [Fact]
    public async Task GetGoalProgressAsync_ReturnsComputedProgressFromContributions()
    {
        _goalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync(new SavingsGoal
            {
                Id = "g1",
                UserId = "user1",
                Name = "Trip",
                TargetAmount = 500m,
                CurrentAmount = 999m,
                Status = GoalStatus.Completed,
                Deadline = new DateTime(2026, 12, 31),
                Description = "Portugal"
            });
        _contributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 200m },
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = -50m }
            ]);

        var result = await CreateSut().GetGoalProgressAsync("g1", "user1");

        Assert.NotNull(result);
        Assert.Equal("g1", result.Id);
        Assert.Equal("Trip", result.Name);
        Assert.Equal(150m, result.CurrentBalance);
        Assert.Equal(30m, result.PercentReached);
        Assert.Equal(350m, result.AmountRemaining);
        Assert.Equal(GoalStatus.Active, result.Status);
        Assert.Equal(new DateTime(2026, 12, 31), result.Deadline);
        Assert.Equal("Portugal", result.Description);
    }

    [Fact]
    public async Task GetGoalProgressListAsync_BatchLoadsContributionsAndComputesEachGoal()
    {
        _goalMock.Setup(g => g.GetByUserAsync("user1"))
            .ReturnsAsync([
                new SavingsGoal { Id = "g1", UserId = "user1", Name = "Trip", TargetAmount = 500m },
                new SavingsGoal { Id = "g2", UserId = "user1", Name = "Car", TargetAmount = 1000m }
            ]);
        _contributionMock.Setup(c => c.GetByGoalsAsync(
                It.Is<List<string>>(ids => ids.SequenceEqual(new[] { "g1", "g2" })),
                "user1"))
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 200m },
                new GoalContribution { GoalId = "g2", UserId = "user1", Amount = 300m },
                new GoalContribution { GoalId = "g2", UserId = "user1", Amount = -100m }
            ]);

        var result = await CreateSut().GetGoalProgressListAsync("user1");

        Assert.Equal(2, result.Count);
        Assert.Equal(200m, result.Single(g => g.Id == "g1").CurrentBalance);
        Assert.Equal(200m, result.Single(g => g.Id == "g2").CurrentBalance);
        _contributionMock.Verify(c => c.GetByGoalAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
