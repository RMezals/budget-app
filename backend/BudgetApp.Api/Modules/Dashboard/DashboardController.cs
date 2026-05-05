using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Dashboard.Models;
using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Transactions.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Dashboard;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(IMongoDatabase db) : ApiControllerBase
{
    private readonly IMongoCollection<Transaction>      _txs         = db.GetCollection<Transaction>("transactions");
    private readonly IMongoCollection<Budget>           _budgets     = db.GetCollection<Budget>("budgets");
    private readonly IMongoCollection<SavingsGoal>      _goals       = db.GetCollection<SavingsGoal>("savings_goals");
    private readonly IMongoCollection<Asset>            _assets      = db.GetCollection<Asset>("assets");
    private readonly IMongoCollection<Liability>        _liabilities = db.GetCollection<Liability>("liabilities");

    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd   = monthStart.AddMonths(1);

        // Net worth — fetch once, resolve in memory
        var assets      = await _assets.Find(a => a.UserId == UserId).ToListAsync();
        var liabilities = await _liabilities.Find(l => l.UserId == UserId).ToListAsync();
        var totalAssets      = assets.Sum(a => ResolvePrice(a.Price, now) * a.Quantity);
        var totalLiabilities = liabilities.Sum(l => ResolveAmount(l.Amount, now));

        // Monthly transactions
        var monthTxs = await _txs
            .Find(t => t.UserId == UserId && t.Date >= monthStart && t.Date < monthEnd)
            .ToListAsync();
        var monthlyIncome   = monthTxs.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var monthlyExpenses = Math.Abs(monthTxs.Where(t => t.Amount < 0).Sum(t => t.Amount));

        // Budget usage — computed on demand from transactions
        var budgets    = await _budgets.Find(b => b.UserId == UserId && b.Date == monthStart).ToListAsync();
        var budgetUsage = budgets.Select(b =>
        {
            var spent = Math.Abs(monthTxs
                .Where(t => t.Category == b.Category && t.Amount < 0)
                .Sum(t => t.Amount));
            return new BudgetUsage { Category = b.Category, Limit = b.LimitAmount, Spent = spent };
        }).ToList();

        // Active goals
        var activeGoals  = await _goals.Find(g => g.UserId == UserId && g.Status == GoalStatus.Active).ToListAsync();
        var goalProgress = activeGoals.Select(g => new GoalProgress
        {
            GoalId        = g.Id,
            Name          = g.Name,
            CurrentAmount = g.CurrentAmount,
            TargetAmount  = g.TargetAmount
        }).ToList();

        return Ok(new DashboardSummary
        {
            NetWorth        = totalAssets - totalLiabilities,
            TotalInvested   = totalAssets,
            TotalSaved      = activeGoals.Sum(g => g.CurrentAmount),
            MonthlyIncome   = monthlyIncome,
            MonthlyExpenses = monthlyExpenses,
            BudgetUsage     = budgetUsage,
            ActiveGoals     = goalProgress
        });
    }

    private static decimal ResolvePrice(List<PriceEntry> history, DateTime date) =>
        history.Where(e => e.Date <= date).MaxBy(e => e.Date)?.Value ?? 0;

    private static decimal ResolveAmount(List<AmountEntry> history, DateTime date) =>
        history.Where(e => e.Date <= date).MaxBy(e => e.Date)?.Value ?? 0;
}
