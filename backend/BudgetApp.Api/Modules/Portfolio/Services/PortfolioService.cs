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
}
