using BudgetApp.Api.Modules.Portfolio.Models;

namespace BudgetApp.Api.Modules.Portfolio.Services;

public record NetWorthSnapshot(decimal TotalAssets, decimal TotalLiabilities, decimal NetWorth);
public record NetWorthHistoryPoint(DateTime Date, decimal TotalAssets, decimal TotalLiabilities, decimal NetWorth);

public interface IPortfolioService
{
    Task<(List<Asset> Assets, List<Liability> Liabilities)> GetAllAsync(string userId);
    NetWorthSnapshot ComputeNetWorth(List<Asset> assets, List<Liability> liabilities, DateTime asOf);
}
