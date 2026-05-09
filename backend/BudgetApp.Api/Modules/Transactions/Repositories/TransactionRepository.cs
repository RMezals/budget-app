using BudgetApp.Api.Configuration;
using BudgetApp.Api.Modules.Transactions.Models;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Transactions.Repositories;

public class TransactionRepository(IMongoDatabase db) : ITransactionRepository
{
    private readonly IMongoCollection<Transaction> _col =
        db.GetCollection<Transaction>(CollectionNames.Transactions);

    public async Task<List<Transaction>> GetAllAsync(string userId, TransactionFilter filter)
    {
        var f = Builders<Transaction>.Filter.Eq(t => t.UserId, userId);
        if (filter.From.HasValue) f &= Builders<Transaction>.Filter.Gte(t => t.Date, filter.From.Value);
        if (filter.To.HasValue) f &= Builders<Transaction>.Filter.Lte(t => t.Date, filter.To.Value);
        if (filter.Category is not null) f &= Builders<Transaction>.Filter.Eq(t => t.Category, filter.Category);
        if (filter.MinAmount.HasValue) f &= Builders<Transaction>.Filter.Gte(t => t.Amount, filter.MinAmount.Value);
        if (filter.MaxAmount.HasValue) f &= Builders<Transaction>.Filter.Lte(t => t.Amount, filter.MaxAmount.Value);
        if (filter.Keyword is not null) f &= Builders<Transaction>.Filter.Regex(t => t.Description, filter.Keyword);
        return await _col.Find(f).SortByDescending(t => t.Date).ToListAsync();
    }

    public async Task<List<Transaction>> GetByMonthAsync(string userId, DateTime monthStart, DateTime monthEnd) =>
        await _col.Find(t => t.UserId == userId && t.Date >= monthStart && t.Date < monthEnd).ToListAsync();

    public async Task InsertAsync(Transaction transaction) =>
        await _col.InsertOneAsync(transaction);

    public async Task<bool> UpdateAsync(string id, string userId, decimal amount, DateTime date, string category, string? description)
    {
        var update = Builders<Transaction>.Update
            .Set(t => t.Amount, amount)
            .Set(t => t.Date, date)
            .Set(t => t.Category, category)
            .Set(t => t.Description, description);
        var result = await _col.UpdateOneAsync(t => t.Id == id && t.UserId == userId, update);
        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, string userId)
    {
        var result = await _col.DeleteOneAsync(t => t.Id == id && t.UserId == userId);
        return result.DeletedCount > 0;
    }
}
