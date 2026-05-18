using BudgetApp.Api.Modules.Portfolio.Models;

namespace BudgetApp.Api.Modules.Portfolio.Services;

public record NetWorthSnapshot(decimal TotalAssets, decimal TotalLiabilities, decimal NetWorth);
public record NetWorthHistoryPoint(DateTime Date, decimal TotalAssets, decimal TotalLiabilities, decimal NetWorth);
public record AssetSummary(string Id, string Name, string Type, decimal Quantity, decimal PurchasePrice, decimal CurrentPrice, decimal CurrentValue, decimal UnrealisedGainLoss, decimal UnrealisedGainLossPercent);
public record AssetAllocation(string Type, decimal TotalValue, decimal AllocationPercent);
public record MonthlyPerformance(string Month, decimal StartValue, decimal EndValue, decimal GainLoss, decimal GainLossPercent);
public record PortfolioGainLoss(decimal TotalInvested, decimal CurrentValue, decimal TotalGainLoss, decimal TotalGainLossPercent);

public interface IPortfolioService
{
    Task<(List<Asset> Assets, List<Liability> Liabilities)> GetAllAsync(string userId);
    NetWorthSnapshot ComputeNetWorth(List<Asset> assets, List<Liability> liabilities, DateTime asOf);
    AssetSummary ComputeAssetSummary(Asset asset, DateTime asOf);
    List<AssetAllocation> ComputeAllocation(List<Asset> assets, DateTime asOf);
    List<MonthlyPerformance> ComputeMonthlyPerformance(List<Asset> assets, DateTime from, DateTime to);
    PortfolioGainLoss ComputeGlobalGainLoss(List<Asset> assets);
}
