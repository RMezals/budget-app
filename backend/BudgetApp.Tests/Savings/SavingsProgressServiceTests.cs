using BudgetApp.Api.Modules.Savings.Models;
using Moq;
using Xunit;

namespace BudgetApp.Tests.Savings;

public class SavingsProgressServiceTests : SavingsServiceTestBase
{
    [Fact]
    public async Task GetGoalProgressAsync_ReturnsComputedProgressFromContributions()
    {
        GoalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
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
        ContributionMock.Setup(c => c.GetByGoalAsync("g1", "user1"))
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 200m },
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = -50m }
            ]);

        var result = await CreateProgressSut().GetGoalProgressAsync("g1", "user1");

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
        GoalMock.Setup(g => g.GetByUserAsync("user1"))
            .ReturnsAsync([
                new SavingsGoal { Id = "g1", UserId = "user1", Name = "Trip", TargetAmount = 500m },
                new SavingsGoal { Id = "g2", UserId = "user1", Name = "Car", TargetAmount = 1000m }
            ]);
        ContributionMock.Setup(c => c.GetByGoalsAsync(
                It.Is<List<string>>(ids => ids.SequenceEqual(new[] { "g1", "g2" })),
                "user1"))
            .ReturnsAsync([
                new GoalContribution { GoalId = "g1", UserId = "user1", Amount = 200m },
                new GoalContribution { GoalId = "g2", UserId = "user1", Amount = 300m },
                new GoalContribution { GoalId = "g2", UserId = "user1", Amount = -100m }
            ]);

        var result = await CreateProgressSut().GetGoalProgressListAsync("user1");

        Assert.Equal(2, result.Count);
        Assert.Equal(200m, result.Single(g => g.Id == "g1").CurrentBalance);
        Assert.Equal(200m, result.Single(g => g.Id == "g2").CurrentBalance);
        ContributionMock.Verify(c => c.GetByGoalAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetGoalProgressListAsync_ReturnsEmptyWithoutContributionLookupWhenNoGoals()
    {
        GoalMock.Setup(g => g.GetByUserAsync("user1"))
            .ReturnsAsync([]);

        var result = await CreateProgressSut().GetGoalProgressListAsync("user1");

        Assert.Empty(result);
        ContributionMock.Verify(c => c.GetByGoalsAsync(It.IsAny<List<string>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetGoalProgressAsync_ReturnsNullWhenGoalNotFound()
    {
        GoalMock.Setup(g => g.GetByIdAsync("g1", "user1"))
            .ReturnsAsync((SavingsGoal?)null);

        var result = await CreateProgressSut().GetGoalProgressAsync("g1", "user1");

        Assert.Null(result);
        ContributionMock.Verify(c => c.GetByGoalAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetProjectionAsync_ReturnsReasonWhenContributionRateIsNotPositive()
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
                new GoalContribution
                {
                    GoalId = "g1",
                    UserId = "user1",
                    Amount = 40m,
                    Date = DateTime.UtcNow.AddDays(-60)
                }
            ]);

        var projection = await CreateProgressSut().GetProjectionAsync("g1", "user1");

        Assert.Null(projection.ProjectedCompletion);
        Assert.Equal("Insufficient contribution rate", projection.Reason);
    }
}
