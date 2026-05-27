using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Portfolio.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Portfolio;

[ApiController]
[Route("api/networth")]
public class NetWorthController(IPortfolioService portfolioService) : ApiControllerBase
{
    // Returns the user's current net worth (total assets minus total liabilities) as of now
    [HttpGet]
    [ProducesResponseType(typeof(NetWorthSnapshot), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrent()
    {
        var (assets, liabilities) = await portfolioService.GetAllAsync(UserId);
        var snapshot = portfolioService.ComputeNetWorth(assets, liabilities, DateTime.UtcNow);
        return Ok(snapshot);
    }

    // Reconstructs net worth series entirely in memory — no additional DB queries per date
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<NetWorthHistoryPoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        // Cap the range to 5 years (1826 days) to prevent unbounded in-memory iteration
        if ((to.Date - from.Date).TotalDays > 1826)
            return BadRequest(new { error = "Date range cannot exceed 5 years." });

        if (from > to)
            return BadRequest(new { error = "Start date must be before end date." });

        var (assets, liabilities) = await portfolioService.GetAllAsync(UserId);

        // Walk through each calendar day in the range and compute a net worth snapshot for it
        var series = new List<NetWorthHistoryPoint>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var s = portfolioService.ComputeNetWorth(assets, liabilities, date);
            series.Add(new NetWorthHistoryPoint(date, s.TotalAssets, s.TotalLiabilities, s.NetWorth));
        }
        return Ok(series);
    }
}
