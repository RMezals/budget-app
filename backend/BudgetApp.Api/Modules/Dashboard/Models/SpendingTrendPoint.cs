namespace BudgetApp.Api.Modules.Dashboard.Models;

public class SpendingTrendPoint
{
    /// <summary>Month in "yyyy-MM" format.</summary>
    public string Month { get; set; } = default!;

    /// <summary>Expense totals keyed by category (positive values).</summary>
    public Dictionary<string, decimal> Expenses { get; set; } = new();
}
