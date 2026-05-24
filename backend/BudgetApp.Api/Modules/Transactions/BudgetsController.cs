using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using BudgetApp.Api.Modules.Transactions.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Transactions;

[ApiController]
[Route("api/budgets")]
public class BudgetsController(IBudgetRepository budgetRepo, IBudgetService budgetService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<Budget>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByMonth([FromQuery] int year, [FromQuery] int month)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var budgets = await budgetRepo.GetByMonthAsync(UserId, monthStart);
        return Ok(budgets);
    }

    // Upsert: create or update a budget limit for a category/month
    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpsertBudgetRequest request)
    {
        if (!Categories.Expense.Contains(request.Category))
            return BadRequest(new { error = "Invalid expense category." });

        var monthStart = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        await budgetRepo.UpsertAsync(UserId, request.Category, monthStart, request.LimitAmount);
        return NoContent();
    }

    // Budget usage is computed on demand from transaction data — never stored
    [HttpGet("usage")]
    [ProducesResponseType(typeof(List<BudgetUsageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsage([FromQuery] int year, [FromQuery] int month)
    {
        var spending = await budgetService.GetUsageAsync(UserId, year, month);
        var usage = spending.Select(b => new BudgetUsageResponse(
            b.Category,
            b.Limit,
            b.Spent,
            b.Limit - b.Spent,
            b.Limit > 0 ? Math.Round(b.Spent / b.Limit * 100, 1) : 0m));
        return Ok(usage);
    }
}

public record UpsertBudgetRequest(int Year, int Month, string Category, decimal LimitAmount);
public record BudgetUsageResponse(string Category, decimal Limit, decimal Spent, decimal Remaining, decimal UsagePercent);
