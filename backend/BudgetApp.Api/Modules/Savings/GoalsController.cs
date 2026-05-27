using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Savings.Repositories;
using BudgetApp.Api.Modules.Savings.Services;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Savings;

[ApiController]
[Route("api/goals")]
public class GoalsController(
    ISavingsGoalRepository goalRepo,
    ISavingsProgressService savingsProgressService,
    ISavingsService savingsService) : ApiControllerBase
{
    // Returns all savings goals for the authenticated user, enriched with current balance and projected completion
    [HttpGet]
    [ProducesResponseType(typeof(List<GoalProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var goals = await savingsProgressService.GetGoalProgressListAsync(UserId);
        return Ok(goals);
    }

    // Returns a single goal by ID with its current progress; returns 404 if not found
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GoalProgressDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(string id)
    {
        var goal = await savingsProgressService.GetGoalProgressAsync(id, UserId);
        return goal is null ? NotFound() : Ok(goal);
    }

    // Creates a new savings goal for the authenticated user and returns the created record
    [HttpPost]
    [ProducesResponseType(typeof(SavingsGoal), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateGoalRequest request)
    {
        var goal = new SavingsGoal
        {
            UserId = UserId,
            // Trim whitespace from the name so storage is consistent
            Name = request.Name.Trim(),
            TargetAmount = request.TargetAmount,
            Deadline = request.Deadline,
            // Store null rather than an empty string for optional description
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };
        await goalRepo.InsertAsync(goal);
        return CreatedAtAction(nameof(GetById), new { id = goal.Id }, goal);
    }

    // Updates the name, target amount, deadline, and description of an existing goal
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateGoalRequest request)
    {
        var updated = await goalRepo.UpdateAsync(id, UserId, request.Name, request.TargetAmount, request.Deadline, request.Description);
        return updated ? NoContent() : NotFound();
    }

    // Updates only the status of a goal (e.g. pausing or resuming it)
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
    {
        var updated = await goalRepo.UpdateStatusAsync(id, UserId, request.Status);
        return updated ? NoContent() : NotFound();
    }

    // Withdraws any remaining balance and permanently marks the goal as Abandoned
    [HttpPost("{id}/abandon")]
    public async Task<IActionResult> Abandon(string id, [FromBody] AbandonGoalRequest request)
    {
        try
        {
            await savingsService.AbandonGoalAsync(id, UserId, request.Date, request.Reason, request.Description);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }


    // Permanently deletes a savings goal and all its data; returns 404 if not found
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await goalRepo.DeleteAsync(id, UserId);
        return deleted ? NoContent() : NotFound();
    }

    // Projected completion date based on last 30 days average daily contribution rate
    [HttpGet("{id}/projection")]
    [ProducesResponseType(typeof(ProjectionResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjection(string id)
    {
        try
        {
            var result = await savingsProgressService.GetProjectionAsync(id, UserId);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public record CreateGoalRequest(string Name, decimal TargetAmount, DateTime Deadline, string? Description);
public record UpdateGoalRequest(string Name, decimal TargetAmount, DateTime Deadline, string? Description);
public record UpdateStatusRequest(GoalStatus Status);
public record AbandonGoalRequest(DateTime Date, string? Reason, string? Description = null);
