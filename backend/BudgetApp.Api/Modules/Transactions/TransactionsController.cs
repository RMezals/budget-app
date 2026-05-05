using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Transactions.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Transactions;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(IMongoDatabase db) : ApiControllerBase
{
    private readonly IMongoCollection<Transaction> _col =
        db.GetCollection<Transaction>("transactions");

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? category,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? keyword)
    {
        var filter = Builders<Transaction>.Filter.Eq(t => t.UserId, UserId);
        if (from.HasValue)        filter &= Builders<Transaction>.Filter.Gte(t => t.Date, from.Value);
        if (to.HasValue)          filter &= Builders<Transaction>.Filter.Lte(t => t.Date, to.Value);
        if (category is not null) filter &= Builders<Transaction>.Filter.Eq(t => t.Category, category);
        if (minAmount.HasValue)   filter &= Builders<Transaction>.Filter.Gte(t => t.Amount, minAmount.Value);
        if (maxAmount.HasValue)   filter &= Builders<Transaction>.Filter.Lte(t => t.Amount, maxAmount.Value);
        if (keyword is not null)  filter &= Builders<Transaction>.Filter.Regex(t => t.Description, keyword);

        var results = await _col.Find(filter).SortByDescending(t => t.Date).ToListAsync();
        return Ok(results);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransactionRequest request)
    {
        if (!Categories.IsValid(request.Category))
            return BadRequest(new { error = "Invalid category." });

        var tx = new Transaction
        {
            UserId      = UserId,
            Amount      = request.Amount,
            Date        = request.Date,
            Category    = request.Category,
            Description = request.Description
        };
        await _col.InsertOneAsync(tx);
        return CreatedAtAction(nameof(GetAll), new { }, tx);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] TransactionRequest request)
    {
        if (!Categories.IsValid(request.Category))
            return BadRequest(new { error = "Invalid category." });

        var update = Builders<Transaction>.Update
            .Set(t => t.Amount,      request.Amount)
            .Set(t => t.Date,        request.Date)
            .Set(t => t.Category,    request.Category)
            .Set(t => t.Description, request.Description);

        var result = await _col.UpdateOneAsync(t => t.Id == id && t.UserId == UserId, update);
        return result.MatchedCount == 0 ? NotFound() : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _col.DeleteOneAsync(t => t.Id == id && t.UserId == UserId);
        return result.DeletedCount == 0 ? NotFound() : NoContent();
    }

    [HttpGet("categories")]
    public IActionResult GetCategories() =>
        Ok(new { expense = Categories.Expense, income = Categories.Income });
}

public record TransactionRequest(decimal Amount, DateTime Date, string Category, string? Description);
