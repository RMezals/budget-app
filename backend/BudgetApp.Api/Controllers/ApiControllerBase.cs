using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Controllers;

// Base class for all API controllers; provides the authenticated user's ID extracted from the Firebase middleware
public abstract class ApiControllerBase : ControllerBase
{
    // Reads the user ID that FirebaseAuthMiddleware placed in HttpContext.Items after token verification
    // Throws UnauthorizedAccessException if the middleware did not set the value (should never happen in normal flow)
    protected string UserId =>
        HttpContext.Items["UserId"] as string
        ?? throw new UnauthorizedAccessException("No authenticated user.");
}
