using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Repositories;

public interface IGoalContributionRepository
{
    Task<List<GoalContribution>> GetByGoalAsync(string goalId, string userId);
    Task<List<GoalContribution>> GetRecentByGoalAsync(string goalId, string userId, DateTime since);
    Task InsertAsync(GoalContribution contribution);
    Task<bool> DeleteAsync(string id, string userId);
}
