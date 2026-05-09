using BudgetApp.Api.Modules.Auth.Models;

namespace BudgetApp.Api.Modules.Auth;

public interface IAuthService
{
    Task<ProfileResponse> GetProfileAsync(string userId);
    Task UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task RevokeTokensAsync(string userId);
}
