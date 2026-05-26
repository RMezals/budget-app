using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;

namespace BudgetApp.Api.Modules.Savings.Services;

public class SavingsService(ISavingsGoalRepository goalRepo, IGoalContributionRepository contributionRepo) : ISavingsService
{
    public async Task<GoalContribution> AddContributionAsync(string goalId, string userId, decimal amount, DateTime date, string? reason, string? description = null)
    {
        if (amount == 0m)
            throw new InvalidOperationException("Contribution amount must be greater than zero.");

        if (date == default)
            throw new InvalidOperationException("Enter a valid contribution date.");

        var goal = await goalRepo.GetByIdAsync(goalId, userId)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        if (goal.Status == GoalStatus.Paused)
            throw new InvalidOperationException("Resume the goal before adding contributions or withdrawals.");

        if (goal.Status == GoalStatus.Abandoned)
            throw new InvalidOperationException("Abandoned goals cannot accept contributions or withdrawals.");

        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var currentBalance = SavingsBalanceCalculator.CalculateCurrentBalance(contributions);

        if (amount < 0 && Math.Abs(amount) > currentBalance)
            throw new InvalidOperationException("Withdrawal exceeds current saved amount.");

        if (amount < 0 && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Enter a withdrawal reason.");

        var newBalance = currentBalance + amount;
        if (amount > 0 && newBalance > goal.TargetAmount)
        {
            var remainingAmount = Math.Max(goal.TargetAmount - currentBalance, 0);
            throw new InvalidOperationException($"Contribution exceeds the remaining target amount of {remainingAmount}.");
        }

        var canonicalReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var canonicalDescription = string.IsNullOrWhiteSpace(description) ? canonicalReason : description.Trim();
        var contribution = new GoalContribution
        {
            GoalId = goalId,
            UserId = userId,
            Amount = amount,
            Date = date,
            Reason = canonicalReason,
            Description = canonicalDescription,
            BalanceAfter = newBalance
        };

        await contributionRepo.InsertAsync(contribution);

        var newStatus = SavingsGoalStatusResolver.ResolveStatusChange(goal, newBalance);
        await goalRepo.UpdateBalanceAsync(goalId, userId, newBalance, newStatus);

        return contribution;
    }

    public async Task<GoalContribution> UpdateContributionAsync(string goalId, string contributionId, string userId, decimal amount, string? reason)
    {
        if (amount == 0m)
            throw new InvalidOperationException("Contribution amount must be greater than zero.");

        var goal = await goalRepo.GetByIdAsync(goalId, userId)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        if (goal.Status == GoalStatus.Abandoned)
            throw new InvalidOperationException("Abandoned goals cannot be edited.");

        var contribution = await contributionRepo.GetByIdAsync(contributionId, goalId, userId)
            ?? throw new KeyNotFoundException($"Contribution {contributionId} not found.");

        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var currentBalance = SavingsBalanceCalculator.CalculateCurrentBalance(contributions);
        var balanceWithoutContribution = currentBalance - contribution.Amount;

        if (amount < 0 && Math.Abs(amount) > balanceWithoutContribution)
            throw new InvalidOperationException("Withdrawal exceeds current saved amount.");

        if (amount < 0 && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Enter a withdrawal reason.");

        var updatedBalance = balanceWithoutContribution + amount;
        if (amount > 0 && updatedBalance > goal.TargetAmount)
        {
            var remainingAmount = Math.Max(goal.TargetAmount - balanceWithoutContribution, 0);
            throw new InvalidOperationException($"Contribution exceeds the remaining target amount of {remainingAmount}.");
        }

        contribution.Amount = amount;
        contribution.Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        await contributionRepo.ReplaceAsync(contribution);

        var newStatus = SavingsGoalStatusResolver.ResolveStatusChange(goal, updatedBalance);
        await goalRepo.UpdateBalanceAsync(goalId, userId, updatedBalance, newStatus);

        return contribution;
    }

    public async Task AbandonGoalAsync(string goalId, string userId, DateTime date, string? reason, string? description = null)
    {
        var goal = await goalRepo.GetByIdAsync(goalId, userId)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var currentBalance = SavingsBalanceCalculator.CalculateCurrentBalance(contributions);

        if (currentBalance > 0)
        {
            var canonicalReason = string.IsNullOrWhiteSpace(reason) ? "Goal abandoned" : reason;
            var canonicalDescription = description ?? canonicalReason;
            var contribution = new GoalContribution
            {
                GoalId = goalId,
                UserId = userId,
                Amount = -currentBalance,
                Date = date,
                Reason = canonicalReason,
                Description = canonicalDescription,
                BalanceAfter = 0m
            };

            await contributionRepo.InsertAsync(contribution);
        }

        await goalRepo.UpdateBalanceAsync(goal.Id, userId, 0m, GoalStatus.Abandoned);
    }

    public async Task RecalculateBalanceAsync(string goalId, string userId)
    {
        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var newBalance = SavingsBalanceCalculator.CalculateCurrentBalance(contributions);
        var goal = await goalRepo.GetByIdAsync(goalId, userId);
        var newStatus = goal is null ? null : SavingsGoalStatusResolver.ResolveStatusChange(goal, newBalance);
        await goalRepo.UpdateBalanceAsync(goalId, userId, newBalance, newStatus);
    }
}
