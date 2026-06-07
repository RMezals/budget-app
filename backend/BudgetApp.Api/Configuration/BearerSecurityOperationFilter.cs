using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BudgetApp.Api.Configuration;

// Stamps every operation with the Bearer security requirement so Swagger UI shows a padlock per endpoint
public class BearerSecurityOperationFilter : IOperationFilter
{
    private static readonly OpenApiSecurityRequirement Requirement = new()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Security ??= [];
        operation.Security.Add(Requirement);
    }
}
