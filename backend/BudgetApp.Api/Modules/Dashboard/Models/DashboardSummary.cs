namespace BudgetApp.Api.Modules.Dashboard.Models;

public class DashboardSummary
{
    public decimal NetWorth { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal TotalSaved { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public List<BudgetUsage> BudgetUsage { get; set; } = [];
    public List<GoalProgress> ActiveGoals { get; set; } = [];
}

public class BudgetUsage
{
    public string Category { get; set; } = default!;
    public decimal Limit { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining => Limit - Spent;
    public decimal UsagePercent => Limit > 0 ? Math.Round(Spent / Limit * 100, 1) : 0;
}

public class GoalProgress
{
    public string GoalId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal CurrentAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal PercentReached => TargetAmount > 0 ? Math.Round(CurrentAmount / TargetAmount * 100, 1) : 0;
    public DateTime? ProjectedCompletion { get; set; }
}
