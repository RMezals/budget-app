using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Transactions.Repositories;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public class AdvisorService(
    ITransactionRepository txRepo,
    IBudgetRepository budgetRepo,
    ISavingsGoalRepository goalRepo) : IAdvisorService
{
    public async Task<string> BuildFinancialSummaryAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var monthTxs = await txRepo.GetByMonthAsync(userId, monthStart, monthEnd);
        var budgets = await budgetRepo.GetByMonthAsync(userId, monthStart);
        var activeGoals = await goalRepo.GetActiveByUserAsync(userId);

        var income = monthTxs.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var expenses = Math.Abs(monthTxs.Where(t => t.Amount < 0).Sum(t => t.Amount));
        var spendByCategory = monthTxs
            .Where(t => t.Amount < 0)
            .GroupBy(t => t.Category)
            .Select(g => $"{g.Key}: {Math.Abs(g.Sum(t => t.Amount)):F2}");

        return $"""
            Monthly income: {income:F2}
            Monthly expenses: {expenses:F2}
            Spending by category: {string.Join(", ", spendByCategory)}
            Active savings goals: {activeGoals.Count}
            Goals details: {string.Join("; ", activeGoals.Select(g => $"{g.Name} ({g.CurrentAmount:F2}/{g.TargetAmount:F2})"))}
            Budget limits set: {budgets.Count}
            """;
    }
}
