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
    [HttpGet]
    [ProducesResponseType(typeof(List<GoalProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var goals = await savingsProgressService.GetGoalProgressListAsync(UserId);
        return Ok(goals);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GoalProgressDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(string id)
    {
        var goal = await savingsProgressService.GetGoalProgressAsync(id, UserId);
        return goal is null ? NotFound() : Ok(goal);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SavingsGoal), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateGoalRequest request)
    {
        var goal = new SavingsGoal
        {
            UserId = UserId,
            Name = request.Name,
            TargetAmount = request.TargetAmount,
            Deadline = request.Deadline,
            Description = request.Description
        };
        await goalRepo.InsertAsync(goal);
        return CreatedAtAction(nameof(GetById), new { id = goal.Id }, goal);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateGoalRequest request)
    {
        var updated = await goalRepo.UpdateAsync(id, UserId, request.Name, request.TargetAmount, request.Deadline, request.Description);
        return updated ? NoContent() : NotFound();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
    {
        var updated = await goalRepo.UpdateStatusAsync(id, UserId, request.Status);
        return updated ? NoContent() : NotFound();
    }

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
