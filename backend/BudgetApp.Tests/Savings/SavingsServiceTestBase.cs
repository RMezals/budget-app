using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Savings.Services;
using Moq;

namespace BudgetApp.Tests.Savings;

public abstract class SavingsServiceTestBase
{
    protected readonly Mock<ISavingsGoalRepository> GoalMock = new();
    protected readonly Mock<IGoalContributionRepository> ContributionMock = new();
    private readonly IGoalProjectionCalculator _projectionCalculator = new GoalProjectionCalculator();

    protected SavingsService CreateSut() => new(GoalMock.Object, ContributionMock.Object);

    protected SavingsProgressService CreateProgressSut() => new(
        GoalMock.Object,
        ContributionMock.Object,
        _projectionCalculator);
}
