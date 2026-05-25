using BudgetApp.Api.Modules.Dashboard.Services;
using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Modules.Reports.Models;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Transactions.Repositories;

namespace BudgetApp.Api.Modules.Reports.Services;

public class MonthlyReportService(
    ITransactionRepository txRepo,
    ISavingsGoalRepository goalRepo,
    IGoalContributionRepository contributionRepo,
    IPortfolioService portfolioService) : IMonthlyReportService
{
    public async Task<MonthlyReport> GetMonthlyReportAsync(string userId, int year, int month)
    {
        var monthStart = DashboardHelper.GetMonthStart(year, month);
        var monthEnd = monthStart.AddMonths(1);

        var transactions = await txRepo.GetByMonthAsync(userId, monthStart, monthEnd);
        var contributions = await contributionRepo.GetByUserAndMonthAsync(userId, monthStart, monthEnd);

        var allGoals = await goalRepo.GetByUserAsync(userId);
        var goalNames = allGoals.ToDictionary(g => g.Id, g => g.Name);

        var expensesByCategory = DashboardHelper.GetExpenseTransactions(transactions)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => Math.Abs(g.Sum(t => t.Amount)));

        var incomeByCategory = DashboardHelper.GetIncomeTransactions(transactions)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        var savingsContributions = contributions
            .GroupBy(c => c.GoalId)
            .Select(g =>
            {
                var deposits = g.Where(c => c.Amount > 0).Sum(c => c.Amount);
                var withdrawals = Math.Abs(g.Where(c => c.Amount < 0).Sum(c => c.Amount));
                return new GoalContributionSummary
                {
                    GoalId = g.Key,
                    GoalName = goalNames.TryGetValue(g.Key, out var name) ? name : "Unknown Goal",
                    TotalDeposited = deposits,
                    TotalWithdrawn = withdrawals,
                    ContributionCount = g.Count()
                };
            })
            .OrderByDescending(s => s.NetContribution)
            .ToList();

        var (assets, liabilities) = await portfolioService.GetAllAsync(userId);
        var startNetWorth = portfolioService.ComputeNetWorth(assets, liabilities, monthStart);
        // For current or future months use now as the end reference so we don't peek into future prices
        var endReference = monthEnd > DateTime.UtcNow ? DateTime.UtcNow : monthEnd.AddSeconds(-1);
        var endNetWorth = portfolioService.ComputeNetWorth(assets, liabilities, endReference);

        return new MonthlyReport
        {
            Year = year,
            Month = month,
            TotalIncome = DashboardHelper.CalculateIncome(transactions),
            TotalExpenses = DashboardHelper.CalculateExpenses(transactions),
            ExpensesByCategory = expensesByCategory,
            IncomeByCategory = incomeByCategory,
            SavingsContributions = savingsContributions,
            PortfolioChange = new PortfolioChangeSummary
            {
                StartValue = startNetWorth.TotalAssets,
                EndValue = endNetWorth.TotalAssets,
            }
        };
    }
}
