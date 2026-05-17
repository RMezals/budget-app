namespace BudgetApp.Api.Modules.Dev;

public record SeedResult(int Transactions, int Budgets, int Goals, int Contributions, int Assets, int Liabilities);

public interface ISeedService
{
    Task<SeedResult> SeedAsync(string userId);
}
