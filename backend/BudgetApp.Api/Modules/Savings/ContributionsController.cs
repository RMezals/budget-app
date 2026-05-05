using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Savings.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Savings;

[ApiController]
[Route("api/goals/{goalId}/contributions")]
public class ContributionsController(IMongoDatabase db) : ApiControllerBase
{
    private readonly IMongoCollection<SavingsGoal>      _goals         = db.GetCollection<SavingsGoal>("savings_goals");
    private readonly IMongoCollection<GoalContribution> _contributions = db.GetCollection<GoalContribution>("goal_contributions");

    [HttpGet]
    public async Task<IActionResult> GetAll(string goalId)
    {
        var list = await _contributions
            .Find(c => c.GoalId == goalId && c.UserId == UserId)
            .SortByDescending(c => c.Date)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Add(string goalId, [FromBody] AddContributionRequest request)
    {
        var goal = await _goals.Find(g => g.Id == goalId && g.UserId == UserId).FirstOrDefaultAsync();
        if (goal is null) return NotFound();

        if (request.Amount < 0 && Math.Abs(request.Amount) > goal.CurrentAmount)
            return BadRequest(new { error = "Withdrawal exceeds current saved amount." });

        var newBalance = goal.CurrentAmount + request.Amount;
        var contribution = new GoalContribution
        {
            GoalId       = goalId,
            UserId       = UserId,
            Amount       = request.Amount,
            Date         = request.Date,
            Description  = request.Description,
            BalanceAfter = newBalance
        };
        await _contributions.InsertOneAsync(contribution);

        // Atomically update running total and auto-complete if target reached
        var goalUpdate = Builders<SavingsGoal>.Update.Set(g => g.CurrentAmount, newBalance);
        if (newBalance >= goal.TargetAmount)
            goalUpdate = goalUpdate.Set(g => g.Status, GoalStatus.Completed);
        await _goals.UpdateOneAsync(g => g.Id == goalId, goalUpdate);

        return CreatedAtAction(nameof(GetAll), new { goalId }, contribution);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string goalId, string id)
    {
        var result = await _contributions.DeleteOneAsync(c => c.Id == id && c.UserId == UserId);
        if (result.DeletedCount == 0) return NotFound();

        // Recalculate goal balance by re-summing remaining contributions
        var remaining = await _contributions
            .Find(c => c.GoalId == goalId && c.UserId == UserId)
            .ToListAsync();
        var newBalance = remaining.Sum(c => c.Amount);
        var update = Builders<SavingsGoal>.Update.Set(g => g.CurrentAmount, newBalance);
        await _goals.UpdateOneAsync(g => g.Id == goalId && g.UserId == UserId, update);

        return NoContent();
    }
}

public record AddContributionRequest(decimal Amount, DateTime Date, string? Description);
