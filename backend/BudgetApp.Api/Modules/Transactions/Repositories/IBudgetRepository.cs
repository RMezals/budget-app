using BudgetApp.Api.Modules.Transactions.Models;

namespace BudgetApp.Api.Modules.Transactions.Repositories;

public interface IBudgetRepository
{
    Task<List<Budget>> GetByMonthAsync(string userId, DateTime monthStart);
    Task UpsertAsync(string userId, string category, DateTime monthStart, decimal limitAmount);
}
