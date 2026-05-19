using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;

namespace BudgetApp.Api.Modules.Savings.Services;

public class SavingsService(ISavingsGoalRepository goalRepo, IGoalContributionRepository contributionRepo) : ISavingsService
{
    public async Task<GoalContribution> AddContributionAsync(string goalId, string userId, decimal amount, DateTime date, string? reason, string? description = null)
    {
        var goal = await goalRepo.GetByIdAsync(goalId, userId)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        if (goal.Status == GoalStatus.Paused)
            throw new InvalidOperationException("Resume the goal before adding contributions or withdrawals.");

        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var currentBalance = CalculateCurrentBalance(contributions);

        if (amount < 0 && Math.Abs(amount) > currentBalance)
            throw new InvalidOperationException("Withdrawal exceeds current saved amount.");

        var newBalance = currentBalance + amount;
        if (amount > 0 && newBalance > goal.TargetAmount)
        {
            var remainingAmount = Math.Max(goal.TargetAmount - currentBalance, 0);
            throw new InvalidOperationException($"Contribution exceeds the remaining target amount of {remainingAmount}.");
        }

        var canonicalDescription = description ?? reason;
        var contribution = new GoalContribution
        {
            GoalId = goalId,
            UserId = userId,
            Amount = amount,
            Date = date,
            Reason = reason,
            Description = canonicalDescription,
            BalanceAfter = newBalance
        };

        await contributionRepo.InsertAsync(contribution);

        var newStatus = ResolveStatusChange(goal, newBalance);
        await goalRepo.UpdateBalanceAsync(goalId, userId, newBalance, newStatus);

        return contribution;
    }

    public async Task RecalculateBalanceAsync(string goalId, string userId)
    {
        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var newBalance = CalculateCurrentBalance(contributions);
        var goal = await goalRepo.GetByIdAsync(goalId, userId);
        var newStatus = goal is null ? null : ResolveStatusChange(goal, newBalance);
        await goalRepo.UpdateBalanceAsync(goalId, userId, newBalance, newStatus);
    }

    public async Task<List<GoalProgressDto>> GetGoalProgressListAsync(string userId)
    {
        var goals = await goalRepo.GetByUserAsync(userId);
        if (goals.Count == 0)
        {
            return [];
        }

        var goalIds = goals.Select(g => g.Id).ToList();
        var contributions = await contributionRepo.GetByGoalsAsync(goalIds, userId);
        var contributionsByGoal = contributions
            .GroupBy(c => c.GoalId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var now = DateTime.UtcNow;
        return goals.Select(goal =>
        {
            contributionsByGoal.TryGetValue(goal.Id, out var goalContributions);
            return CreateProgressDto(goal, goalContributions ?? [], now);
        }).ToList();
    }

    public async Task<GoalProgressDto?> GetGoalProgressAsync(string goalId, string userId)
    {
        var goal = await goalRepo.GetByIdAsync(goalId, userId);
        if (goal is null)
        {
            return null;
        }

        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        return CreateProgressDto(goal, contributions, DateTime.UtcNow);
    }

    public async Task<ProjectionResult> GetProjectionAsync(string goalId, string userId)
    {
        var goal = await goalRepo.GetByIdAsync(goalId, userId)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var currentBalance = CalculateCurrentBalance(contributions);
        var projectedCompletion = CalculateProjectedCompletion(goal.TargetAmount, currentBalance, contributions, DateTime.UtcNow);
        var reason = projectedCompletion is null ? GetProjectionReason(goal.TargetAmount, currentBalance, contributions, DateTime.UtcNow) : null;

        return new ProjectionResult(projectedCompletion, reason);
    }

    private static GoalProgressDto CreateProgressDto(
        SavingsGoal goal,
        List<GoalContribution> contributions,
        DateTime now)
    {
        var currentBalance = CalculateCurrentBalance(contributions);

        return new GoalProgressDto
        {
            Id = goal.Id,
            UserId = goal.UserId,
            Name = goal.Name,
            TargetAmount = goal.TargetAmount,
            CurrentBalance = currentBalance,
            ProjectedCompletion = CalculateProjectedCompletion(goal.TargetAmount, currentBalance, contributions, now),
            Status = ResolveProgressStatus(goal, currentBalance),
            Deadline = goal.Deadline,
            Description = goal.Description
        };
    }

    private static decimal CalculateCurrentBalance(List<GoalContribution> contributions) =>
        contributions.Sum(c => c.Amount);

    private static GoalStatus ResolveProgressStatus(SavingsGoal goal, decimal currentBalance)
    {
        if (currentBalance >= goal.TargetAmount)
        {
            return GoalStatus.Completed;
        }

        return goal.Status == GoalStatus.Completed ? GoalStatus.Active : goal.Status;
    }

    private static GoalStatus? ResolveStatusChange(SavingsGoal goal, decimal currentBalance)
    {
        var status = ResolveProgressStatus(goal, currentBalance);
        return status == goal.Status ? null : status;
    }

    private static DateTime? CalculateProjectedCompletion(
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
