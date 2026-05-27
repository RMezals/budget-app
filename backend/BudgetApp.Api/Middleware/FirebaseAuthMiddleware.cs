using FirebaseAdmin;
using FirebaseAdmin.Auth;

namespace BudgetApp.Api.Middleware;

// ASP.NET Core middleware that validates Firebase ID tokens on every incoming request
public class FirebaseAuthMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<FirebaseAuthMiddleware> logger)
{
    // Used when Firebase is not configured in Development
    private const string DevUserId = "dev-user";

    // Called automatically by the ASP.NET Core pipeline for each HTTP request
    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        // A valid Bearer token must start with "Bearer " followed by the token string
        var hasToken = authHeader?.StartsWith("Bearer ") == true;

        // Dev bypass: only when no token is sent AND Firebase Admin is not configured.
        // If a token IS present we must verify it — never fall back to dev-user.
        if (!hasToken && env.IsDevelopment() && FirebaseApp.DefaultInstance == null)
        {
            // Inject a predictable user ID so controllers can run without real auth in local development
            context.Items["UserId"] = DevUserId;
            await next(context);
            return;
        }

        // No token provided outside the dev bypass — reject the request immediately
        if (!hasToken)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // If a token was sent but Firebase is not initialised, something is misconfigured on the server
        if (FirebaseApp.DefaultInstance == null)
        {
            logger.LogWarning("Firebase Admin SDK is not configured but a token was received.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Strip the "Bearer " prefix (7 characters) to obtain the raw token string
        var token = authHeader!["Bearer ".Length..];
        try
        {
            // Verify the token signature and expiry with the Firebase Admin SDK
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
            // Store the verified user ID so controllers can access it via HttpContext.Items
            context.Items["UserId"] = decoded.Uid;
            context.Items["Claims"] = decoded.Claims;
        }
        catch (Exception ex)
        {
            // Any verification failure (expired, tampered, wrong audience) results in 401
            logger.LogWarning(ex, "Failed to verify Firebase token. Path: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
