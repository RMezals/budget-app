using BudgetApp.Api.Modules.Dashboard.Models;
using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Transactions.Repositories;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public class DashboardService(
    IPortfolioService portfolioService,
    ITransactionRepository txRepo,
    IBudgetRepository budgetRepo,
    ISavingsGoalRepository goalRepo) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(string userId)
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd   = monthStart.AddMonths(1);

        var (assets, liabilities) = await portfolioService.GetAllAsync(userId);
        var netWorth   = portfolioService.ComputeNetWorth(assets, liabilities, now);

        var monthTxs    = await txRepo.GetByMonthAsync(userId, monthStart, monthEnd);
        var budgets     = await budgetRepo.GetByMonthAsync(userId, monthStart);
        var activeGoals = await goalRepo.GetActiveByUserAsync(userId);

        var budgetUsage = budgets.Select(b =>
        {
            var spent = Math.Abs(monthTxs
                .Where(t => t.Category == b.Category && t.Amount < 0)
                .Sum(t => t.Amount));
            return new BudgetUsage { Category = b.Category, Limit = b.LimitAmount, Spent = spent };
        }).ToList();

        var goalProgress = activeGoals.Select(g => new GoalProgress
        {
            GoalId        = g.Id,
            Name          = g.Name,
            CurrentAmount = g.CurrentAmount,
            TargetAmount  = g.TargetAmount
        }).ToList();

        return new DashboardSummary
        {
            NetWorth        = netWorth.NetWorth,
            TotalInvested   = netWorth.TotalAssets,
            TotalSaved      = activeGoals.Sum(g => g.CurrentAmount),
            MonthlyIncome   = monthTxs.Where(t => t.Amount > 0).Sum(t => t.Amount),
            MonthlyExpenses = Math.Abs(monthTxs.Where(t => t.Amount < 0).Sum(t => t.Amount)),
            BudgetUsage     = budgetUsage,
            ActiveGoals     = goalProgress
        };
    }
}
