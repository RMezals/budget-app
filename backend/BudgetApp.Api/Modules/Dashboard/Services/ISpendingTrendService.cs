using BudgetApp.Api.Modules.Dashboard.Models;

namespace BudgetApp.Api.Modules.Dashboard.Services;

public interface ISpendingTrendService
{
    Task<List<SpendingTrendPoint>> GetSpendingTrendAsync(string userId, int months = 12);
}
