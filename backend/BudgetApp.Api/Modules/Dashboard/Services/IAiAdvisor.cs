namespace BudgetApp.Api.Modules.Dashboard.Services;

public interface IAiAdvisor
{
    Task<string> AnalyseAsync(string financialSummary, string userGoals);
}
