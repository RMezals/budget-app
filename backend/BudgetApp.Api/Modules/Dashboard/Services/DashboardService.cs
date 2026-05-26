using BudgetApp.Api.Modules.Dashboard.Models;
using BudgetApp.Api.Modules.Portfolio.Services;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using BudgetApp.Api.Shared;
using Microsoft.Extensions.Logging;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public class DashboardService(
    IPortfolioService portfolioService,
    ITransactionRepository txRepo,
    IBudgetRepository budgetRepo,
    ISavingsGoalRepository goalRepo,
    IGoalContributionRepository contributionRepo,
    IGoalProjectionCalculator projectionCalculator,
    ILogger<DashboardService> logger) : IDashboardService
{
    public async Task<DashboardSummary> GetSummaryAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        try
        {
            logger.LogInformation("Getting dashboard summary for user {UserId}", userId);
            var now = DateTime.UtcNow;
            var (monthStart, monthEnd) = FinancialCalculations.GetCurrentMonthRange(now);

            var netWorth = await FinancialCalculations.GetNetWorthAsync(portfolioService, userId, now);

            var monthTxs = await txRepo.GetByMonthAsync(userId, monthStart, monthEnd);
            var budgets = await budgetRepo.GetByMonthAsync(userId, monthStart);
            var activeGoals = await goalRepo.GetActiveByUserAsync(userId);

            var budgetUsage = CalculateBudgetUsage(budgets, monthTxs);

            var goalProgress = await BuildGoalProgressListAsync(activeGoals, userId, now);

            return new DashboardSummary
            {
                NetWorth = netWorth.NetWorth,
                TotalInvested = netWorth.TotalAssets,
                TotalSaved = activeGoals.Sum(g => g.CurrentAmount),
                MonthlyIncome = FinancialCalculations.CalculateIncome(monthTxs),
                MonthlyExpenses = FinancialCalculations.CalculateExpenses(monthTxs),
                BudgetUsage = budgetUsage,
                ActiveGoals = goalProgress
            };
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            logger.LogError(ex, "Failed to get dashboard summary for user {UserId}", userId);
            throw;
        }
    }

    private async Task<List<GoalProgress>> BuildGoalProgressListAsync(
        List<SavingsGoal> goals,
        string userId,
        DateTime now)
    {
        if (goals.Count == 0)
        {
            return new List<GoalProgress>();
        }

        // Batch load all contributions to avoid N+1 problem
        var goalIds = goals.Select(g => g.Id).ToList();
        var allContributions = await contributionRepo.GetByGoalsAsync(goalIds, userId);
        var contributionsByGoal = allContributions
            .GroupBy(c => c.GoalId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return goals.Select(goal => CreateGoalProgress(goal, contributionsByGoal, now)).ToList();
    }

    private GoalProgress CreateGoalProgress(
        SavingsGoal goal,
        Dictionary<string, List<GoalContribution>> contributionsByGoal,
        DateTime now)
    {
        contributionsByGoal.TryGetValue(goal.Id, out var contributions);
        var projectedCompletion = projectionCalculator.CalculateProjectedCompletion(
            goal,
            contributions ?? new List<GoalContribution>(),
            now);

        return new GoalProgress
        {
            GoalId = goal.Id,
            Name = goal.Name,
            CurrentAmount = goal.CurrentAmount,
            TargetAmount = goal.TargetAmount,
            ProjectedCompletion = projectedCompletion
        };
    }

    private static List<BudgetUsage> CalculateBudgetUsage(
        List<Budget> budgets,
        List<Transaction> transactions)
    {
        return budgets.Select(b =>
        {
            var spent = FinancialCalculations.CalculateSpentInCategory(transactions, b.Category);
            return new BudgetUsage { Category = b.Category, Limit = b.LimitAmount, Spent = spent };
        }).ToList();
    }
}
