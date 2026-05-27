using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Transactions.Models;
using BudgetApp.Api.Modules.Transactions.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Transactions;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(ITransactionRepository repo) : ApiControllerBase
{
    // Returns all transactions for the authenticated user, optionally filtered by date range, category, amount, or keyword
    [HttpGet]
    [ProducesResponseType(typeof(List<Transaction>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? category,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? keyword)
    {
        // Pack all optional query parameters into a filter object before passing to the repository
        var filter = new TransactionFilter(from, to, category, minAmount, maxAmount, keyword);
        var results = await repo.GetAllAsync(UserId, filter);
        return Ok(results);
    }

    // Creates a new transaction for the authenticated user and returns the created record
    [HttpPost]
    [ProducesResponseType(typeof(Transaction), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] TransactionRequest request)
    {
        // Reject unknown categories before writing to the database
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

    // Updates an existing transaction by ID; returns 404 if not found or not owned by the user
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] TransactionRequest request)
    {
        if (!Categories.IsValid(request.Category))
            return BadRequest(new { error = "Invalid category." });

        var updated = await repo.UpdateAsync(id, UserId, request.Amount, request.Date, request.Category, request.Description);
        // NoContent (204) on success; NotFound (404) when the record doesn't exist or belongs to another user
        return updated ? NoContent() : NotFound();
    }

    // Deletes a transaction by ID; returns 404 if not found or not owned by the user
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await repo.DeleteAsync(id, UserId);
        return deleted ? NoContent() : NotFound();
    }

    // Returns the lists of valid expense and income category strings
    [HttpGet("categories")]
    [ProducesResponseType(typeof(TransactionCategoriesResponse), StatusCodes.Status200OK)]
    public IActionResult GetCategories() =>
        Ok(new TransactionCategoriesResponse(Categories.Expense, Categories.Income));
}

public record TransactionRequest(decimal Amount, DateTime Date, string Category, string? Description);
public record TransactionCategoriesResponse(IReadOnlyList<string> Expense, IReadOnlyList<string> Income);
