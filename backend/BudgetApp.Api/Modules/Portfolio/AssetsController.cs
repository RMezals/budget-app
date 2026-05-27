using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Portfolio.Repositories;
using BudgetApp.Api.Modules.Portfolio.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Portfolio;

[ApiController]
[Route("api/assets")]
public class AssetsController(IAssetRepository repo, IPortfolioService portfolioService) : ApiControllerBase
{
    // Returns all assets owned by the authenticated user (raw records without computed values)
    [HttpGet]
    [ProducesResponseType(typeof(List<Asset>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var assets = await repo.GetByUserAsync(UserId);
        return Ok(assets);
    }

    // Returns all assets enriched with unrealised gain/loss computed from price history
    [HttpGet("summary")]
    [ProducesResponseType(typeof(List<AssetSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary()
    {
        var assets = await repo.GetByUserAsync(UserId);
        var now = DateTime.UtcNow;
        // ComputeAssetSummary resolves the current price from the price history and calculates gain/loss
        var summaries = assets.Select(a => portfolioService.ComputeAssetSummary(a, now));
        return Ok(summaries);
    }

    // Allocation breakdown: percentage of total portfolio value per asset type
    [HttpGet("allocation")]
    [ProducesResponseType(typeof(List<AssetAllocation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllocation()
    {
        var assets = await repo.GetByUserAsync(UserId);
        var allocation = portfolioService.ComputeAllocation(assets, DateTime.UtcNow);
        return Ok(allocation);
    }

    // Global unrealised gain/loss vs total cost basis across all assets
    [HttpGet("gain-loss")]
    [ProducesResponseType(typeof(PortfolioGainLoss), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGainLoss()
    {
        var assets = await repo.GetByUserAsync(UserId);
        return Ok(portfolioService.ComputeGlobalGainLoss(assets));
    }

    // Month-over-month performance for a date range
    [HttpGet("performance")]
    [ProducesResponseType(typeof(List<MonthlyPerformance>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPerformance([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (from > to) return BadRequest(new { error = "from must be before to" });
        var assets = await repo.GetByUserAsync(UserId);
        return Ok(portfolioService.ComputeMonthlyPerformance(assets, from, to));
    }

    // Returns available asset types (e.g. Stock, Crypto, RealEstate) so the UI can populate a dropdown
    [HttpGet("types")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public IActionResult GetTypes() => Ok(AssetTypes.All);

    // Returns a single asset by ID; returns 404 if not found or not owned by the user
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(string id)
    {
        var asset = await repo.GetByIdAsync(id, UserId);
        return asset is null ? NotFound() : Ok(asset);
    }

    // Creates a new asset; the purchase price is recorded as the first price history entry
    [HttpPost]
    [ProducesResponseType(typeof(Asset), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAssetRequest request)
    {
        var asset = new Asset
        {
            UserId = UserId,
            Name = request.Name,
            Type = request.Type,
            Quantity = request.Quantity,
            PurchasePrice = request.PurchasePrice,
            PurchaseDate = request.PurchaseDate,
            // Seed the price history with the purchase price so there is always at least one data point
            Price = [new PriceEntry { Value = request.PurchasePrice, Date = request.PurchaseDate }]
        };
        await repo.InsertAsync(asset);
        return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset);
    }

    // Updates the name, type, and quantity of an existing asset (does not change price history)
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateAssetRequest request)
    {
        var updated = await repo.UpdateAsync(id, UserId, request.Name, request.Type, request.Quantity);
        return updated ? NoContent() : NotFound();
    }

    // Append a new price entry — never modifies existing entries
    [HttpPost("{id}/prices")]
    public async Task<IActionResult> AddPrice(string id, [FromBody] AddPriceRequest request)
    {
        var entry = new PriceEntry { Value = request.Value, Date = request.Date };
        var updated = await repo.AddPriceAsync(id, UserId, entry);
        return updated ? NoContent() : NotFound();
    }

    // Permanently deletes an asset and all its price history
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await repo.DeleteAsync(id, UserId);
        return deleted ? NoContent() : NotFound();
    }
}

public record CreateAssetRequest(string Name, string Type, decimal Quantity, decimal PurchasePrice, DateTime PurchaseDate);
public record UpdateAssetRequest(string Name, string Type, decimal Quantity);
public record AddPriceRequest(decimal Value, DateTime Date);
