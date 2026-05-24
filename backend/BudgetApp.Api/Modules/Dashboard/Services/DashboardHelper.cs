using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Modules.Transactions.Models;

namespace BudgetApp.Api.Modules.Dashboard.Services;

/// <summary>
/// Shared helper methods for dashboard operations to avoid code duplication
/// </summary>
public static class DashboardHelper
{
    /// <summary>
    /// Returns the UTC start of the month containing <paramref name="date"/>.
    /// </summary>
    public static DateTime GetMonthStart(DateTime date) =>
        new(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Calculates the start and end dates for the current month.
    /// The end is exclusive (first instant of the following month).
    /// </summary>
    public static (DateTime monthStart, DateTime monthEnd) GetCurrentMonthRange(DateTime now)
    {
        var monthStart = GetMonthStart(now);
        var monthEnd = monthStart.AddMonths(1);
        return (monthStart, monthEnd);
    }

    /// <summary>
    /// Fetches portfolio data and computes net worth
    /// </summary>
    public static async Task<NetWorthSnapshot> GetNetWorthAsync(
        IPortfolioService portfolioService,
        string userId,
        DateTime now)
    {
        var (assets, liabilities) = await portfolioService.GetAllAsync(userId);
        return portfolioService.ComputeNetWorth(assets, liabilities, now);
    }

    /// <summary>
    /// Filters transactions to get income transactions (Amount > 0)
    /// </summary>
    public static IEnumerable<Transaction> GetIncomeTransactions(List<Transaction> transactions)
    {
        return transactions.Where(t => t.Amount > 0);
    }

    /// <summary>
    /// Filters transactions to get expense transactions (Amount < 0)
    /// </summary>
    public static IEnumerable<Transaction> GetExpenseTransactions(List<Transaction> transactions)
    {
        return transactions.Where(t => t.Amount < 0);
    }

    /// <summary>
    /// Calculates total income from transactions
    /// </summary>
    public static decimal CalculateIncome(List<Transaction> transactions)
    {
        return GetIncomeTransactions(transactions).Sum(t => t.Amount);
    }

    /// <summary>
    /// Calculates total expenses from transactions (as positive value)
    /// </summary>
    public static decimal CalculateExpenses(List<Transaction> transactions)
    {
        return Math.Abs(GetExpenseTransactions(transactions).Sum(t => t.Amount));
    }
}
