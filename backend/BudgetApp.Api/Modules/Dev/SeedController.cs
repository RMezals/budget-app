using BudgetApp.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Dev;

[ApiController]
[Route("api/dev")]
public class SeedController(ISeedService seedService) : ApiControllerBase
{
    public record SeedUserRequest(string Email);

    // Known demo emails mapped to their seed profiles
    private static readonly Dictionary<string, string> KnownProfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ausmoons@gmail.com"] = "A",
        ["test@test.com"] = "B",
        ["endercave@gmail.com"] = "C",
    };

    [HttpPost("seed/user")]
    public async Task<IActionResult> SeedUser([FromBody] SeedUserRequest request)
    {
        if (!KnownProfiles.TryGetValue(request.Email, out var profile))
            return BadRequest(new { error = $"'{request.Email}' is not a known demo email. Use one of: {string.Join(", ", KnownProfiles.Keys)}" });

        var result = profile switch
        {
            "A" => await seedService.SeedAsync(UserId),
            "B" => await seedService.SeedSecondaryAsync(UserId),
            _ => await seedService.SeedTertiaryAsync(UserId),
        };

        return Ok(new
        {
            message = $"Seed complete — Profile {profile} ({request.Email})",
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
