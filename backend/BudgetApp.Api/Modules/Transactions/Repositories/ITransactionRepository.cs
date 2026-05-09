using BudgetApp.Api.Modules.Transactions.Models;

namespace BudgetApp.Api.Modules.Transactions.Repositories;

public record TransactionFilter(
    DateTime? From,
    DateTime? To,
    string? Category,
    decimal? MinAmount,
    decimal? MaxAmount,
    string? Keyword);

public interface ITransactionRepository
{
    Task<List<Transaction>> GetAllAsync(string userId, TransactionFilter filter);
    Task<List<Transaction>> GetByMonthAsync(string userId, DateTime monthStart, DateTime monthEnd);
    Task InsertAsync(Transaction transaction);
    Task<bool> UpdateAsync(string id, string userId, decimal amount, DateTime date, string category, string? description);
    Task<bool> DeleteAsync(string id, string userId);
}
