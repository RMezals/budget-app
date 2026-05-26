using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Services;

public static class SavingsBalanceCalculator
{
    public static decimal CalculateCurrentBalance(List<GoalContribution> contributions) =>
        contributions.Sum(c => c.Amount);
}
