using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected string UserId =>
        HttpContext.Items["UserId"] as string
        ?? throw new UnauthorizedAccessException("No authenticated user.");
}
