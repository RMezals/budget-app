using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Dashboard.Models;
using BudgetApp.Api.Modules.Dashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Dashboard;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(IDashboardService dashboardService) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DashboardSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await dashboardService.GetSummaryAsync(UserId);
        return Ok(summary);
    }
}
