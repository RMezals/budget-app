using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Portfolio.Repositories;
using BudgetApp.Api.Shared;

namespace BudgetApp.Api.Modules.Portfolio.Services;

public class PortfolioService(IAssetRepository assetRepo, ILiabilityRepository liabilityRepo) : IPortfolioService
{
    public async Task<(List<Asset> Assets, List<Liability> Liabilities)> GetAllAsync(string userId)
    {
        var assets = await assetRepo.GetByUserAsync(userId);
        var liabilities = await liabilityRepo.GetByUserAsync(userId);
        return (assets, liabilities);
    }

    public NetWorthSnapshot ComputeNetWorth(List<Asset> assets, List<Liability> liabilities, DateTime asOf)
    {
        var totalAssets = assets.Sum(a => PortfolioCalculator.ResolveCurrentPrice(a.Price, asOf) * a.Quantity);
        var totalLiabilities = liabilities.Sum(l => PortfolioCalculator.ResolveCurrentAmount(l.Amount, asOf));
        return new NetWorthSnapshot(totalAssets, totalLiabilities, totalAssets - totalLiabilities);
    }

    public AssetSummary ComputeAssetSummary(Asset asset, DateTime asOf)
    {
        var currentPrice = PortfolioCalculator.ResolveCurrentPrice(asset.Price, asOf);
        var currentValue = currentPrice * asset.Quantity;
        var costBasis = asset.PurchasePrice * asset.Quantity;
        var gainLoss = currentValue - costBasis;
        var gainLossPercent = costBasis != 0 ? Math.Round(gainLoss / costBasis * 100, 2) : 0;

        return new AssetSummary(
            asset.Id,
            asset.Name,
            asset.Type,
            asset.Quantity,
            asset.PurchasePrice,
            currentPrice,
            currentValue,
            gainLoss,
            gainLossPercent);
    }

    public List<AssetAllocation> ComputeAllocation(List<Asset> assets, DateTime asOf)
    {
        var byType = assets
            .GroupBy(a => a.Type)
            .Select(g => new
            {
                Type = g.Key,
                TotalValue = g.Sum(a => PortfolioCalculator.ResolveCurrentPrice(a.Price, asOf) * a.Quantity)
            })
            .ToList();

        var portfolioTotal = byType.Sum(x => x.TotalValue);

        return byType.Select(x => new AssetAllocation(
            x.Type,
            x.TotalValue,
            portfolioTotal != 0 ? Math.Round(x.TotalValue / portfolioTotal * 100, 2) : 0
        )).ToList();
    }

    // Month-over-month performance: compares portfolio value at start vs end of each month
    public List<MonthlyPerformance> ComputeMonthlyPerformance(List<Asset> assets, DateTime from, DateTime to)
    {
        var result = new List<MonthlyPerformance>();
        var cursor = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var last = new DateTime(to.Year, to.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        while (cursor <= last)
        {
            var monthEnd = cursor.AddMonths(1).AddSeconds(-1);
            var startValue = assets.Sum(a => PortfolioCalculator.ResolveCurrentPrice(a.Price, cursor) * a.Quantity);
            var endValue = assets.Sum(a => PortfolioCalculator.ResolveCurrentPrice(a.Price, monthEnd) * a.Quantity);
            var gainLoss = endValue - startValue;
            var gainLossPercent = startValue != 0 ? Math.Round(gainLoss / startValue * 100, 2) : 0;

            result.Add(new MonthlyPerformance(cursor.ToString("yyyy-MM"), startValue, endValue, gainLoss, gainLossPercent));
            cursor = cursor.AddMonths(1);
        }

        return result;
    }

    // Global unrealised gain/loss: current portfolio value vs total cost basis
    public PortfolioGainLoss ComputeGlobalGainLoss(List<Asset> assets)
    {
        var now = DateTime.UtcNow;
        var totalInvested = assets.Sum(a => a.PurchasePrice * a.Quantity);
        var currentValue = assets.Sum(a => PortfolioCalculator.ResolveCurrentPrice(a.Price, now) * a.Quantity);
        var gainLoss = currentValue - totalInvested;
        var gainLossPercent = totalInvested != 0 ? Math.Round(gainLoss / totalInvested * 100, 2) : 0;
        return new PortfolioGainLoss(totalInvested, currentValue, gainLoss, gainLossPercent);
    }
}
