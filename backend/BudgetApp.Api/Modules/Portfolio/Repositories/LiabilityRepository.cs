using BudgetApp.Api.Configuration;
using BudgetApp.Api.Modules.Portfolio.Models;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Portfolio.Repositories;

public class LiabilityRepository(IMongoDatabase db) : ILiabilityRepository
{
    private readonly IMongoCollection<Liability> _col =
        db.GetCollection<Liability>(CollectionNames.Liabilities);

    public async Task<List<Liability>> GetByUserAsync(string userId) =>
        await _col.Find(l => l.UserId == userId).ToListAsync();

    public async Task<Liability?> GetByIdAsync(string id, string userId) =>
        await _col.Find(l => l.Id == id && l.UserId == userId).FirstOrDefaultAsync();

    public async Task InsertAsync(Liability liability) =>
        await _col.InsertOneAsync(liability);

    public async Task<bool> AddAmountAsync(string id, string userId, AmountEntry entry)
    {
        var update = Builders<Liability>.Update.Push(l => l.Amount, entry);
        var result = await _col.UpdateOneAsync(l => l.Id == id && l.UserId == userId, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, string userId)
    {
        var result = await _col.DeleteOneAsync(l => l.Id == id && l.UserId == userId);
        return result.DeletedCount > 0;
    }
}
