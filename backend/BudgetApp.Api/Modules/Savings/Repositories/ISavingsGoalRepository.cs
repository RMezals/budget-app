using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Repositories;

public interface ISavingsGoalRepository
{
    Task<List<SavingsGoal>> GetByUserAsync(string userId);
    Task<List<SavingsGoal>> GetActiveByUserAsync(string userId);
    Task<SavingsGoal?> GetByIdAsync(string id, string userId);
    Task InsertAsync(SavingsGoal goal);
    Task<bool> UpdateAsync(string id, string userId, string name, decimal targetAmount, DateTime deadline, string? description);
    Task<bool> UpdateStatusAsync(string id, string userId, GoalStatus status);
    Task<bool> UpdateBalanceAsync(string id, string userId, decimal newBalance, GoalStatus? newStatus = null);
    Task<bool> DeleteAsync(string id, string userId);
}
