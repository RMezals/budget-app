using BudgetApp.Api.Modules.Dashboard.Models;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(string userId);
}
