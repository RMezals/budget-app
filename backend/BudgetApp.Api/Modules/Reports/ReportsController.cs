using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Reports.Models;
using BudgetApp.Api.Modules.Reports.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Reports;

[ApiController]
[Route("api/reports")]
public class ReportsController(IMonthlyReportService reportService) : ApiControllerBase
{
    [HttpGet("monthly")]
    [ProducesResponseType(typeof(MonthlyReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlyReport(
        [FromQuery] int year,
        [FromQuery] int month)
    {
        if (year < 2000 || year > 2100)
            return BadRequest(new { error = "Year must be between 2000 and 2100." });

        if (month < 1 || month > 12)
            return BadRequest(new { error = "Month must be between 1 and 12." });

        var report = await reportService.GetMonthlyReportAsync(UserId, year, month);
        return Ok(report);
    }
}
