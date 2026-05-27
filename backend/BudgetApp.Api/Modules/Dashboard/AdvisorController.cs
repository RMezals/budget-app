using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Dashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Dashboard;

// Exposes an AI-powered financial advisor endpoint that analyses the user's finances and returns actionable tips
[ApiController]
[Route("api/advisor")]
public class AdvisorController(
    IAdvisorService advisorService,
    IAiAdvisorFactory advisorFactory,
    ILogger<AdvisorController> logger) : ApiControllerBase
{
    // Tips are generated fresh on each request and are not persisted
    // Accepts the user's chosen AI provider, optional financial goals, and an optional API key for that provider
    [HttpPost("analyse")]
    [ProducesResponseType(typeof(AdvisorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Analyse([FromBody] AnalyseRequest request)
    {
        logger.LogInformation(
            "Advisor request received. Provider: {Provider}, Goals: {Goals}, ApiKeyProvided: {ApiKeyProvided}, UserId: {UserId}",
            request.Provider,
            string.Join(", ", request.Goals ?? new List<string>()),
            !string.IsNullOrEmpty(request.ApiKey),
            UserId);

        // Collect the user's financial data to include as context in the AI prompt
        var summary = await advisorService.BuildFinancialSummaryAsync(UserId);

        // Convert goal keys to human-readable descriptions; fall back to a generic goal if none are specified
        var userGoals = request.Goals is { Count: > 0 }
            ? string.Join(" and ", request.Goals.Select(g => GoalDescriptions.All.GetValueOrDefault(g, g)))
            : "improve their overall financial health";

        try
        {
            // Use the factory to get the correct AI advisor implementation for the requested provider
            var advisor = advisorFactory.GetAdvisor(request.Provider);
            var tips = await advisor.AnalyseAsync(summary, userGoals, request.ApiKey);
            return Ok(new AdvisorResult(request.Provider, tips));
        }
        catch (InvalidOperationException ex)
        {
            // AI service configuration or availability issues - return 503
            logger.LogWarning(ex, "AI service unavailable. Provider: {Provider}, UserId: {UserId}", request.Provider, UserId);
            return StatusCode(503, new ErrorResult(ex.Message));
        }
    }
}
