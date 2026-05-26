using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Dashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Dashboard;

[ApiController]
[Route("api/advisor")]
public class AdvisorController(
    IAdvisorService advisorService,
    IAiAdvisorFactory advisorFactory,
    ILogger<AdvisorController> logger) : ApiControllerBase
{
    // Tips are generated fresh on each request and are not persisted
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

        var summary = await advisorService.BuildFinancialSummaryAsync(UserId);

        var userGoals = request.Goals is { Count: > 0 }
            ? string.Join(" and ", request.Goals.Select(g => GoalDescriptions.All.GetValueOrDefault(g, g)))
            : "improve their overall financial health";

        try
        {
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
