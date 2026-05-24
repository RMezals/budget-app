using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Shared;
using Xunit;

namespace BudgetApp.Tests.Portfolio;

public class PortfolioCalculatorTests
{
    // ── ResolveCurrentPrice ───────────────────────────────────────────────

    [Fact]
    public void ResolveCurrentPrice_EmptyHistory_ReturnsZero()
    {
        var result = PortfolioCalculator.ResolveCurrentPrice([], new DateTime(2026, 1, 1));
        Assert.Equal(0m, result);
    }

    [Fact]
    public void ResolveCurrentPrice_SingleEntryOnExactDate_ReturnsItsValue()
    {
        var history = new List<PriceEntry>
        {
            new() { Value = 150m, Date = new DateTime(2026, 3, 1) }
        };

        var result = PortfolioCalculator.ResolveCurrentPrice(history, new DateTime(2026, 3, 1));

        Assert.Equal(150m, result);
    }

    [Fact]
    public void ResolveCurrentPrice_AllEntriesInFuture_ReturnsZero()
    {
        var history = new List<PriceEntry>
        {
            new() { Value = 200m, Date = new DateTime(2027, 1, 1) }
        };

        var result = PortfolioCalculator.ResolveCurrentPrice(history, new DateTime(2026, 1, 1));

        Assert.Equal(0m, result);
    }

    [Fact]
    public void ResolveCurrentPrice_ReturnsLatestEntryOnOrBeforeAsOf()
    {
        var history = new List<PriceEntry>
        {
            new() { Value = 100m, Date = new DateTime(2026, 1, 1) },
            new() { Value = 120m, Date = new DateTime(2026, 3, 1) },
            new() { Value = 999m, Date = new DateTime(2026, 6, 1) }  // future
        };

        var result = PortfolioCalculator.ResolveCurrentPrice(history, new DateTime(2026, 4, 15));

        Assert.Equal(120m, result);
    }

    [Fact]
    public void ResolveCurrentPrice_MultipleSameDateEntries_ReturnsLast()
    {
        // "Last in array order wins" when dates tie
        var history = new List<PriceEntry>
        {
            new() { Value = 100m, Date = new DateTime(2026, 5, 1) },
            new() { Value = 200m, Date = new DateTime(2026, 5, 1) }
        };

        var result = PortfolioCalculator.ResolveCurrentPrice(history, new DateTime(2026, 5, 1));

        Assert.Equal(200m, result);
    }

    // ── ResolveCurrentAmount ──────────────────────────────────────────────

    [Fact]
    public void ResolveCurrentAmount_EmptyHistory_ReturnsZero()
    {
        var result = PortfolioCalculator.ResolveCurrentAmount([], new DateTime(2026, 1, 1));
        Assert.Equal(0m, result);
    }

    [Fact]
    public void ResolveCurrentAmount_ReturnsLatestEntryOnOrBeforeAsOf()
    {
        var history = new List<AmountEntry>
        {
            new() { Value = 5000m, Date = new DateTime(2026, 1, 1) },
            new() { Value = 4800m, Date = new DateTime(2026, 4, 1) },
            new() { Value = 4500m, Date = new DateTime(2026, 8, 1) }  // future
        };

        var result = PortfolioCalculator.ResolveCurrentAmount(history, new DateTime(2026, 5, 15));

        Assert.Equal(4800m, result);
    }

    [Fact]
    public void ResolveCurrentAmount_AllEntriesInFuture_ReturnsZero()
    {
        var history = new List<AmountEntry>
        {
            new() { Value = 3000m, Date = new DateTime(2027, 1, 1) }
        };

        var result = PortfolioCalculator.ResolveCurrentAmount(history, new DateTime(2026, 1, 1));

        Assert.Equal(0m, result);
    }

    [Fact]
    public void ResolveCurrentAmount_MultipleSameDateEntries_ReturnsLast()
    {
        var history = new List<AmountEntry>
        {
            new() { Value = 1000m, Date = new DateTime(2026, 3, 1) },
            new() { Value = 1200m, Date = new DateTime(2026, 3, 1) }
        };

        var result = PortfolioCalculator.ResolveCurrentAmount(history, new DateTime(2026, 3, 1));

        Assert.Equal(1200m, result);
    }
}
