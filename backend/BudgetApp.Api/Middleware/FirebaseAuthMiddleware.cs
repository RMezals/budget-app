using FirebaseAdmin;
using FirebaseAdmin.Auth;

namespace BudgetApp.Api.Middleware;

public class FirebaseAuthMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<FirebaseAuthMiddleware> logger)
{
    // Used when Firebase is not configured in Development
    private const string DevUserId = "dev-user";

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        var hasToken = authHeader?.StartsWith("Bearer ") == true;

        // Dev bypass: only when no token is sent AND Firebase Admin is not configured.
        // If a token IS present we must verify it — never fall back to dev-user.
        if (!hasToken && env.IsDevelopment() && FirebaseApp.DefaultInstance == null)
        {
            context.Items["UserId"] = DevUserId;
            await next(context);
            return;
        }

        if (!hasToken)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (FirebaseApp.DefaultInstance == null)
        {
            logger.LogWarning("Firebase Admin SDK is not configured but a token was received.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var token = authHeader!["Bearer ".Length..];
        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
            context.Items["UserId"] = decoded.Uid;
            context.Items["Claims"] = decoded.Claims;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to verify Firebase token. Path: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
