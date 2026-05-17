using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Transactions;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(ITransactionRepository repo) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? category,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? keyword)
    {
        var filter = new TransactionFilter(from, to, category, minAmount, maxAmount, keyword);
        var results = await repo.GetAllAsync(UserId, filter);
        return Ok(results);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransactionRequest request)
    {
        if (!Categories.IsValid(request.Category))
            return BadRequest(new { error = "Invalid category." });

        var tx = new Transaction
        {
            UserId = UserId,
            Amount = request.Amount,
            Date = request.Date,
            Category = request.Category,
            Description = request.Description
        };
        await repo.InsertAsync(tx);
        return CreatedAtAction(nameof(GetAll), new { }, tx);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] TransactionRequest request)
    {
        if (!Categories.IsValid(request.Category))
            return BadRequest(new { error = "Invalid category." });

        var updated = await repo.UpdateAsync(id, UserId, request.Amount, request.Date, request.Category, request.Description);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await repo.DeleteAsync(id, UserId);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("categories")]
    public IActionResult GetCategories() =>
        Ok(new { expense = Categories.Expense, income = Categories.Income });
}

public record TransactionRequest(decimal Amount, DateTime Date, string Category, string? Description);
