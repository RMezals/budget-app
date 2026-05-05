using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Auth.Models;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await FirebaseAuth.DefaultInstance.GetUserAsync(UserId);
        var currency = user.CustomClaims?.TryGetValue("currency", out var c) == true ? c?.ToString() : null;
        return Ok(new ProfileResponse(user.Uid, user.DisplayName, user.Email, currency));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var args = new UserRecordArgs { Uid = UserId };
        if (request.DisplayName is not null) args.DisplayName = request.DisplayName;
        if (request.Email is not null) args.Email = request.Email;
        await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);

        if (request.Currency is not null)
        {
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
                UserId,
                new Dictionary<string, object> { ["currency"] = request.Currency });
        }

        return NoContent();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await FirebaseAuth.DefaultInstance.RevokeRefreshTokensAsync(UserId);
        return NoContent();
    }
}
