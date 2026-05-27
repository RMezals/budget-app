using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Modules.Transactions.Models;

namespace BudgetApp.Api.Shared;

// Shared helpers for common financial calculations used across multiple modules
public static class FinancialCalculations
{
    // Returns midnight UTC on the first day of the given year/month
    public static DateTime GetMonthStart(int year, int month) =>
        new(year, month, 1, 0, 0, 0, DateTimeKind.Utc);

    // Overload that extracts year and month from an existing DateTime
    public static DateTime GetMonthStart(DateTime date) =>
        GetMonthStart(date.Year, date.Month);

    // Returns (inclusive start, exclusive end) for the calendar month that contains 'now'
    // The end is exclusive (first instant of the following month).
    public static (DateTime monthStart, DateTime monthEnd) GetCurrentMonthRange(DateTime now)
    {
        var monthStart = GetMonthStart(now);
        // AddMonths(1) gives the first instant of the next month, which is the exclusive upper bound
        var monthEnd = monthStart.AddMonths(1);
        return (monthStart, monthEnd);
    }

    // Fetches assets and liabilities from the portfolio service and returns a net worth snapshot for the given moment
    public static async Task<NetWorthSnapshot> GetNetWorthAsync(
        IPortfolioService portfolioService,
        string userId,
        DateTime now)
    {
        var (assets, liabilities) = await portfolioService.GetAllAsync(userId);
        return portfolioService.ComputeNetWorth(assets, liabilities, now);
    }

    // Filters the transaction list to only positive-amount (income) records
    public static IEnumerable<Transaction> GetIncomeTransactions(List<Transaction> transactions) =>
        transactions.Where(t => t.Amount > 0);

    // Filters the transaction list to only negative-amount (expense) records
    public static IEnumerable<Transaction> GetExpenseTransactions(List<Transaction> transactions) =>
        transactions.Where(t => t.Amount < 0);

    // Returns the total income as a positive value
    public static decimal CalculateIncome(List<Transaction> transactions) =>
        GetIncomeTransactions(transactions).Sum(t => t.Amount);

    // Returns expenses as a positive value.
    // Expense amounts are stored as negatives; Math.Abs converts them to a positive total
    public static decimal CalculateExpenses(List<Transaction> transactions) =>
        Math.Abs(GetExpenseTransactions(transactions).Sum(t => t.Amount));

    // Returns the amount spent in a category as a positive value.
    public static decimal CalculateSpentInCategory(List<Transaction> transactions, string category) =>
        Math.Abs(GetExpenseTransactions(transactions)
            .Where(t => t.Category == category)
            .Sum(t => t.Amount));
}
