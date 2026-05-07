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

    // Tips are generated fresh on each request and are not persisted
    [HttpPost("analyse")]
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
            return Ok(new { provider = request.Provider, tips });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }
}
