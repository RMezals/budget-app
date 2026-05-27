using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Services;

// Estimates when a savings goal will be reached based on the user's recent contribution behaviour
public class GoalProjectionCalculator : IGoalProjectionCalculator
{
    // Returns the estimated completion date, or null if the goal is already met or there is no positive contribution trend
    public DateTime? CalculateProjectedCompletion(
        decimal targetAmount,
        decimal currentBalance,
        List<GoalContribution> contributions,
        DateTime now)
    {
        // Cannot project for invalid targets or already-completed goals
        if (targetAmount <= 0 || currentBalance >= targetAmount)
        {
            return null;
        }

        var dailyRate = CalculateRecentDailyRate(contributions, now);
        // If no money has been deposited recently, projection is impossible
        if (dailyRate <= 0)
        {
            return null;
        }

        // Divide the remaining amount by the average daily deposit rate to get the number of days needed
        var days = (int)Math.Ceiling((targetAmount - currentBalance) / dailyRate);
        return now.AddDays(days);
    }

    // Returns a ProjectionResult that includes the projected date plus a human-readable reason when projection is unavailable
    public ProjectionResult CalculateProjection(
        decimal targetAmount,
        decimal currentBalance,
        List<GoalContribution> contributions,
        DateTime now)
    {
        var projectedCompletion = CalculateProjectedCompletion(targetAmount, currentBalance, contributions, now);
        // Only populate the reason when there is no projected date, so the UI can display a helpful message
        var reason = projectedCompletion is null
            ? GetProjectionReason(targetAmount, currentBalance, contributions, now)
            : null;

        return new ProjectionResult(projectedCompletion, reason);
    }

    // Explains why a projected completion date could not be calculated
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

        // If we get here the only remaining cause is an insufficient recent deposit rate
        return CalculateRecentDailyRate(contributions, now) <= 0
            ? "Insufficient contribution rate"
            : null;
    }

    // Calculates the average daily deposit amount over the past 30 days
    private static decimal CalculateRecentDailyRate(List<GoalContribution> contributions, DateTime now)
    {
        var since = now.AddDays(-30);
        // Only count deposits (positive amounts) — including withdrawals would reduce the rate and
        // produce a misleadingly pessimistic or null projected completion date
        return contributions.Where(c => c.Date >= since && c.Amount > 0).Sum(c => c.Amount) / 30m;
    }
}
