namespace BudgetApp.Api.Modules.Dashboard.Services;

public interface IAdvisorService
{
    Task<string> BuildFinancialSummaryAsync(string userId);
}
