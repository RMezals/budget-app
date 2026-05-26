using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public interface IGoalProjectionCalculator
{
    DateTime? CalculateProjectedCompletion(SavingsGoal goal, List<GoalContribution> contributions, DateTime now);
}
