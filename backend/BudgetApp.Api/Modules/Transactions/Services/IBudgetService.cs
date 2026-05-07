namespace BudgetApp.Api.Modules.Transactions.Services;

public record BudgetSpending(string Category, decimal Limit, decimal Spent);

public interface IBudgetService
{
    Task<List<BudgetSpending>> GetUsageAsync(string userId, int year, int month);
}
