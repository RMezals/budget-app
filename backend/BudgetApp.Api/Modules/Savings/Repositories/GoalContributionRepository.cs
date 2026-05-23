using BudgetApp.Api.Configuration;
using BudgetApp.Api.Modules.Savings.Models;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Savings.Repositories;

public class GoalContributionRepository(IMongoDatabase db) : IGoalContributionRepository
{
    private readonly IMongoCollection<GoalContribution> _col =
        db.GetCollection<GoalContribution>(CollectionNames.GoalContributions);

    public async Task<List<GoalContribution>> GetByGoalAsync(string goalId, string userId) =>
        await _col.Find(c => c.GoalId == goalId && c.UserId == userId)
            .SortByDescending(c => c.Date)
            .ToListAsync();

    public async Task<List<GoalContribution>> GetByGoalsAsync(List<string> goalIds, string userId) =>
        await _col.Find(c => goalIds.Contains(c.GoalId) && c.UserId == userId)
            .SortByDescending(c => c.Date)
            .ToListAsync();

    public async Task<List<GoalContribution>> GetRecentByGoalAsync(string goalId, string userId, DateTime since) =>
        await _col.Find(c => c.GoalId == goalId && c.UserId == userId && c.Date >= since).ToListAsync();

    public async Task InsertAsync(GoalContribution contribution) =>
        await _col.InsertOneAsync(contribution);

    public async Task<bool> DeleteAsync(string id, string goalId, string userId)
    {
        var result = await _col.DeleteOneAsync(c => c.Id == id && c.GoalId == goalId && c.UserId == userId);
        return result.DeletedCount > 0;
    }
}
