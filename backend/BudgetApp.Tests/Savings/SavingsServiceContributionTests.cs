using BudgetApp.Api.Modules.Savings.Models;
using Moq;
using Xunit;

namespace BudgetApp.Tests.Savings;

public class SavingsServiceContributionTests : SavingsServiceTestBase
{
    [Fact]
    public async Task AddContributionAsync_WithdrawalUsesComputedContributionBalance()
    {
        GoalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync(new SavingsGoal
            {
                Id = "g1",
                UserId = "user1",
                Name = "Trip",
                TargetAmount = 500m,
                CurrentAmount = 999m,
                Status = GoalStatus.Active
            });
        ContributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
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
        GoalMock.Verify(g => g.UpdateBalanceAsync("g1", "user1", 25m, null), Times.Once);
    }

    [Fact]
    public async Task AddContributionAsync_RejectsWithdrawalWithoutReason()
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
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 100m }
            ]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().AddContributionAsync(
                "g1",
                "user1",
                -50m,
                new DateTime(2026, 5, 14),
                " ",
                null));

        Assert.Equal("Enter a withdrawal reason.", ex.Message);
        ContributionMock.Verify(c => c.InsertAsync(It.IsAny<GoalContribution>()), Times.Never);
        GoalMock.Verify(g => g.UpdateBalanceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<GoalStatus?>()), Times.Never);
    }

    [Fact]
    public async Task AddContributionAsync_RejectsWithdrawalAboveComputedContributionBalance()
    {
        GoalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync(new SavingsGoal
            {
                Id = "g1",
                UserId = "user1",
                Name = "Trip",
                TargetAmount = 500m,
                CurrentAmount = 999m,
                Status = GoalStatus.Active
            });
        ContributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
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

        ContributionMock.Verify(c => c.InsertAsync(It.IsAny<GoalContribution>()), Times.Never);
        GoalMock.Verify(g => g.UpdateBalanceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<GoalStatus?>()), Times.Never);
    }

    [Fact]
    public async Task AddContributionAsync_RejectsPausedGoal()
    {
        GoalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync(new SavingsGoal
            {
                Id = "g1",
                UserId = "user1",
                Name = "Trip",
                TargetAmount = 500m,
                Status = GoalStatus.Paused
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().AddContributionAsync(
                "g1",
                "user1",
                50m,
                new DateTime(2026, 5, 14),
                "Deposit",
                null));

        Assert.Equal("Resume the goal before adding contributions or withdrawals.", ex.Message);
        ContributionMock.Verify(c => c.GetByGoalAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        ContributionMock.Verify(c => c.InsertAsync(It.IsAny<GoalContribution>()), Times.Never);
        GoalMock.Verify(g => g.UpdateBalanceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<GoalStatus?>()), Times.Never);
    }

    [Fact]
    public async Task AddContributionAsync_RejectsAbandonedGoal()
    {
        GoalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync(new SavingsGoal
            {
                Id = "g1",
                UserId = "user1",
                Name = "Trip",
                TargetAmount = 500m,
                Status = GoalStatus.Abandoned
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().AddContributionAsync(
                "g1",
                "user1",
                50m,
                new DateTime(2026, 5, 14),
                "Deposit",
                null));

        Assert.Equal("Abandoned goals cannot accept contributions or withdrawals.", ex.Message);
        ContributionMock.Verify(c => c.GetByGoalAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        ContributionMock.Verify(c => c.InsertAsync(It.IsAny<GoalContribution>()), Times.Never);
        GoalMock.Verify(g => g.UpdateBalanceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<GoalStatus?>()), Times.Never);
    }

    [Fact]
    public async Task AddContributionAsync_RejectsZeroAmount()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().AddContributionAsync(
                "g1",
                "user1",
                0m,
                new DateTime(2026, 5, 14),
                "Deposit",
                null));

        Assert.Equal("Contribution amount cannot be zero.", ex.Message);
        GoalMock.Verify(g => g.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        ContributionMock.Verify(c => c.InsertAsync(It.IsAny<GoalContribution>()), Times.Never);
    }

    [Fact]
    public async Task AddContributionAsync_RejectsDefaultDate()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().AddContributionAsync(
                "g1",
                "user1",
                25m,
                default,
                "Deposit",
                null));

        Assert.Equal("Enter a valid contribution date.", ex.Message);
        GoalMock.Verify(g => g.GetByIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        ContributionMock.Verify(c => c.InsertAsync(It.IsAny<GoalContribution>()), Times.Never);
    }

    [Fact]
    public async Task UpdateContributionAsync_WithdrawalUsesBalanceWithoutCurrentContribution()
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
        ContributionMock.Setup(c => c.GetByIdAsync("c1", "g1", "user1"))
            .ReturnsAsync(new GoalContribution
            {
                Id = "c1",
                GoalId = "g1",
                UserId = "user1",
                Amount = 40m
            });
        ContributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 100m },
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 40m }
            ]);

        var contribution = await CreateSut().UpdateContributionAsync("g1", "c1", "user1", -20m, " Planned ");

        Assert.Equal(-20m, contribution.Amount);
        Assert.Equal("Planned", contribution.Reason);
        ContributionMock.Verify(c => c.ReplaceAsync(It.Is<GoalContribution>(updated =>
            updated.Id == "c1" && updated.Amount == -20m && updated.Reason == "Planned")), Times.Once);
        GoalMock.Verify(g => g.UpdateBalanceAsync("g1", "user1", 80m, null), Times.Once);
    }

    [Fact]
    public async Task UpdateContributionAsync_RejectsWithdrawalWithoutReason()
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
        ContributionMock.Setup(c => c.GetByIdAsync("c1", "g1", "user1"))
            .ReturnsAsync(new GoalContribution
            {
                Id = "c1",
                GoalId = "g1",
                UserId = "user1",
                Amount = 40m
            });
        ContributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 100m },
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 40m }
            ]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().UpdateContributionAsync("g1", "c1", "user1", -20m, " "));

        Assert.Equal("Enter a withdrawal reason.", ex.Message);
        ContributionMock.Verify(c => c.ReplaceAsync(It.IsAny<GoalContribution>()), Times.Never);
        GoalMock.Verify(g => g.UpdateBalanceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<decimal>(),
            It.IsAny<GoalStatus?>()), Times.Never);
    }
}
