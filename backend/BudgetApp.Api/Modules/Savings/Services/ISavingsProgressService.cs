using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Services;

public record ProjectionResult(DateTime? ProjectedCompletion, string? Reason);

public interface ISavingsProgressService
{
    Task<List<GoalProgressDto>> GetGoalProgressListAsync(string userId);
    Task<GoalProgressDto?> GetGoalProgressAsync(string goalId, string userId);
    Task<ProjectionResult> GetProjectionAsync(string goalId, string userId);
}
