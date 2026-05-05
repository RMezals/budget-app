using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Portfolio.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Portfolio;

[ApiController]
[Route("api/liabilities")]
public class LiabilitiesController(IMongoDatabase db) : ApiControllerBase
{
    private readonly IMongoCollection<Liability> _liabilities = db.GetCollection<Liability>("liabilities");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _liabilities.Find(l => l.UserId == UserId).ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var item = await _liabilities.Find(l => l.Id == id && l.UserId == UserId).FirstOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLiabilityRequest request)
    {
        var liability = new Liability
        {
            UserId = UserId,
            Name   = request.Name,
            Type   = request.Type,
            Amount = [new AmountEntry { Value = request.InitialAmount, Date = request.Date }]
        };
        await _liabilities.InsertOneAsync(liability);
        return CreatedAtAction(nameof(GetById), new { id = liability.Id }, liability);
    }

    // Append a new balance entry — never modifies existing entries
    [HttpPost("{id}/amounts")]
    public async Task<IActionResult> AddAmount(string id, [FromBody] AddAmountRequest request)
    {
        var entry  = new AmountEntry { Value = request.Value, Date = request.Date };
        var update = Builders<Liability>.Update.Push(l => l.Amount, entry);
        var result = await _liabilities.UpdateOneAsync(l => l.Id == id && l.UserId == UserId, update);
        return result.MatchedCount == 0 ? NotFound() : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _liabilities.DeleteOneAsync(l => l.Id == id && l.UserId == UserId);
        return result.DeletedCount == 0 ? NotFound() : NoContent();
    }
}

public record CreateLiabilityRequest(string Name, string Type, decimal InitialAmount, DateTime Date);
public record AddAmountRequest(decimal Value, DateTime Date);
