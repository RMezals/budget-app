using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace BudgetApp.Api.Modules.Dev;

[ApiController]
[Route("api/dev")]
public class TokenController(IHttpClientFactory httpClientFactory, IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    public record LoginRequest(string Email, string Password);

    /// <summary>
    /// Dev only — exchanges email + password for a Firebase ID token usable in the Authorize dialog.
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> GetToken([FromBody] LoginRequest request)
    {
        if (!env.IsDevelopment())
            return Forbid();

        var apiKey = config["Firebase:WebApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return StatusCode(500, new { error = "Firebase:WebApiKey is not configured in appsettings.Development.json." });

        var client = httpClientFactory.CreateClient();
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

        var response = await client.PostAsJsonAsync(url, new
        {
            email = request.Email,
            password = request.Password,
            returnSecureToken = true,
        });

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return Unauthorized(new { error = "Firebase rejected the credentials.", detail = err });
        }

        var body = await response.Content.ReadFromJsonAsync<FirebaseSignInResponse>();
        return Ok(new { idToken = body!.IdToken, expiresIn = body.ExpiresIn });
    }

    private sealed class FirebaseSignInResponse
    {
        [JsonPropertyName("idToken")] public string IdToken { get; set; } = "";
        [JsonPropertyName("expiresIn")] public string ExpiresIn { get; set; } = "";
    }
}
