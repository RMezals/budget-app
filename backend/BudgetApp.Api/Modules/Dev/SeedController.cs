using BudgetApp.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Dev;

[ApiController]
[Route("api/dev")]
public class SeedController(ISeedService seedService, IWebHostEnvironment env) : ApiControllerBase
{
    /// <summary>
    /// Clears and re-seeds realistic sample data for the authenticated user.
    /// Only available in Development.
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        if (!env.IsDevelopment())
            return Forbid();

        var result = await seedService.SeedAsync(UserId);
        return Ok(new
        {
            message = "Seed complete",
            userId = UserId,
            transactions = result.Transactions,
            budgets = result.Budgets,
            goals = result.Goals,
            contributions = result.Contributions,
            assets = result.Assets,
            liabilities = result.Liabilities,
        });
    }
}
