using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Middleware;

// Global exception handler middleware that catches all unhandled exceptions
// and returns appropriate error responses without leaking sensitive information.
public class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
{
    // Generic message shown for unexpected errors so internal details are not exposed to the client
    private const string GenericErrorMessage = "An unexpected error occurred. Please try again later.";

    // Called by the ASP.NET Core pipeline; wraps the rest of the pipeline in a try/catch
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    // Maps the exception type to an HTTP status code and writes a ProblemDetails JSON response
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the full exception details
        logger.LogError(exception, "Unhandled exception occurred. Path: {Path}, Method: {Method}, UserId: {UserId}",
            context.Request.Path,
            context.Request.Method,
            context.Items["UserId"]);

        // Map well-known exception types to appropriate HTTP status codes; everything else is 500
        var (statusCode, message) = exception switch
        {
            InvalidOperationException => (HttpStatusCode.ServiceUnavailable, exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access."),
            KeyNotFoundException => (HttpStatusCode.NotFound, "The requested resource was not found."),
            _ => (HttpStatusCode.InternalServerError, GenericErrorMessage)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        // Build a standard ProblemDetails object (RFC 7807) for a consistent error response shape
        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = GetTitleForStatusCode(statusCode),
            Detail = message,
            Instance = context.Request.Path
        };

        // In development, include more details for debugging
        if (env.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
        {
            // Only expose extra detail in development so production clients don't see stack information
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["exceptionMessage"] = exception.Message;
        }

        // Serialise to camelCase JSON to match the rest of the API's response format
        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    // Converts an HTTP status code to a short human-readable title string
    private static string GetTitleForStatusCode(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => "Bad Request",
        HttpStatusCode.Unauthorized => "Unauthorized",
        HttpStatusCode.NotFound => "Not Found",
        HttpStatusCode.ServiceUnavailable => "Service Unavailable",
        HttpStatusCode.InternalServerError => "Internal Server Error",
        _ => "Error"
    };
}
