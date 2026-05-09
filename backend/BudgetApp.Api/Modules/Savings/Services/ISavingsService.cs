using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Services;

public record ProjectionResult(DateTime? ProjectedCompletion, string? Reason);

public interface ISavingsService
{
    Task<GoalContribution> AddContributionAsync(string goalId, string userId, decimal amount, DateTime date, string? description);
    Task RecalculateBalanceAsync(string goalId, string userId);
    Task<ProjectionResult> GetProjectionAsync(string goalId, string userId);
}
