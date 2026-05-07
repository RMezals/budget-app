using BudgetApp.Api.Configuration;
using BudgetApp.Api.Modules.Transactions.Models;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Transactions.Repositories;

public class BudgetRepository(IMongoDatabase db) : IBudgetRepository
{
    private readonly IMongoCollection<Budget> _col =
        db.GetCollection<Budget>(CollectionNames.Budgets);

    public async Task<List<Budget>> GetByMonthAsync(string userId, DateTime monthStart) =>
        await _col.Find(b => b.UserId == userId && b.Date == monthStart).ToListAsync();

    public async Task UpsertAsync(string userId, string category, DateTime monthStart, decimal limitAmount)
    {
        var filter = Builders<Budget>.Filter.And(
            Builders<Budget>.Filter.Eq(b => b.UserId,   userId),
            Builders<Budget>.Filter.Eq(b => b.Category, category),
            Builders<Budget>.Filter.Eq(b => b.Date,     monthStart));

        var update = Builders<Budget>.Update
            .SetOnInsert(b => b.UserId,   userId)
            .SetOnInsert(b => b.Category, category)
            .SetOnInsert(b => b.Date,     monthStart)
            .Set(b => b.LimitAmount,      limitAmount);

        await _col.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }
}
