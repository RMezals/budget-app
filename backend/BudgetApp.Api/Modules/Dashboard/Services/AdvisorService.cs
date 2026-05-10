using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using Microsoft.Extensions.Logging;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public class AdvisorService(
    IPortfolioService portfolioService,
    ITransactionRepository txRepo,
    IBudgetRepository budgetRepo,
    ISavingsGoalRepository goalRepo,
    ILogger<AdvisorService> logger) : IAdvisorService
{
    public async Task<string> BuildFinancialSummaryAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        try
        {
            logger.LogInformation("Building financial summary for user {UserId}", userId);
            var now = DateTime.UtcNow;
            var (monthStart, monthEnd) = DashboardHelper.GetCurrentMonthRange(now);

            var netWorth = await DashboardHelper.GetNetWorthAsync(portfolioService, userId, now);

            var monthTxs = await txRepo.GetByMonthAsync(userId, monthStart, monthEnd);
            var budgets = await budgetRepo.GetByMonthAsync(userId, monthStart);
            var activeGoals = await goalRepo.GetActiveByUserAsync(userId);

            var income = DashboardHelper.CalculateIncome(monthTxs);
            var expenses = DashboardHelper.CalculateExpenses(monthTxs);
            var spendByCategory = FormatSpendingByCategory(monthTxs);

            return BuildSummaryText(netWorth, income, expenses, spendByCategory, activeGoals, budgets.Count);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            logger.LogError(ex, "Failed to build financial summary for user {UserId}", userId);
            throw;
        }
    }

    private static IEnumerable<string> FormatSpendingByCategory(List<Transaction> transactions)
    {
        return DashboardHelper.GetExpenseTransactions(transactions)
            .GroupBy(t => t.Category ?? "Uncategorized")
            .Select(g => FormattableString.Invariant($"{g.Key}: {Math.Abs(g.Sum(t => t.Amount)):F2}"));
    }

    private static string BuildSummaryText(
        NetWorthSnapshot netWorth,
        decimal income,
        decimal expenses,
        IEnumerable<string> spendByCategory,
        List<SavingsGoal> activeGoals,
        int budgetCount)
    {
        return FormattableString.Invariant($"""
            Net worth: {netWorth.NetWorth:F2}
            Total assets: {netWorth.TotalAssets:F2}
            Total liabilities: {netWorth.TotalLiabilities:F2}
            Monthly income: {income:F2}
            Monthly expenses: {expenses:F2}
            Spending by category: {string.Join(", ", spendByCategory)}
            Active savings goals: {activeGoals.Count}
            Goals details: {string.Join("; ", activeGoals.Select(g => FormattableString.Invariant($"{g.Name} ({g.CurrentAmount:F2}/{g.TargetAmount:F2})")))}
            Budget limits set: {budgetCount}
            """);
    }
}
