using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Shared;
using BudgetApp.Api.Modules.Reports.Models;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Transactions.Repositories;

namespace BudgetApp.Api.Modules.Reports.Services;

// Builds the monthly financial report by aggregating transactions, savings contributions, and portfolio data
public class MonthlyReportService(
    ITransactionRepository txRepo,
    ISavingsGoalRepository goalRepo,
    IGoalContributionRepository contributionRepo,
    IPortfolioService portfolioService) : IMonthlyReportService
{
    // Generates a full monthly report for the given user, year, and month
    public async Task<MonthlyReport> GetMonthlyReportAsync(string userId, int year, int month)
    {
        // monthEnd is exclusive — it equals the first instant of the next month
        var monthStart = FinancialCalculations.GetMonthStart(year, month);
        var monthEnd = monthStart.AddMonths(1);

        var transactions = await txRepo.GetByMonthAsync(userId, monthStart, monthEnd);
        var contributions = await contributionRepo.GetByUserAndMonthAsync(userId, monthStart, monthEnd);

        // Build a name lookup so contribution summaries can show goal names instead of IDs
        var allGoals = await goalRepo.GetByUserAsync(userId);
        var goalNames = allGoals.ToDictionary(g => g.Id, g => g.Name);

        // Group expense transactions by category and sum absolute values for display
        var expensesByCategory = FinancialCalculations.GetExpenseTransactions(transactions)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => Math.Abs(g.Sum(t => t.Amount)));

        // Group income transactions by category and sum their amounts
        var incomeByCategory = FinancialCalculations.GetIncomeTransactions(transactions)
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        // Build per-goal savings summaries: separate deposits and withdrawals so both are visible
        var savingsContributions = contributions
            .GroupBy(c => c.GoalId)
            .Select(g =>
            {
                var deposits = g.Where(c => c.Amount > 0).Sum(c => c.Amount);
                var withdrawals = Math.Abs(g.Where(c => c.Amount < 0).Sum(c => c.Amount));
                return new GoalContributionSummary
                {
                    GoalId = g.Key,
                    // Fall back to "Unknown Goal" if the goal was deleted after the contribution was made
                    GoalName = goalNames.TryGetValue(g.Key, out var name) ? name : "Unknown Goal",
                    TotalDeposited = deposits,
                    TotalWithdrawn = withdrawals,
                    ContributionCount = g.Count()
                };
            })
            // Show the goals with the highest net contributions first
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
            TotalIncome = FinancialCalculations.CalculateIncome(transactions),
            TotalExpenses = FinancialCalculations.CalculateExpenses(transactions),
            ExpensesByCategory = expensesByCategory,
            IncomeByCategory = incomeByCategory,
            SavingsContributions = savingsContributions,
            // Use NetWorth (assets minus liabilities) so users with debt see the correct picture
            PortfolioChange = new PortfolioChangeSummary
            {
                StartValue = startNetWorth.NetWorth,
                EndValue = endNetWorth.NetWorth,
            }
        };
    }
}
