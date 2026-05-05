using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Savings.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Savings;

[ApiController]
[Route("api/goals")]
public class GoalsController(IMongoDatabase db) : ApiControllerBase
{
    private readonly IMongoCollection<SavingsGoal>     _goals = db.GetCollection<SavingsGoal>("savings_goals");
    private readonly IMongoCollection<GoalContribution> _contributions = db.GetCollection<GoalContribution>("goal_contributions");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var goals = await _goals.Find(g => g.UserId == UserId).ToListAsync();
        return Ok(goals);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var goal = await _goals.Find(g => g.Id == id && g.UserId == UserId).FirstOrDefaultAsync();
        return goal is null ? NotFound() : Ok(goal);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGoalRequest request)
    {
        var goal = new SavingsGoal
        {
            UserId        = UserId,
            Name          = request.Name,
            TargetAmount  = request.TargetAmount,
            Deadline      = request.Deadline,
            Description   = request.Description
        };
        await _goals.InsertOneAsync(goal);
        return CreatedAtAction(nameof(GetById), new { id = goal.Id }, goal);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateGoalRequest request)
    {
        var update = Builders<SavingsGoal>.Update
            .Set(g => g.Name,         request.Name)
            .Set(g => g.TargetAmount, request.TargetAmount)
            .Set(g => g.Deadline,     request.Deadline)
            .Set(g => g.Description,  request.Description);

        var result = await _goals.UpdateOneAsync(g => g.Id == id && g.UserId == UserId, update);
        return result.MatchedCount == 0 ? NotFound() : NoContent();
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest request)
    {
        var update = Builders<SavingsGoal>.Update.Set(g => g.Status, request.Status);
        var result = await _goals.UpdateOneAsync(g => g.Id == id && g.UserId == UserId, update);
        return result.MatchedCount == 0 ? NotFound() : NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _goals.DeleteOneAsync(g => g.Id == id && g.UserId == UserId);
        return result.DeletedCount == 0 ? NotFound() : NoContent();
    }

    // Projected completion date based on last 30 days average daily contribution rate
    [HttpGet("{id}/projection")]
    public async Task<IActionResult> GetProjection(string id)
    {
        var goal = await _goals.Find(g => g.Id == id && g.UserId == UserId).FirstOrDefaultAsync();
        if (goal is null) return NotFound();

        var since = DateTime.UtcNow.AddDays(-30);
        var recent = await _contributions
            .Find(c => c.GoalId == id && c.UserId == UserId && c.Date >= since)
            .ToListAsync();

        var netAdded = recent.Sum(c => c.Amount);
        var dailyRate = netAdded / 30m;

        if (dailyRate <= 0)
            return Ok(new { projectedCompletion = (DateTime?)null, reason = "Insufficient contribution rate" });

        var remaining = goal.TargetAmount - goal.CurrentAmount;
        var days = (int)Math.Ceiling(remaining / dailyRate);
        return Ok(new { projectedCompletion = DateTime.UtcNow.AddDays(days) });
    }
}

public record CreateGoalRequest(string Name, decimal TargetAmount, DateTime Deadline, string? Description);
public record UpdateGoalRequest(string Name, decimal TargetAmount, DateTime Deadline, string? Description);
public record UpdateStatusRequest(GoalStatus Status);
