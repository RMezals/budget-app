using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Services;

public interface ISavingsService
{
    Task<GoalContribution> AddContributionAsync(string goalId, string userId, decimal amount, DateTime date, string? reason, string? description = null);
    Task AbandonGoalAsync(string goalId, string userId, DateTime date, string? reason, string? description = null);
    Task RecalculateBalanceAsync(string goalId, string userId);
}
