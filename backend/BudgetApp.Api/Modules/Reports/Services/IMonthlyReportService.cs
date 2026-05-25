using BudgetApp.Api.Modules.Reports.Models;

namespace BudgetApp.Api.Modules.Reports.Services;

public interface IMonthlyReportService
{
    Task<MonthlyReport> GetMonthlyReportAsync(string userId, int year, int month);
}
