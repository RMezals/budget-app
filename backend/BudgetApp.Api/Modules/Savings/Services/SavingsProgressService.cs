using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;

namespace BudgetApp.Api.Modules.Savings.Services;

// Assembles progress DTOs that combine goal metadata with live balance and projected completion date
public class SavingsProgressService(
    ISavingsGoalRepository goalRepo,
    IGoalContributionRepository contributionRepo,
    IGoalProjectionCalculator projectionCalculator) : ISavingsProgressService
{
    // Returns a progress summary for every goal belonging to the user
    public async Task<List<GoalProgressDto>> GetGoalProgressListAsync(string userId)
    {
        var goals = await goalRepo.GetByUserAsync(userId);
        if (goals.Count == 0)
        {
            return [];
        }

        var goalIds = goals.Select(g => g.Id).ToList();
        // Fetch all contributions in a single query and then split them by goal in memory
        var contributions = await contributionRepo.GetByGoalsAsync(goalIds, userId);
        var contributionsByGoal = contributions
            .GroupBy(c => c.GoalId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var now = DateTime.UtcNow;
        return goals.Select(goal =>
        {
            // Goals with no contributions yet will get an empty list rather than null
            contributionsByGoal.TryGetValue(goal.Id, out var goalContributions);
            return CreateProgressDto(goal, goalContributions ?? [], now);
        }).ToList();
    }

    // Returns the progress summary for a single goal, or null if the goal is not found
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

    // Returns the projected completion date and an explanation if no projection can be made
    public async Task<ProjectionResult> GetProjectionAsync(string goalId, string userId)
    {
        var goal = await goalRepo.GetByIdAsync(goalId, userId)
            ?? throw new KeyNotFoundException($"Goal {goalId} not found.");

        var contributions = await contributionRepo.GetByGoalAsync(goalId, userId);
        var currentBalance = SavingsBalanceCalculator.CalculateCurrentBalance(contributions);

        return projectionCalculator.CalculateProjection(goal.TargetAmount, currentBalance, contributions, DateTime.UtcNow);
    }

    // Builds a GoalProgressDto by combining goal data with the calculated balance and projection
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
            // ProjectedCompletion is null if the goal is already met or no positive contribution trend exists
            ProjectedCompletion = projectionCalculator.CalculateProjectedCompletion(
                goal.TargetAmount,
                currentBalance,
                contributions,
                now),
            // ResolveProgressStatus may differ from goal.Status if a recent withdrawal un-completed the goal
            Status = SavingsGoalStatusResolver.ResolveProgressStatus(goal, currentBalance),
            Deadline = goal.Deadline,
            Description = goal.Description
        };
    }
}
