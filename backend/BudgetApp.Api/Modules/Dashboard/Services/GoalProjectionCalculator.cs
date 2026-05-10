using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Dashboard.Services;

/// <summary>
/// Calculates projected completion dates for savings goals based on historical contributions
/// </summary>
public static class GoalProjectionCalculator
{
    private const int LookbackMonths = 3;
    private const int MinimumContributionsRequired = 2;

    /// <summary>
    /// Calculates when a goal is projected to be completed based on recent contribution history
    /// </summary>
    /// <param name="goal">The savings goal</param>
    /// <param name="contributions">Recent contributions to the goal</param>
    /// <param name="now">Current date/time</param>
    /// <returns>Projected completion date, or null if insufficient data or already completed</returns>
    public static DateTime? CalculateProjectedCompletion(
        SavingsGoal goal,
        List<GoalContribution> contributions,
        DateTime now)
    {
        if (IsGoalAlreadyComplete(goal))
        {
            return null;
        }

        var recentContributions = GetRecentContributions(contributions, now);

        if (!HasSufficientData(recentContributions))
        {
            return null;
        }

        var averageMonthlyContribution = CalculateAverageMonthlyContribution(recentContributions, now);

        if (IsContributionRateTooLow(averageMonthlyContribution))
        {
            return null;
        }

        return CalculateCompletionDate(goal, averageMonthlyContribution, now);
    }

    private static bool IsGoalAlreadyComplete(SavingsGoal goal)
    {
        return goal.CurrentAmount >= goal.TargetAmount;
    }

    private static List<GoalContribution> GetRecentContributions(List<GoalContribution> contributions, DateTime now)
    {
        var cutoffDate = now.AddMonths(-LookbackMonths);
        return contributions
            .Where(c => c.Date >= cutoffDate && c.Amount > 0)
            .OrderBy(c => c.Date)
            .ToList();
    }

    private static bool HasSufficientData(List<GoalContribution> contributions)
    {
        return contributions.Count >= MinimumContributionsRequired;
    }

    private static decimal CalculateAverageMonthlyContribution(List<GoalContribution> contributions, DateTime now)
    {
        if (contributions.Count == 0)
        {
            return 0;
        }

        var totalContributed = contributions.Sum(c => c.Amount);
        var monthsSpan = CalculateMonthsSpan(contributions, now);

        return totalContributed / monthsSpan;
    }

    private const double AverageDaysPerMonth = 30.0;

    private static decimal CalculateMonthsSpan(List<GoalContribution> contributions, DateTime now)
    {
        var firstContributionDate = contributions.First().Date;
        var daysDifference = (now - firstContributionDate).TotalDays;
        var monthsSpan = (decimal)(daysDifference / AverageDaysPerMonth);

        return Math.Max(monthsSpan, 1);
    }

    private static bool IsContributionRateTooLow(decimal averageMonthlyContribution)
    {
        return averageMonthlyContribution <= 0;
    }

    private static DateTime? CalculateCompletionDate(SavingsGoal goal, decimal averageMonthlyContribution, DateTime now)
    {
        var remaining = goal.TargetAmount - goal.CurrentAmount;
        var monthsToCompletion = (double)(remaining / averageMonthlyContribution);

        return now.AddMonths((int)Math.Ceiling(monthsToCompletion));
    }
}
