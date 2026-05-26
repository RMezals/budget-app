using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Repositories;

public interface IGoalContributionRepository
{
    Task<List<GoalContribution>> GetByGoalAsync(string goalId, string userId);
    Task<List<GoalContribution>> GetByGoalsAsync(List<string> goalIds, string userId);
    Task<List<GoalContribution>> GetRecentByGoalAsync(string goalId, string userId, DateTime since);
    Task<List<GoalContribution>> GetByUserAndMonthAsync(string userId, DateTime from, DateTime to);
    Task<GoalContribution?> GetByIdAsync(string id, string goalId, string userId);
    Task InsertAsync(GoalContribution contribution);
    Task<bool> ReplaceAsync(GoalContribution contribution);
    Task<bool> DeleteAsync(string id, string goalId, string userId);
}
