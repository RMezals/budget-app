using BudgetApp.Api.Modules.Portfolio.Models;

namespace BudgetApp.Api.Modules.Portfolio.Repositories;

public interface IAssetRepository
{
    Task<List<Asset>> GetByUserAsync(string userId);
    Task<Asset?> GetByIdAsync(string id, string userId);
    Task InsertAsync(Asset asset);
    Task<bool> UpdateAsync(string id, string userId, string name, string type, decimal quantity);
    Task<bool> AddPriceAsync(string id, string userId, PriceEntry entry);
    Task<bool> DeleteAsync(string id, string userId);
}
