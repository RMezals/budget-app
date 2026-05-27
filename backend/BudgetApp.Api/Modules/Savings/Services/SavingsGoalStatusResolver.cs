using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Services;

// Determines the correct GoalStatus based on the current balance relative to the target
public static class SavingsGoalStatusResolver
{
    // Returns the status the goal should be in given the current balance (does not write to the database)
    public static GoalStatus ResolveProgressStatus(SavingsGoal goal, decimal currentBalance)
    {
        if (currentBalance >= goal.TargetAmount)
        {
            return GoalStatus.Completed;
        }

        // If the balance dropped below the target (e.g. after a withdrawal), revert Completed → Active
        return goal.Status == GoalStatus.Completed ? GoalStatus.Active : goal.Status;
    }

    // Returns the new status only when it differs from the current one; returns null if no change is needed
    public static GoalStatus? ResolveStatusChange(SavingsGoal goal, decimal currentBalance)
    {
        var status = ResolveProgressStatus(goal, currentBalance);
        // Returning null signals callers that no database update for the status field is required
        return status == goal.Status ? null : status;
    }
}
