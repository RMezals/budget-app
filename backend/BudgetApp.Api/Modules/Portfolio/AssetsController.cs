using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Portfolio.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Portfolio;

[ApiController]
[Route("api/assets")]
public class AssetsController(IMongoDatabase db) : ApiControllerBase
{
    private readonly IMongoCollection<Asset> _assets = db.GetCollection<Asset>("assets");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var assets = await _assets.Find(a => a.UserId == UserId).ToListAsync();
        return Ok(assets);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var asset = await _assets.Find(a => a.Id == id && a.UserId == UserId).FirstOrDefaultAsync();
        return asset is null ? NotFound() : Ok(asset);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssetRequest request)
    {
        var asset = new Asset
        {
            UserId        = UserId,
            Name          = request.Name,
            Type          = request.Type,
            Quantity      = request.Quantity,
            PurchasePrice = request.PurchasePrice,
            PurchaseDate  = request.PurchaseDate,
            Price         = [new PriceEntry { Value = request.PurchasePrice, Date = request.PurchaseDate }]
        };
        await _assets.InsertOneAsync(asset);
        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateAssetRequest request)
    {
        var update = Builders<Asset>.Update
            .Set(a => a.Name,     request.Name)
            .Set(a => a.Type,     request.Type)
            .Set(a => a.Quantity, request.Quantity);
        var result = await _assets.UpdateOneAsync(a => a.Id == id && a.UserId == UserId, update);
        return result.MatchedCount == 0 ? NotFound() : NoContent();
    }

    // Append a new price entry — never modifies existing entries
    [HttpPost("{id}/prices")]
    public async Task<IActionResult> AddPrice(string id, [FromBody] AddPriceRequest request)
    {
        var entry  = new PriceEntry { Value = request.Value, Date = request.Date };
        var update = Builders<Asset>.Update.Push(a => a.Price, entry);
        var result = await _assets.UpdateOneAsync(a => a.Id == id && a.UserId == UserId, update);
        return result.MatchedCount == 0 ? NotFound() : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _assets.DeleteOneAsync(a => a.Id == id && a.UserId == UserId);
        return result.DeletedCount == 0 ? NotFound() : NoContent();
    }
}

public record CreateAssetRequest(string Name, string Type, decimal Quantity, decimal PurchasePrice, DateTime PurchaseDate);
public record UpdateAssetRequest(string Name, string Type, decimal Quantity);
public record AddPriceRequest(decimal Value, DateTime Date);
