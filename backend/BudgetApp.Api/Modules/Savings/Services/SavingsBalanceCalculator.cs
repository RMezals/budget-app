using BudgetApp.Api.Modules.Savings.Models;

namespace BudgetApp.Api.Modules.Savings.Services;

// Computes the running balance of a savings goal from its contribution history
public static class SavingsBalanceCalculator
{
    // Sums all contribution amounts (positive = deposit, negative = withdrawal) to get the current balance
    public static decimal CalculateCurrentBalance(List<GoalContribution> contributions) =>
        contributions.Sum(c => c.Amount);
}
