using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;

namespace BudgetApp.Api.Modules.Savings.Services;

public class SavingsService(ISavingsGoalRepository goalRepo, IGoalContributionRepository contributionRepo) : ISavingsService
{
    public async Task<GoalContribution> AddContributionAsync(string goalId, string userId, decimal amount, DateTime date, string? note, string? reason, string? description = null)
    {
        var goal = await goalRepo.GetByIdAsync(goalId, userId)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        if (amount < 0 && Math.Abs(amount) > goal.CurrentAmount)
            throw new InvalidOperationException("Withdrawal exceeds current saved amount.");

        var newBalance = goal.CurrentAmount + amount;
        note ??= description;
        var contribution = new GoalContribution
        {
            GoalId = goalId,
            UserId = userId,
            Amount = amount,
            Date = date,
            Note = note,
            Reason = reason,
            Description = description ?? note,
            BalanceAfter = newBalance
        };

        await contributionRepo.InsertAsync(contribution);

        var newStatus = newBalance >= goal.TargetAmount ? GoalStatus.Completed : (GoalStatus?)null;
        await goalRepo.UpdateBalanceAsync(goalId, userId, newBalance, newStatus);

        return contribution;
    }

    public async Task RecalculateBalanceAsync(string goalId, string userId)
    {
        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var newBalance = contributions.Sum(c => c.Amount);
        await goalRepo.UpdateBalanceAsync(goalId, userId, newBalance);
    }

    public async Task<ProjectionResult> GetProjectionAsync(string goalId, string userId)
    {
        var goal = await goalRepo.GetByIdAsync(goalId, userId)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        var since = DateTime.UtcNow.AddDays(-30);
        var recent = await contributionRepo.GetRecentByGoalAsync(goalId, userId, since);

        var dailyRate = recent.Sum(c => c.Amount) / 30m;
        if (dailyRate <= 0)
            return new ProjectionResult(null, "Insufficient contribution rate");

        var days = (int)Math.Ceiling((goal.TargetAmount - goal.CurrentAmount) / dailyRate);
        return new ProjectionResult(DateTime.UtcNow.AddDays(days), null);
    }
}
