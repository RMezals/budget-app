using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Portfolio.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Portfolio;

[ApiController]
[Route("api/networth")]
public class NetWorthController(IPortfolioService portfolioService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCurrent()
    {
        var (assets, liabilities) = await portfolioService.GetAllAsync(UserId);
        var snapshot = portfolioService.ComputeNetWorth(assets, liabilities, DateTime.UtcNow);
        return Ok(snapshot);
    }

    // Reconstructs net worth series entirely in memory — no additional DB queries per date
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var (assets, liabilities) = await portfolioService.GetAllAsync(UserId);

        var series = new List<NetWorthHistoryPoint>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var s = portfolioService.ComputeNetWorth(assets, liabilities, date);
            series.Add(new NetWorthHistoryPoint(date, s.TotalAssets, s.TotalLiabilities, s.NetWorth));
        }
        return Ok(series);
    }
}
