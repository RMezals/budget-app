using BudgetApp.Api.Modules.Dashboard.Models;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using Microsoft.Extensions.Logging;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public class SpendingTrendService(
    ITransactionRepository txRepo,
    ILogger<SpendingTrendService> logger) : ISpendingTrendService
{
    private const int MaxMonths = 24;

    public async Task<List<SpendingTrendPoint>> GetSpendingTrendAsync(string userId, int months = 12)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        var clampedMonths = Math.Clamp(months, 1, MaxMonths);

        try
        {
            logger.LogInformation(
                "Getting spending trend for user {UserId}, past {Months} months", userId, clampedMonths);

            var now = DateTime.UtcNow;
            var rangeStart = DashboardHelper.GetMonthStart(now.AddMonths(-(clampedMonths - 1)));
            var rangeEnd = DashboardHelper.GetMonthStart(now).AddMonths(1); // exclusive end of current month

            var transactions = await txRepo.GetByRangeAsync(userId, rangeStart, rangeEnd);

            return BuildTrendPoints(transactions, rangeStart, clampedMonths);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            logger.LogError(ex, "Failed to get spending trend for user {UserId}", userId);
            throw;
        }
    }

    private static List<SpendingTrendPoint> BuildTrendPoints(
        List<Transaction> transactions,
        DateTime rangeStart,
        int months)
    {
        var expensesByMonth = DashboardHelper.GetExpenseTransactions(transactions)
            .GroupBy(t => t.Date.ToString("yyyy-MM"))
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(t => t.Category ?? "Other")
                       .ToDictionary(
                           c => c.Key,
                           c => Math.Abs(c.Sum(t => t.Amount))));

        return Enumerable.Range(0, months)
            .Select(i =>
            {
                var month = rangeStart.AddMonths(i).ToString("yyyy-MM");
                return new SpendingTrendPoint
                {
                    Month = month,
                    Expenses = expensesByMonth.GetValueOrDefault(month, new Dictionary<string, decimal>()),
                };
            })
            .ToList();
    }
}
