using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Portfolio.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Portfolio;

[ApiController]
[Route("api/liabilities")]
public class LiabilitiesController(ILiabilityRepository repo) : ApiControllerBase
{
    // Returns all liabilities (debts) for the authenticated user
    [HttpGet]
    [ProducesResponseType(typeof(List<Liability>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var list = await repo.GetByUserAsync(UserId);
        return Ok(list);
    }

    // Returns a single liability by ID; returns 404 if not found or not owned by the user
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Liability), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(string id)
    {
        var item = await repo.GetByIdAsync(id, UserId);
        return item is null ? NotFound() : Ok(item);
    }

    // Creates a new liability; the initial balance is stored as the first amount history entry
    [HttpPost]
    [ProducesResponseType(typeof(Liability), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateLiabilityRequest request)
    {
        var liability = new Liability
        {
            UserId = UserId,
            Name = request.Name,
            Type = request.Type,
            // Seed the amount history with the starting balance so there is always at least one data point
            Amount = [new AmountEntry { Value = request.InitialAmount, Date = request.Date }]
        };
        await repo.InsertAsync(liability);
        return CreatedAtAction(nameof(GetById), new { id = liability.Id }, liability);
    }

    // Updates the name and type of an existing liability (does not change the balance history)
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateLiabilityRequest request)
    {
        var updated = await repo.UpdateAsync(id, UserId, request.Name, request.Type);
        return updated ? NoContent() : NotFound();
    }

    // Append a new balance entry — never modifies existing entries
    [HttpPost("{id}/amounts")]
    public async Task<IActionResult> AddAmount(string id, [FromBody] AddAmountRequest request)
    {
        var entry = new AmountEntry { Value = request.Value, Date = request.Date };
        var updated = await repo.AddAmountAsync(id, UserId, entry);
        return updated ? NoContent() : NotFound();
    }

    // Permanently deletes a liability and all its balance history
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await repo.DeleteAsync(id, UserId);
        return deleted ? NoContent() : NotFound();
    }

    // Returns the list of valid liability types (e.g. Loan, Mortgage) so the UI can populate a dropdown
    [HttpGet("types")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public IActionResult GetTypes() => Ok(BudgetApp.Api.Modules.Portfolio.Validators.LiabilityTypes.All);
}

public record CreateLiabilityRequest(string Name, string Type, decimal InitialAmount, DateTime Date);
public record UpdateLiabilityRequest(string Name, string Type);
public record AddAmountRequest(decimal Value, DateTime Date);
