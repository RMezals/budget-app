using BudgetApp.Api.Modules.Savings.Models;
using Moq;
using Xunit;

namespace BudgetApp.Tests.Savings;

public class SavingsServiceBalanceAndAbandonTests : SavingsServiceTestBase
{
    [Fact]
    public async Task RecalculateBalanceAsync_UpdatesBalanceAndDerivedStatus()
    {
        GoalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync(new SavingsGoal
            {
                Id = "g1",
                UserId = "user1",
                Name = "Trip",
                TargetAmount = 100m,
                Status = GoalStatus.Active
            });
        ContributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 40m },
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 60m }
            ]);

        await CreateSut().RecalculateBalanceAsync("g1", "user1");

        GoalMock.Verify(g => g.UpdateBalanceAsync("g1", "user1", 100m, GoalStatus.Completed), Times.Once);
    }

    [Fact]
    public async Task RecalculateBalanceAsync_UpdatesBalanceWithoutStatusWhenGoalMissing()
    {
        GoalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync((SavingsGoal?)null);
        ContributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 55m }
            ]);

        await CreateSut().RecalculateBalanceAsync("g1", "user1");

        GoalMock.Verify(g => g.UpdateBalanceAsync("g1", "user1", 55m, null), Times.Once);
    }

    [Fact]
    public async Task AbandonGoalAsync_WithdrawsCurrentBalanceAndMarksGoalAbandoned()
    {
        var abandonedOn = new DateTime(2026, 5, 21);
        GoalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync(new SavingsGoal
            {
                Id = "g1",
                UserId = "user1",
                Name = "Trip",
                TargetAmount = 500m,
                CurrentAmount = 999m,
                Status = GoalStatus.Paused
            });
        ContributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 200m },
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = -25m }
            ]);

        await CreateSut().AbandonGoalAsync(
            "g1",
            "user1",
            abandonedOn,
            "Goal abandoned",
            "Withdrew saved amount before abandoning.");

        ContributionMock.Verify(c => c.InsertAsync(It.Is<GoalContribution>(contribution =>
            contribution.GoalId == "g1" &&
            contribution.UserId == "user1" &&
            contribution.Amount == -175m &&
            contribution.Date == abandonedOn &&
            contribution.Reason == "Goal abandoned" &&
            contribution.Description == "Withdrew saved amount before abandoning." &&
            contribution.BalanceAfter == 0m)), Times.Once);
        GoalMock.Verify(g => g.UpdateBalanceAsync("g1", "user1", 0m, GoalStatus.Abandoned), Times.Once);
    }

    [Fact]
    public async Task AbandonGoalAsync_MarksGoalAbandonedWithoutWithdrawalWhenBalanceIsZero()
    {
        GoalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync(new SavingsGoal
            {
                Id = "g1",
                UserId = "user1",
                Name = "Trip",
                TargetAmount = 500m,
                Status = GoalStatus.Active
            });
        ContributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
            .ReturnsAsync([]);

        await CreateSut().AbandonGoalAsync(
            "g1",
            "user1",
            new DateTime(2026, 5, 21),
            "Goal abandoned",
            null);

        ContributionMock.Verify(c => c.InsertAsync(It.IsAny<GoalContribution>()), Times.Never);
        GoalMock.Verify(g => g.UpdateBalanceAsync("g1", "user1", 0m, GoalStatus.Abandoned), Times.Once);
    }
}
