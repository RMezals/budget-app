using BudgetApp.Api.Modules.Portfolio.Models;

namespace BudgetApp.Api.Modules.Portfolio.Repositories;

public interface ILiabilityRepository
{
    Task<List<Liability>> GetByUserAsync(string userId);
    Task<Liability?> GetByIdAsync(string id, string userId);
    Task InsertAsync(Liability liability);
    Task<bool> AddAmountAsync(string id, string userId, AmountEntry entry);
    Task<bool> DeleteAsync(string id, string userId);
}
