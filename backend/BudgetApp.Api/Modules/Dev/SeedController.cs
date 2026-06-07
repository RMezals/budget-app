using BudgetApp.Api.Controllers;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Dev;

[ApiController]
[Route("api/dev")]
public class SeedController(ISeedService seedService, IWebHostEnvironment env) : ApiControllerBase
{
    public record SeedUserRequest(string Email);

    // Known demo emails mapped to their seed profiles
    private static readonly Dictionary<string, string> KnownProfiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ausmoons@gmail.com"] = "A",
        ["test@test.com"] = "B",
        ["endercave@gmail.com"] = "C",
    };

    /// <summary>
    /// Clears and re-seeds data for a specific demo user identified by email.
    /// Resolves the Firebase UID automatically — no need to be logged in as that user.
    /// Only available in Development.
    /// </summary>
    [HttpPost("seed/user")]
    public async Task<IActionResult> SeedUser([FromBody] SeedUserRequest request)
    {
        if (!env.IsDevelopment())
            return Forbid();

        if (!KnownProfiles.TryGetValue(request.Email, out var profile))
            return BadRequest(new { error = $"'{request.Email}' is not a known demo email. Use one of: {string.Join(", ", KnownProfiles.Keys)}" });

        UserRecord userRecord;
        try
        {
            userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(request.Email);
        }
        catch (FirebaseAuthException)
        {
            return NotFound(new { error = $"No Firebase user found for '{request.Email}'. Make sure the account exists." });
        }

        var result = profile switch
        {
            "A" => await seedService.SeedAsync(userRecord.Uid),
            "B" => await seedService.SeedSecondaryAsync(userRecord.Uid),
            _ => await seedService.SeedTertiaryAsync(userRecord.Uid),
        };

        return Ok(new
        {
            message = $"Seed complete — Profile {profile} ({request.Email})",
            userId = userRecord.Uid,
            transactions = result.Transactions,
            budgets = result.Budgets,
            goals = result.Goals,
            contributions = result.Contributions,
            assets = result.Assets,
            liabilities = result.Liabilities,
        });
    }


    /// <summary>
    /// Clears and re-seeds Profile A for the authenticated user (ausmoons@gmail.com).
    /// Middle income · 3 savings goals · stocks + ETF + crypto · student loan.
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
            message = "Seed complete — Profile A",
            userId = UserId,
            transactions = result.Transactions,
            budgets = result.Budgets,
            goals = result.Goals,
            contributions = result.Contributions,
            assets = result.Assets,
            liabilities = result.Liabilities,
        });
    }

    /// <summary>
    /// Clears and re-seeds Profile B for the authenticated user (test@test.com).
    /// Higher income · 2 savings goals · ETFs only · debt-free.
    /// Only available in Development.
    /// </summary>
    [HttpPost("seed/secondary")]
    public async Task<IActionResult> SeedSecondary()
    {
        if (!env.IsDevelopment())
            return Forbid();

        var result = await seedService.SeedSecondaryAsync(UserId);
        return Ok(new
        {
            message = "Seed complete — Profile B",
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
