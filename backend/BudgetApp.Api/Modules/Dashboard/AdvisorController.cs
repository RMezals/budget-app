using BudgetApp.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Transactions.Models;

namespace BudgetApp.Api.Modules.Dashboard;

[ApiController]
[Route("api/advisor")]
public class AdvisorController(IMongoDatabase db, IConfiguration config, IHttpClientFactory httpClientFactory)
    : ApiControllerBase
{
    private readonly IMongoCollection<Transaction> _txs   = db.GetCollection<Transaction>("transactions");
    private readonly IMongoCollection<Budget>      _budgets = db.GetCollection<Budget>("budgets");
    private readonly IMongoCollection<SavingsGoal> _goals = db.GetCollection<SavingsGoal>("savings_goals");

    // Tips are generated fresh on each request and are not persisted
    [HttpPost("analyse")]
    public async Task<IActionResult> Analyse()
    {
        var now        = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd   = monthStart.AddMonths(1);

        var monthTxs     = await _txs.Find(t => t.UserId == UserId && t.Date >= monthStart && t.Date < monthEnd).ToListAsync();
        var budgets      = await _budgets.Find(b => b.UserId == UserId && b.Date == monthStart).ToListAsync();
        var activeGoals  = await _goals.Find(g => g.UserId == UserId && g.Status == GoalStatus.Active).ToListAsync();

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

        var tips = await CallAiAdvisor(summary);
        return Ok(new { tips });
    }

    private async Task<string> CallAiAdvisor(string financialSummary)
    {
        var apiKey = config["Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return "AI advisor is not configured.";

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var payload = new
        {
            model      = "claude-haiku-4-5-20251001",
            max_tokens = 1024,
            messages   = new[]
            {
                new
                {
                    role    = "user",
                    content = $"You are a personal finance advisor. Based on this financial summary, provide 3 concise, actionable tips:\n\n{financialSummary}"
                }
            }
        };

        var json     = JsonSerializer.Serialize(payload);
        var content  = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
        response.EnsureSuccessStatusCode();

        var body   = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(body);
        return parsed.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
    }
}
