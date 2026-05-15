namespace BudgetApp.Api.Modules.Savings.Models;

public class GoalProgressDto
{
    public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal TargetAmount { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal PercentReached => TargetAmount > 0 ? Math.Round(CurrentBalance / TargetAmount * 100, 1) : 0;
    public decimal AmountRemaining => Math.Max(TargetAmount - CurrentBalance, 0);
    public DateTime? ProjectedCompletion { get; set; }
    public GoalStatus Status { get; set; }
    public DateTime Deadline { get; set; }
    public string? Description { get; set; }
}
