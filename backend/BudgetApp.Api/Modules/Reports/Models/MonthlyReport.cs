namespace BudgetApp.Api.Modules.Reports.Models;

public class MonthlyReport
{
    public int Year { get; set; }
    public int Month { get; set; }

    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetSavings => TotalIncome - TotalExpenses;

    public Dictionary<string, decimal> ExpensesByCategory { get; set; } = new();
    public Dictionary<string, decimal> IncomeByCategory { get; set; } = new();

    public List<GoalContributionSummary> SavingsContributions { get; set; } = new();

    public PortfolioChangeSummary PortfolioChange { get; set; } = new();
}

public class GoalContributionSummary
{
    public string GoalId { get; set; } = default!;
    public string GoalName { get; set; } = default!;
    public decimal TotalDeposited { get; set; }
    public decimal TotalWithdrawn { get; set; }
    public decimal NetContribution => TotalDeposited - TotalWithdrawn;
    public int ContributionCount { get; set; }
}

public class PortfolioChangeSummary
{
    public decimal StartValue { get; set; }
    public decimal EndValue { get; set; }
    public decimal Change => EndValue - StartValue;
    public decimal ChangePercent => StartValue != 0
        ? Math.Round((EndValue - StartValue) / StartValue * 100, 2)
        : 0;
}
