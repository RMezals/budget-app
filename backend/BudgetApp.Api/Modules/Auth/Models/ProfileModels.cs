namespace BudgetApp.Api.Modules.Auth.Models;

public record ProfileResponse(string Uid, string? DisplayName, string? Email, string? Currency);
public record UpdateProfileRequest(string? DisplayName, string? Email, string? Currency);
