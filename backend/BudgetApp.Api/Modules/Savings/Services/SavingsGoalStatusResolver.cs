using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Services;

public static class SavingsGoalStatusResolver
{
    public static GoalStatus ResolveProgressStatus(SavingsGoal goal, decimal currentBalance)
    {
        if (currentBalance >= goal.TargetAmount)
        {
            return GoalStatus.Completed;
        }

        return goal.Status == GoalStatus.Completed ? GoalStatus.Active : goal.Status;
    }

    public static GoalStatus? ResolveStatusChange(SavingsGoal goal, decimal currentBalance)
    {
        var status = ResolveProgressStatus(goal, currentBalance);
        return status == goal.Status ? null : status;
    }
}
