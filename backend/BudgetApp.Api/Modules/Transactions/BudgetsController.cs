using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Transactions.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Transactions;

[ApiController]
[Route("api/budgets")]
public class BudgetsController(IMongoDatabase db) : ApiControllerBase
{
    private readonly IMongoCollection<Budget>      _budgets = db.GetCollection<Budget>("budgets");
    private readonly IMongoCollection<Transaction> _txs     = db.GetCollection<Transaction>("transactions");

    [HttpGet]
    public async Task<IActionResult> GetByMonth([FromQuery] int year, [FromQuery] int month)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var budgets = await _budgets
            .Find(b => b.UserId == UserId && b.Date == monthStart)
            .ToListAsync();
        return Ok(budgets);
    }

    // Upsert: create or update a budget limit for a category/month
    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpsertBudgetRequest request)
    {
        if (!Categories.Expense.Contains(request.Category))
            return BadRequest(new { error = "Invalid expense category." });

        var monthStart = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var filter = Builders<Budget>.Filter.And(
            Builders<Budget>.Filter.Eq(b => b.UserId,   UserId),
            Builders<Budget>.Filter.Eq(b => b.Category, request.Category),
            Builders<Budget>.Filter.Eq(b => b.Date,     monthStart));

        var update = Builders<Budget>.Update
            .SetOnInsert(b => b.UserId,   UserId)
            .SetOnInsert(b => b.Category, request.Category)
            .SetOnInsert(b => b.Date,     monthStart)
            .Set(b => b.LimitAmount,      request.LimitAmount);

        await _budgets.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        return NoContent();
    }

    // Budget usage is computed on demand from transaction data — never stored
    [HttpGet("usage")]
    public async Task<IActionResult> GetUsage([FromQuery] int year, [FromQuery] int month)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd   = monthStart.AddMonths(1);

        var budgets  = await _budgets.Find(b => b.UserId == UserId && b.Date == monthStart).ToListAsync();
        var expenses = await _txs
            .Find(t => t.UserId == UserId && t.Date >= monthStart && t.Date < monthEnd && t.Amount < 0)
            .ToListAsync();

        var usage = budgets.Select(b =>
        {
            var spent = Math.Abs(expenses.Where(t => t.Category == b.Category).Sum(t => t.Amount));
            return new
            {
                b.Category,
                Limit   = b.LimitAmount,
                Spent   = spent,
                Remaining    = b.LimitAmount - spent,
                UsagePercent = b.LimitAmount > 0 ? Math.Round(spent / b.LimitAmount * 100, 1) : 0m
            };
        });
        return Ok(usage);
    }
}

public record UpsertBudgetRequest(int Year, int Month, string Category, decimal LimitAmount);
