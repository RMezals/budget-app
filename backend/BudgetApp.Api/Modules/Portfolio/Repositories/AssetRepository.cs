using BudgetApp.Api.Configuration;
using BudgetApp.Api.Modules.Portfolio.Models;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Portfolio.Repositories;

public class AssetRepository(IMongoDatabase db) : IAssetRepository
{
    private readonly IMongoCollection<Asset> _col =
        db.GetCollection<Asset>(CollectionNames.Assets);

    public async Task<List<Asset>> GetByUserAsync(string userId) =>
        await _col.Find(a => a.UserId == userId).ToListAsync();

    public async Task<Asset?> GetByIdAsync(string id, string userId) =>
        await _col.Find(a => a.Id == id && a.UserId == userId).FirstOrDefaultAsync();

    public async Task InsertAsync(Asset asset) =>
        await _col.InsertOneAsync(asset);

    public async Task<bool> UpdateAsync(string id, string userId, string name, string type, decimal quantity)
    {
        var update = Builders<Asset>.Update
            .Set(a => a.Name, name)
            .Set(a => a.Type, type)
            .Set(a => a.Quantity, quantity);
        var result = await _col.UpdateOneAsync(a => a.Id == id && a.UserId == userId, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> AddPriceAsync(string id, string userId, PriceEntry entry)
    {
        var update = Builders<Asset>.Update.Push(a => a.Price, entry);
        var result = await _col.UpdateOneAsync(a => a.Id == id && a.UserId == userId, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, string userId)
    {
        var result = await _col.DeleteOneAsync(a => a.Id == id && a.UserId == userId);
        return result.DeletedCount > 0;
    }
}
