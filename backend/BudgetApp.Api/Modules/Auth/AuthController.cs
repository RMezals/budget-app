using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Auth.Models;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ApiControllerBase
{
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await authService.GetProfileAsync(UserId);
        return Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        await authService.UpdateProfileAsync(UserId, request);
        return NoContent();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await authService.RevokeTokensAsync(UserId);
        return NoContent();
    }
}
