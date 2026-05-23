using BudgetApp.Api.Modules.Portfolio.Models;

namespace BudgetApp.Api.Shared;

public static class PortfolioCalculator
{
    public static decimal ResolveCurrentPrice(List<PriceEntry> history, DateTime asOf)
    {
        var valid = history.Where(e => e.Date <= asOf).ToList();
        if (valid.Count == 0) return 0;
        var maxDate = valid.Max(e => e.Date);
        // Last in array order wins when multiple entries share the same date
        return valid.LastOrDefault(e => e.Date == maxDate)?.Value ?? 0;
    }

    public static decimal ResolveCurrentAmount(List<AmountEntry> history, DateTime asOf)
    {
        var valid = history.Where(e => e.Date <= asOf).ToList();
        if (valid.Count == 0) return 0;
        var maxDate = valid.Max(e => e.Date);
        return valid.LastOrDefault(e => e.Date == maxDate)?.Value ?? 0;
    }
}
