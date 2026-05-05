using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Portfolio.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Portfolio;

[ApiController]
[Route("api/networth")]
public class NetWorthController(IMongoDatabase db) : ApiControllerBase
{
    private readonly IMongoCollection<Asset>     _assets      = db.GetCollection<Asset>("assets");
    private readonly IMongoCollection<Liability> _liabilities = db.GetCollection<Liability>("liabilities");

    [HttpGet]
    public async Task<IActionResult> GetCurrent()
    {
        var (assets, liabilities) = await FetchAll();
        var today = DateTime.UtcNow;
        return Ok(Compute(assets, liabilities, today));
    }

    // Reconstructs net worth series entirely in memory — no additional DB queries per date
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var (assets, liabilities) = await FetchAll();

        var series = new List<object>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var point = Compute(assets, liabilities, date);
            series.Add(new { date, point.NetWorth, point.TotalAssets, point.TotalLiabilities });
        }
        return Ok(series);
    }

    private async Task<(List<Asset>, List<Liability>)> FetchAll()
    {
        var assets      = await _assets.Find(a => a.UserId == UserId).ToListAsync();
        var liabilities = await _liabilities.Find(l => l.UserId == UserId).ToListAsync();
        return (assets, liabilities);
    }

    private static NetWorthSnapshot Compute(List<Asset> assets, List<Liability> liabilities, DateTime date)
    {
        var totalAssets      = assets.Sum(a => ResolvePrice(a.Price, date) * a.Quantity);
        var totalLiabilities = liabilities.Sum(l => ResolveAmount(l.Amount, date));
        return new NetWorthSnapshot(totalAssets, totalLiabilities, totalAssets - totalLiabilities);
    }

    private static decimal ResolvePrice(List<PriceEntry> history, DateTime date) =>
        history.Where(e => e.Date <= date).MaxBy(e => e.Date)?.Value ?? 0;

    private static decimal ResolveAmount(List<AmountEntry> history, DateTime date) =>
        history.Where(e => e.Date <= date).MaxBy(e => e.Date)?.Value ?? 0;
}

public record NetWorthSnapshot(decimal TotalAssets, decimal TotalLiabilities, decimal NetWorth);
