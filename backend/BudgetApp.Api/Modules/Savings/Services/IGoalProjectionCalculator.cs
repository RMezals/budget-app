using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Services;

public interface IGoalProjectionCalculator
{
    DateTime? CalculateProjectedCompletion(
        decimal targetAmount,
        decimal currentBalance,
        List<GoalContribution> contributions,
        DateTime now);

    ProjectionResult CalculateProjection(
        decimal targetAmount,
        decimal currentBalance,
        List<GoalContribution> contributions,
        DateTime now);
}
