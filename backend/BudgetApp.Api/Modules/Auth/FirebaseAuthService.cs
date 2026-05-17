using BudgetApp.Api.Modules.Auth.Models;
using FirebaseAdmin.Auth;

namespace BudgetApp.Api.Modules.Auth;

public class FirebaseAuthService : IAuthService
{
    public async Task<ProfileResponse> GetProfileAsync(string userId)
    {
        var user = await FirebaseAuth.DefaultInstance.GetUserAsync(userId);
        var currency = user.CustomClaims?.TryGetValue("currency", out var c) == true ? c?.ToString() : null;
        return new ProfileResponse(user.Uid, user.DisplayName, user.Email, currency);
    }

    public async Task UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var args = new UserRecordArgs { Uid = userId };
        if (request.DisplayName is not null) args.DisplayName = request.DisplayName;
        if (request.Email is not null) args.Email = request.Email;
        await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);

        if (request.Currency is not null)
        {
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
                userId,
                new Dictionary<string, object> { ["currency"] = request.Currency });
        }
    }

    public async Task RevokeTokensAsync(string userId) =>
        await FirebaseAuth.DefaultInstance.RevokeRefreshTokensAsync(userId);
}
