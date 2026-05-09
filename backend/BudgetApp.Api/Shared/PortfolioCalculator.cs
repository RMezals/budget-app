using BudgetApp.Api.Modules.Portfolio.Models;

namespace BudgetApp.Api.Shared;

public static class PortfolioCalculator
{
    public static decimal ResolveCurrentPrice(List<PriceEntry> history, DateTime asOf) =>
        history.Where(e => e.Date <= asOf).MaxBy(e => e.Date)?.Value ?? 0;

    public static decimal ResolveCurrentAmount(List<AmountEntry> history, DateTime asOf) =>
        history.Where(e => e.Date <= asOf).MaxBy(e => e.Date)?.Value ?? 0;
}
