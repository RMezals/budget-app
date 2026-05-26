using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;

namespace BudgetApp.Api.Modules.Savings.Services;

public class SavingsProgressService(
    ISavingsGoalRepository goalRepo,
    IGoalContributionRepository contributionRepo,
    IGoalProjectionCalculator projectionCalculator) : ISavingsProgressService
{
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
        var currentBalance = SavingsBalanceCalculator.CalculateCurrentBalance(contributions);

        return projectionCalculator.CalculateProjection(goal.TargetAmount, currentBalance, contributions, DateTime.UtcNow);
    }

    private GoalProgressDto CreateProgressDto(
        SavingsGoal goal,
        List<GoalContribution> contributions,
        DateTime now)
    {
        var currentBalance = SavingsBalanceCalculator.CalculateCurrentBalance(contributions);

        return new GoalProgressDto
        {
            Id = goal.Id,
            UserId = goal.UserId,
            Name = goal.Name,
            TargetAmount = goal.TargetAmount,
            CurrentBalance = currentBalance,
            ProjectedCompletion = projectionCalculator.CalculateProjectedCompletion(
                goal.TargetAmount,
                currentBalance,
                contributions,
                now),
            Status = SavingsGoalStatusResolver.ResolveProgressStatus(goal, currentBalance),
            Deadline = goal.Deadline,
            Description = goal.Description
        };
    }
}
