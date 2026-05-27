using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Dashboard.Models;
using BudgetApp.Api.Modules.Dashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Dashboard;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(
    IDashboardService dashboardService,
    ISpendingTrendService spendingTrendService) : ApiControllerBase
{
    // Returns the main dashboard summary: income, expenses, savings progress, and net worth for the current month
    [HttpGet]
    [ProducesResponseType(typeof(DashboardSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await dashboardService.GetSummaryAsync(UserId);
        return Ok(summary);
    }

    // Returns monthly spending totals for the past N months (default 12) used to render the spending trend chart
    [HttpGet("spending-trend")]
    [ProducesResponseType(typeof(List<SpendingTrendPoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpendingTrend([FromQuery] int months = 12)
    {
        var trend = await spendingTrendService.GetSpendingTrendAsync(UserId, months);
        return Ok(trend);
    }
}
