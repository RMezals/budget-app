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
    // Returns all budget limits set for the given calendar month
    [HttpGet]
    [ProducesResponseType(typeof(List<Budget>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByMonth([FromQuery] int year, [FromQuery] int month)
    {
        // Validate before passing to DateTime — the constructor throws an uncaught exception for out-of-range values
        if (month < 1 || month > 12 || year < 1)
            return BadRequest(new { error = "Invalid year or month." });

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

        // Validate year/month and limit amount before touching DateTime or the DB
        if (request.Month < 1 || request.Month > 12 || request.Year < 1)
            return BadRequest(new { error = "Invalid year or month." });

        if (request.LimitAmount < 0)
            return BadRequest(new { error = "Budget limit cannot be negative." });

        var monthStart = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        await budgetRepo.UpsertAsync(UserId, request.Category, monthStart, request.LimitAmount);
        return NoContent();
    }

    // Budget usage is computed on demand from transaction data — never stored
    [HttpGet("usage")]
    [ProducesResponseType(typeof(List<BudgetUsageResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsage([FromQuery] int year, [FromQuery] int month)
    {
        if (month < 1 || month > 12 || year < 1)
            return BadRequest(new { error = "Invalid year or month." });

        var spending = await budgetService.GetUsageAsync(UserId, year, month);

        // Remaining can be negative when spending exceeds the limit (intentional — shows overspend)
        // UsagePercent is 0 when no limit is set to avoid division by zero
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
