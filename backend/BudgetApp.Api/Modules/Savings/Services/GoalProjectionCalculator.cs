using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Services;

public class GoalProjectionCalculator : IGoalProjectionCalculator
{
    public DateTime? CalculateProjectedCompletion(
        decimal targetAmount,
        decimal currentBalance,
        List<GoalContribution> contributions,
        DateTime now)
    {
        if (targetAmount <= 0 || currentBalance >= targetAmount)
        {
            return null;
        }

        var dailyRate = CalculateRecentDailyRate(contributions, now);
        if (dailyRate <= 0)
        {
            return null;
        }

        var days = (int)Math.Ceiling((targetAmount - currentBalance) / dailyRate);
        return now.AddDays(days);
    }

    public ProjectionResult CalculateProjection(
        decimal targetAmount,
        decimal currentBalance,
        List<GoalContribution> contributions,
        DateTime now)
    {
        var projectedCompletion = CalculateProjectedCompletion(targetAmount, currentBalance, contributions, now);
        var reason = projectedCompletion is null
            ? GetProjectionReason(targetAmount, currentBalance, contributions, now)
            : null;

        return new ProjectionResult(projectedCompletion, reason);
    }

    private static string? GetProjectionReason(
        decimal targetAmount,
        decimal currentBalance,
        List<GoalContribution> contributions,
        DateTime now)
    {
        if (targetAmount <= 0)
        {
            return "Target amount must be greater than zero";
        }

        if (currentBalance >= targetAmount)
        {
            return "Goal already completed";
        }

        return CalculateRecentDailyRate(contributions, now) <= 0
            ? "Insufficient contribution rate"
            : null;
    }

    private static decimal CalculateRecentDailyRate(List<GoalContribution> contributions, DateTime now)
    {
        var since = now.AddDays(-30);
        return contributions.Where(c => c.Date >= since).Sum(c => c.Amount) / 30m;
    }
}
