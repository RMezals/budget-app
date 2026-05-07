using BudgetApp.Api.Configuration;
using BudgetApp.Api.Modules.Savings.Models;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Savings.Repositories;

public class SavingsGoalRepository(IMongoDatabase db) : ISavingsGoalRepository
{
    private readonly IMongoCollection<SavingsGoal> _col =
        db.GetCollection<SavingsGoal>(CollectionNames.SavingsGoals);

    public async Task<List<SavingsGoal>> GetByUserAsync(string userId) =>
        await _col.Find(g => g.UserId == userId).ToListAsync();

    public async Task<List<SavingsGoal>> GetActiveByUserAsync(string userId) =>
        await _col.Find(g => g.UserId == userId && g.Status == GoalStatus.Active).ToListAsync();

    public async Task<SavingsGoal?> GetByIdAsync(string id, string userId) =>
        await _col.Find(g => g.Id == id && g.UserId == userId).FirstOrDefaultAsync();

    public async Task InsertAsync(SavingsGoal goal) =>
        await _col.InsertOneAsync(goal);

    public async Task<bool> UpdateAsync(string id, string userId, string name, decimal targetAmount, DateTime deadline, string? description)
    {
        var update = Builders<SavingsGoal>.Update
            .Set(g => g.Name,         name)
            .Set(g => g.TargetAmount, targetAmount)
            .Set(g => g.Deadline,     deadline)
            .Set(g => g.Description,  description);
        var result = await _col.UpdateOneAsync(g => g.Id == id && g.UserId == userId, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> UpdateStatusAsync(string id, string userId, GoalStatus status)
    {
        var update = Builders<SavingsGoal>.Update.Set(g => g.Status, status);
        var result = await _col.UpdateOneAsync(g => g.Id == id && g.UserId == userId, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> UpdateBalanceAsync(string id, string userId, decimal newBalance, GoalStatus? newStatus = null)
    {
        var update = Builders<SavingsGoal>.Update.Set(g => g.CurrentAmount, newBalance);
        if (newStatus.HasValue)
            update = update.Set(g => g.Status, newStatus.Value);
        var result = await _col.UpdateOneAsync(g => g.Id == id && g.UserId == userId, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, string userId)
    {
        var result = await _col.DeleteOneAsync(g => g.Id == id && g.UserId == userId);
        return result.DeletedCount > 0;
    }
}
