using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Portfolio.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Modules.Portfolio;

[ApiController]
[Route("api/liabilities")]
public class LiabilitiesController(ILiabilityRepository repo) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await repo.GetByUserAsync(UserId);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var item = await repo.GetByIdAsync(id, UserId);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLiabilityRequest request)
    {
        var liability = new Liability
        {
            UserId = UserId,
            Name = request.Name,
            Type = request.Type,
            Amount = [new AmountEntry { Value = request.InitialAmount, Date = request.Date }]
        };
        await repo.InsertAsync(liability);
        return CreatedAtAction(nameof(GetById), new { id = liability.Id }, liability);
    }

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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await repo.DeleteAsync(id, UserId);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("types")]
    public IActionResult GetTypes() => Ok(BudgetApp.Api.Modules.Portfolio.Validators.LiabilityTypes.All);
}

public record CreateLiabilityRequest(string Name, string Type, decimal InitialAmount, DateTime Date);
public record UpdateLiabilityRequest(string Name, string Type);
public record AddAmountRequest(decimal Value, DateTime Date);
