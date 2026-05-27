using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Savings.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Savings;

// Manages deposits and withdrawals for a specific savings goal (nested under /api/goals/{goalId}/contributions)
[ApiController]
[Route("api/goals/{goalId}/contributions")]
public class ContributionsController(
    IGoalContributionRepository contributionRepo,
    ISavingsService savingsService) : ApiControllerBase
{
    // Returns all contribution records for the specified goal
    [HttpGet]
    [ProducesResponseType(typeof(List<GoalContribution>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(string goalId)
    {
        var list = await contributionRepo.GetByGoalAsync(goalId, UserId);
        return Ok(list);
    }

    // Records a new deposit or withdrawal for the goal; negative amounts are treated as withdrawals
    [HttpPost]
    [ProducesResponseType(typeof(GoalContribution), StatusCodes.Status201Created)]
    public async Task<IActionResult> Add(string goalId, [FromBody] AddContributionRequest request)
    {
        try
        {
            // Description and Note are aliases — Description takes priority if both are supplied
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
            // Business rule violations (e.g. over-withdrawal, paused goal) are reported as 400 Bad Request
            return BadRequest(new { error = ex.Message });
        }
    }

    // Updates the amount and optional reason for an existing contribution and recalculates the goal balance
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(GoalContribution), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string goalId, string id, [FromBody] UpdateContributionRequest request)
    {
        try
        {
            var contribution = await savingsService.UpdateContributionAsync(
                goalId,
                id,
                UserId,
                request.Amount,
                request.Reason);

            return Ok(contribution);
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

    // Deletes a contribution and triggers a full balance recalculation so the goal stays accurate
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string goalId, string id)
    {
        var deleted = await contributionRepo.DeleteAsync(id, goalId, UserId);
        if (!deleted) return NotFound();

        // Recalculate because the deleted contribution may have affected the current balance and status
        await savingsService.RecalculateBalanceAsync(goalId, UserId);
        return NoContent();
    }
}

public record AddContributionRequest(decimal Amount, DateTime Date, string? Reason, string? Description = null, string? Note = null);
public record UpdateContributionRequest(decimal Amount, string? Reason);
