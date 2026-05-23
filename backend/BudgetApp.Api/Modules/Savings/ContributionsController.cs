using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Savings.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Savings;

[ApiController]
[Route("api/goals/{goalId}/contributions")]
public class ContributionsController(
    IGoalContributionRepository contributionRepo,
    ISavingsService savingsService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(string goalId)
    {
        var list = await contributionRepo.GetByGoalAsync(goalId, UserId);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Add(string goalId, [FromBody] AddContributionRequest request)
    {
        try
        {
            var contribution = await savingsService.AddContributionAsync(
                goalId, UserId, request.Amount, request.Date, request.Reason, request.Description ?? request.Note);
            return CreatedAtAction(nameof(GetAll), new { goalId }, contribution);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string goalId, string id)
    {
        var deleted = await contributionRepo.DeleteAsync(id, UserId);
        if (!deleted) return NotFound();

        await savingsService.RecalculateBalanceAsync(goalId, UserId);
        return NoContent();
    }
}

public record AddContributionRequest(decimal Amount, DateTime Date, string? Reason, string? Description = null, string? Note = null);
