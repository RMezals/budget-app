using FirebaseAdmin;
using FirebaseAdmin.Auth;

namespace BudgetApp.Api.Middleware;

public class FirebaseAuthMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<FirebaseAuthMiddleware> logger)
{
    // Used when Firebase is not configured in Development
    private const string DevUserId = "dev-user";

    public async Task InvokeAsync(HttpContext context)
    {
        // Firebase not configured + Development = bypass auth with a fixed dev user
        if (env.IsDevelopment() && FirebaseApp.DefaultInstance == null)
        {
            context.Items["UserId"] = DevUserId;
            await next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            var token = authHeader["Bearer ".Length..];
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
        }

        await next(context);
    }
}
