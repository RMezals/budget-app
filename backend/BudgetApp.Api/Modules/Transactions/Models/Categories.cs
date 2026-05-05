namespace BudgetApp.Api.Modules.Transactions.Models;

public static class Categories
{
    public static readonly IReadOnlyList<string> Expense =
    [
        "Housing", "Utilities", "Food", "Transport", "Healthcare",
        "Entertainment", "Clothing", "Education", "Insurance", "Other"
    ];

    public static readonly IReadOnlyList<string> Income =
    [
        "Salary", "Freelance", "Investment", "Rental", "Gift", "Other"
    ];

    public static bool IsValid(string category) =>
        Expense.Contains(category) || Income.Contains(category);
}
