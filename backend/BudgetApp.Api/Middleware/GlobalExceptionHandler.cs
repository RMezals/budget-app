using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Middleware;

/// <summary>
/// Global exception handler middleware that catches all unhandled exceptions
/// and returns appropriate error responses without leaking sensitive information.
/// </summary>
public class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
{
    private const string GenericErrorMessage = "An unexpected error occurred. Please try again later.";

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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the full exception details
        logger.LogError(exception, "Unhandled exception occurred. Path: {Path}, Method: {Method}, UserId: {UserId}",
            context.Request.Path,
            context.Request.Method,
            context.Items["UserId"]);

        // Determine status code and message based on exception type
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
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["exceptionMessage"] = exception.Message;
        }

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

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
