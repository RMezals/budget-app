using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;

namespace BudgetApp.Api.Modules.Savings.Services;

// Handles the business rules around adding/editing contributions and abandoning goals
public class SavingsService(ISavingsGoalRepository goalRepo, IGoalContributionRepository contributionRepo) : ISavingsService
{
    // Validates and records a new deposit or withdrawal for a savings goal, then updates the goal balance and status
    public async Task<GoalContribution> AddContributionAsync(string goalId, string userId, decimal amount, DateTime date, string? reason, string? description = null)
    {
        // Zero is rejected; negative values are valid withdrawals and are handled below
        if (amount == 0m)
            throw new InvalidOperationException("Contribution amount cannot be zero.");

        if (date == default)
            throw new InvalidOperationException("Enter a valid contribution date.");

        var goal = await goalRepo.GetByIdAsync(goalId, userId)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        // Paused goals must be resumed before any money movement is allowed
        if (goal.Status == GoalStatus.Paused)
            throw new InvalidOperationException("Resume the goal before adding contributions or withdrawals.");

        if (goal.Status == GoalStatus.Abandoned)
            throw new InvalidOperationException("Abandoned goals cannot accept contributions or withdrawals.");

        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var currentBalance = SavingsBalanceCalculator.CalculateCurrentBalance(contributions);

        // A withdrawal cannot exceed what has already been saved
        if (amount < 0 && Math.Abs(amount) > currentBalance)
            throw new InvalidOperationException("Withdrawal exceeds current saved amount.");

        // Require a reason so users have a record of why money was taken out
        if (amount < 0 && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Enter a withdrawal reason.");

        var newBalance = currentBalance + amount;
        // Prevent over-contributing past the target — the user should complete the goal exactly
        if (amount > 0 && newBalance > goal.TargetAmount)
        {
            // Tell the user how much space is left so they can adjust the amount
            var remainingAmount = Math.Max(goal.TargetAmount - currentBalance, 0);
            throw new InvalidOperationException($"Contribution exceeds the remaining target amount of {remainingAmount}.");
        }

        // Normalise whitespace; for deposits the description falls back to the reason if not provided
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

        // Check whether the new balance completes the goal and update the goal status accordingly
        var newStatus = SavingsGoalStatusResolver.ResolveStatusChange(goal, newBalance);
        await goalRepo.UpdateBalanceAsync(goalId, userId, newBalance, newStatus);

        return contribution;
    }

    // Updates the amount and optional reason of an existing contribution and recalculates the goal balance
    public async Task<GoalContribution> UpdateContributionAsync(string goalId, string contributionId, string userId, decimal amount, string? reason)
    {
        // Zero is rejected; negative values are valid withdrawals and are handled below
        if (amount == 0m)
            throw new InvalidOperationException("Contribution amount cannot be zero.");

        var goal = await goalRepo.GetByIdAsync(goalId, userId)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        if (goal.Status == GoalStatus.Abandoned)
            throw new InvalidOperationException("Abandoned goals cannot be edited.");

        var contribution = await contributionRepo.GetByIdAsync(contributionId, goalId, userId)
            ?? throw new KeyNotFoundException($"Contribution {contributionId} not found.");

        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var currentBalance = SavingsBalanceCalculator.CalculateCurrentBalance(contributions);
        // Remove the old contribution amount first so we validate against the balance without it
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
        // Keep BalanceAfter consistent with the recalculated balance so the stored record is not stale
        contribution.BalanceAfter = updatedBalance;

        await contributionRepo.ReplaceAsync(contribution);

        var newStatus = SavingsGoalStatusResolver.ResolveStatusChange(goal, updatedBalance);
        await goalRepo.UpdateBalanceAsync(goalId, userId, updatedBalance, newStatus);

        return contribution;
    }

    // Withdraws the full balance of a goal and marks it as Abandoned; a withdrawal contribution is recorded for audit purposes
    public async Task AbandonGoalAsync(string goalId, string userId, DateTime date, string? reason, string? description = null)
    {
        var goal = await goalRepo.GetByIdAsync(goalId, userId)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var currentBalance = SavingsBalanceCalculator.CalculateCurrentBalance(contributions);

        // Only insert a withdrawal record if there is actually money to withdraw
        if (currentBalance > 0)
        {
            var canonicalReason = string.IsNullOrWhiteSpace(reason) ? "Goal abandoned" : reason;
            var canonicalDescription = description ?? canonicalReason;
            // Negative amount represents a full withdrawal back to zero
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

        // Set balance to zero and mark as Abandoned regardless of previous balance
        await goalRepo.UpdateBalanceAsync(goal.Id, userId, 0m, GoalStatus.Abandoned);
    }

    // Recomputes the goal balance from scratch by summing all stored contributions (useful after a deletion)
    public async Task RecalculateBalanceAsync(string goalId, string userId)
    {
        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var newBalance = SavingsBalanceCalculator.CalculateCurrentBalance(contributions);
        var goal = await goalRepo.GetByIdAsync(goalId, userId);
        // Only resolve a new status if the goal still exists
        var newStatus = goal is null ? null : SavingsGoalStatusResolver.ResolveStatusChange(goal, newBalance);
        await goalRepo.UpdateBalanceAsync(goalId, userId, newBalance, newStatus);
    }
}
