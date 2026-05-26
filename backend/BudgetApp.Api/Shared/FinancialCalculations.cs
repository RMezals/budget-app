using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Modules.Transactions.Models;

namespace BudgetApp.Api.Shared;

public static class FinancialCalculations
{
    public static DateTime GetMonthStart(int year, int month) =>
        new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

    public static DateTime GetMonthStart(DateTime date) =>
        GetMonthStart(date.Year, date.Month);

    /// <summary>
    /// The end is exclusive (first instant of the following month).
    /// </summary>
    public static (DateTime monthStart, DateTime monthEnd) GetCurrentMonthRange(DateTime now)
    {
        var monthStart = GetMonthStart(now);
        var monthEnd = monthStart.AddMonths(1);
        return (monthStart, monthEnd);
    }

    public static async Task<NetWorthSnapshot> GetNetWorthAsync(
        IPortfolioService portfolioService,
        string userId,
        DateTime now)
    {
        var (assets, liabilities) = await portfolioService.GetAllAsync(userId);
        return portfolioService.ComputeNetWorth(assets, liabilities, now);
    }

    public static IEnumerable<Transaction> GetIncomeTransactions(List<Transaction> transactions) =>
        transactions.Where(t => t.Amount > 0);

    public static IEnumerable<Transaction> GetExpenseTransactions(List<Transaction> transactions) =>
        transactions.Where(t => t.Amount < 0);

    public static decimal CalculateIncome(List<Transaction> transactions) =>
        GetIncomeTransactions(transactions).Sum(t => t.Amount);

    /// <summary>
    /// Returns expenses as a positive value.
    /// </summary>
    public static decimal CalculateExpenses(List<Transaction> transactions) =>
        Math.Abs(GetExpenseTransactions(transactions).Sum(t => t.Amount));

    /// <summary>
    /// Returns the amount spent in a category as a positive value.
    /// </summary>
    public static decimal CalculateSpentInCategory(List<Transaction> transactions, string category) =>
        Math.Abs(GetExpenseTransactions(transactions)
            .Where(t => t.Category == category)
            .Sum(t => t.Amount));
}
