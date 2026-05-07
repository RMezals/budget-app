using BudgetApp.Api.Modules.Transactions.Repositories;

namespace BudgetApp.Api.Modules.Transactions.Services;

public class BudgetService(IBudgetRepository budgetRepo, ITransactionRepository txRepo) : IBudgetService
{
    public async Task<List<BudgetSpending>> GetUsageAsync(string userId, int year, int month)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd   = monthStart.AddMonths(1);

        var budgets  = await budgetRepo.GetByMonthAsync(userId, monthStart);
        var expenses = await txRepo.GetByMonthAsync(userId, monthStart, monthEnd);

        return budgets.Select(b =>
        {
            var spent = Math.Abs(expenses
                .Where(t => t.Category == b.Category && t.Amount < 0)
                .Sum(t => t.Amount));
            return new BudgetSpending(b.Category, b.LimitAmount, spent);
        }).ToList();
    }
}
