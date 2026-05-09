using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Dashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Dashboard;

[ApiController]
[Route("api/advisor")]
public class AdvisorController(
    IAdvisorService advisorService,
    [FromKeyedServices("claude")] IAiAdvisor claudeAdvisor,
    [FromKeyedServices("ollama")] IAiAdvisor ollamaAdvisor) : ApiControllerBase
{
    public record AnalyseRequest(string Provider = "ollama", List<string>? Goals = null);
    public record AdvisorResult(string Provider, string Tips);
    public record ErrorResult(string Error);

    // Tips are generated fresh on each request and are not persisted
    [HttpPost("analyse")]
    [ProducesResponseType(typeof(AdvisorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Analyse([FromBody] AnalyseRequest request)
    {
        var summary = await advisorService.BuildFinancialSummaryAsync(UserId);

        var userGoals = request.Goals is { Count: > 0 }
            ? string.Join(" and ", request.Goals.Select(g => GoalDescriptions.All.GetValueOrDefault(g, g)))
            : "improve their overall financial health";

        IAiAdvisor advisor = request.Provider == "claude" ? claudeAdvisor : ollamaAdvisor;

        try
        {
            var tips = await advisor.AnalyseAsync(summary, userGoals);
            return Ok(new AdvisorResult(request.Provider, tips));
        }
        catch (InvalidOperationException)
        {
            // AI service configuration or availability issues
            return StatusCode(503, new ErrorResult("AI service is currently unavailable. Please try again later."));
        }
        catch (Exception ex)
        {
            // Log unexpected errors (in real app, inject ILogger)
            Console.Error.WriteLine($"Unexpected error in advisor: {ex}");
            return StatusCode(500, new ErrorResult("An unexpected error occurred. Please try again."));
        }
    }
}
