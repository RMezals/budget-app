using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Portfolio.Repositories;
using BudgetApp.Api.Modules.Portfolio.Services;
using Moq;
using Xunit;

namespace BudgetApp.Tests.Portfolio;

public class PortfolioServiceTests
{
    private readonly Mock<IAssetRepository> _assetMock = new();
    private readonly Mock<ILiabilityRepository> _liabilityMock = new();

    private PortfolioService CreateSut() => new(_assetMock.Object, _liabilityMock.Object);

    private static readonly DateTime AsOf = new(2026, 5, 1);

    private static Asset MakeAsset(decimal quantity, decimal purchasePrice, decimal currentPrice) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "u1",
            Name = "Asset",
            Type = "Stock",
            Quantity = quantity,
            PurchasePrice = purchasePrice,
            Price = [new PriceEntry { Value = currentPrice, Date = AsOf.AddDays(-1) }]
        };

    private static Liability MakeLiability(decimal amount) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "u1",
            Name = "Debt",
            Type = "Loan",
            Amount = [new AmountEntry { Value = amount, Date = AsOf.AddDays(-1) }]
        };

    // ── ComputeNetWorth ───────────────────────────────────────────────────

    [Fact]
    public void ComputeNetWorth_NoAssetsOrLiabilities_ReturnsAllZeros()
    {
        var result = CreateSut().ComputeNetWorth([], [], AsOf);

        Assert.Equal(0m, result.TotalAssets);
        Assert.Equal(0m, result.TotalLiabilities);
        Assert.Equal(0m, result.NetWorth);
    }

    [Fact]
    public void ComputeNetWorth_SumsAllAssetsAndLiabilities()
    {
        var assets = new List<Asset>
        {
            MakeAsset(quantity: 10, purchasePrice: 80m, currentPrice: 100m),
            MakeAsset(quantity: 5,  purchasePrice: 200m, currentPrice: 300m)
        };
        var liabilities = new List<Liability>
        {
            MakeLiability(500m),
            MakeLiability(250m)
        };

        var result = CreateSut().ComputeNetWorth(assets, liabilities, AsOf);

        Assert.Equal(2500m, result.TotalAssets);     // 10*100 + 5*300
        Assert.Equal(750m, result.TotalLiabilities); // 500 + 250
        Assert.Equal(1750m, result.NetWorth);
    }

    [Fact]
    public void ComputeNetWorth_LiabilitiesExceedAssets_NetWorthIsNegative()
    {
        var assets = new List<Asset> { MakeAsset(quantity: 1, purchasePrice: 100m, currentPrice: 100m) };
        var liabilities = new List<Liability> { MakeLiability(5000m) };

        var result = CreateSut().ComputeNetWorth(assets, liabilities, AsOf);

        Assert.Equal(-4900m, result.NetWorth);
    }

    // ── ComputeAssetSummary ───────────────────────────────────────────────

    [Fact]
    public void ComputeAssetSummary_CalculatesGainLossAndPercent()
    {
        var asset = MakeAsset(quantity: 10, purchasePrice: 80m, currentPrice: 120m);

        var result = CreateSut().ComputeAssetSummary(asset, AsOf);

        Assert.Equal(1200m, result.CurrentValue);     // 10 * 120
        Assert.Equal(400m,  result.UnrealisedGainLoss);     // 1200 - 800
        Assert.Equal(50m,   result.UnrealisedGainLossPercent); // 400/800 * 100
    }

    [Fact]
    public void ComputeAssetSummary_AtLoss_ReturnsNegativeGainLoss()
    {
        var asset = MakeAsset(quantity: 5, purchasePrice: 200m, currentPrice: 150m);

        var result = CreateSut().ComputeAssetSummary(asset, AsOf);

        Assert.Equal(750m,   result.CurrentValue);
        Assert.Equal(-250m,  result.UnrealisedGainLoss);
        Assert.Equal(-25m,   result.UnrealisedGainLossPercent);
    }

    [Fact]
    public void ComputeAssetSummary_ZeroPurchasePrice_GainLossPercentIsZero()
    {
        var asset = MakeAsset(quantity: 1, purchasePrice: 0m, currentPrice: 100m);

        var result = CreateSut().ComputeAssetSummary(asset, AsOf);

        Assert.Equal(0m, result.UnrealisedGainLossPercent);
    }

    // ── ComputeAllocation ─────────────────────────────────────────────────

    [Fact]
    public void ComputeAllocation_GroupsByTypeAndCalculatesPercents()
    {
        var assetStock1 = MakeAsset(quantity: 10, purchasePrice: 100m, currentPrice: 100m); assetStock1.Type = "Stock";
        var assetStock2 = MakeAsset(quantity: 5,  purchasePrice: 200m, currentPrice: 200m); assetStock2.Type = "Stock";
        var assetBond   = MakeAsset(quantity: 1,  purchasePrice: 500m, currentPrice: 500m); assetBond.Type   = "Bond";
        var assets = new List<Asset> { assetStock1, assetStock2, assetBond };

        var result = CreateSut().ComputeAllocation(assets, AsOf);

        var stock = result.Single(a => a.Type == "Stock");
        var bond  = result.Single(a => a.Type == "Bond");

        Assert.Equal(2000m, stock.TotalValue);  // 10*100 + 5*200
        Assert.Equal(500m,  bond.TotalValue);
        Assert.Equal(80m,   stock.AllocationPercent); // 2000/2500
        Assert.Equal(20m,   bond.AllocationPercent);
    }

    [Fact]
    public void ComputeAllocation_EmptyAssets_ReturnsEmptyList()
    {
        var result = CreateSut().ComputeAllocation([], AsOf);
        Assert.Empty(result);
    }

    [Fact]
    public void ComputeAllocation_SingleAssetType_HasHundredPercentAllocation()
    {
        var cash = MakeAsset(quantity: 2, purchasePrice: 500m, currentPrice: 500m); cash.Type = "Cash";
        var assets = new List<Asset> { cash };

        var result = CreateSut().ComputeAllocation(assets, AsOf);

        Assert.Single(result);
        Assert.Equal(100m, result[0].AllocationPercent);
    }

    // ── ComputeMonthlyPerformance ─────────────────────────────────────────

    [Fact]
    public void ComputeMonthlyPerformance_SingleMonth_ReturnsOneEntry()
    {
        var from = new DateTime(2026, 1, 1);
        var to   = new DateTime(2026, 1, 31);

        // Asset with static price — zero gain
        var asset = new Asset
        {
            Id = "a1", UserId = "u1", Name = "Fund", Type = "ETF",
            Quantity = 10, PurchasePrice = 100m,
            Price = [new PriceEntry { Value = 100m, Date = new DateTime(2025, 12, 1) }]
        };

        var result = CreateSut().ComputeMonthlyPerformance([asset], from, to);

        Assert.Single(result);
        Assert.Equal("2026-01", result[0].Month);
        Assert.Equal(0m, result[0].GainLoss);
    }

    [Fact]
    public void ComputeMonthlyPerformance_MultipleMonths_ReturnsOneEntryPerMonth()
    {
        var from = new DateTime(2026, 1, 1);
        var to   = new DateTime(2026, 3, 31);

        var result = CreateSut().ComputeMonthlyPerformance([], from, to);

        Assert.Equal(3, result.Count);
        Assert.Equal("2026-01", result[0].Month);
        Assert.Equal("2026-02", result[1].Month);
        Assert.Equal("2026-03", result[2].Month);
    }

    // ── ComputeGlobalGainLoss ─────────────────────────────────────────────

    [Fact]
    public void ComputeGlobalGainLoss_ReturnsUnrealisedGainLossAcrossPortfolio()
    {
        var assets = new List<Asset>
        {
            MakeAsset(quantity: 10, purchasePrice: 100m, currentPrice: 120m),
            MakeAsset(quantity: 5,  purchasePrice: 200m, currentPrice: 180m)
        };

        var result = CreateSut().ComputeGlobalGainLoss(assets);

        Assert.Equal(2000m,  result.TotalInvested);   // 10*100 + 5*200
        Assert.Equal(100m,   result.TotalGainLoss);   // 10*(120-100)=+200, 5*(180-200)=-100 → net 100
    }

    [Fact]
    public void ComputeGlobalGainLoss_ZeroInvested_GainLossPercentIsZero()
    {
        var result = CreateSut().ComputeGlobalGainLoss([]);

        Assert.Equal(0m, result.TotalGainLossPercent);
    }
}
