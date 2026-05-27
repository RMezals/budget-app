using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Auth.Models;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ApiControllerBase
{
    // Returns the profile data (display name, email, currency, etc.) for the authenticated user
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await authService.GetProfileAsync(UserId);
        return Ok(profile);
    }

    // Updates the authenticated user's profile fields (e.g. display name or preferred currency)
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        await authService.UpdateProfileAsync(UserId, request);
        return NoContent();
    }

    // Revokes all active Firebase tokens for the user, effectively logging them out on all devices
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await authService.RevokeTokensAsync(UserId);
        return NoContent();
    }
}
