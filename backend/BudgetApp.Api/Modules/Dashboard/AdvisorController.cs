using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Dashboard.Services;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Transactions.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Dashboard;

[ApiController]
[Route("api/advisor")]
public class AdvisorController(
    IMongoDatabase db,
    [FromKeyedServices("claude")] IAiAdvisor claudeAdvisor,
    [FromKeyedServices("ollama")] IAiAdvisor ollamaAdvisor) : ApiControllerBase
{
    private readonly IMongoCollection<Transaction> _txs     = db.GetCollection<Transaction>("transactions");
    private readonly IMongoCollection<Budget>      _budgets = db.GetCollection<Budget>("budgets");
    private readonly IMongoCollection<SavingsGoal> _goals   = db.GetCollection<SavingsGoal>("savings_goals");

    public record AnalyseRequest(string Provider = "ollama", List<string>? Goals = null);

    // Tips are generated fresh on each request and are not persisted
    [HttpPost("analyse")]
    public async Task<IActionResult> Analyse([FromBody] AnalyseRequest request)
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd   = monthStart.AddMonths(1);

        var monthTxs    = await _txs.Find(t => t.UserId == UserId && t.Date >= monthStart && t.Date < monthEnd).ToListAsync();
        var budgets     = await _budgets.Find(b => b.UserId == UserId && b.Date == monthStart).ToListAsync();
        var activeGoals = await _goals.Find(g => g.UserId == UserId && g.Status == GoalStatus.Active).ToListAsync();

        var income   = monthTxs.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var expenses = Math.Abs(monthTxs.Where(t => t.Amount < 0).Sum(t => t.Amount));
        var spendByCategory = monthTxs
            .Where(t => t.Amount < 0)
            .GroupBy(t => t.Category)
            .Select(g => $"{g.Key}: {Math.Abs(g.Sum(t => t.Amount)):F2}");

        var summary = $"""
            Monthly income: {income:F2}
            Monthly expenses: {expenses:F2}
            Spending by category: {string.Join(", ", spendByCategory)}
            Active savings goals: {activeGoals.Count}
            Goals details: {string.Join("; ", activeGoals.Select(g => $"{g.Name} ({g.CurrentAmount:F2}/{g.TargetAmount:F2})"))}
            Budget limits set: {budgets.Count}
            """;

        // Map goal IDs to readable text
        var goalDescriptions = new Dictionary<string, string>
        {
            ["save_more"] = "save more money",
            ["reduce_expenses"] = "reduce expenses",
            ["invest"] = "start investing",
            ["emergency_fund"] = "build an emergency fund",
            ["pay_debt"] = "pay off debt",
            ["budget_better"] = "budget better"
        };

        var userGoals = request.Goals != null && request.Goals.Count > 0
            ? string.Join(" and ", request.Goals.Select(g => goalDescriptions.GetValueOrDefault(g, g)))
            : "improve their overall financial health";

        IAiAdvisor advisor = request.Provider == "claude" ? claudeAdvisor : ollamaAdvisor;
        var tips = await advisor.AnalyseAsync(summary, userGoals);
        return Ok(new { provider = request.Provider, tips });
    }
}
