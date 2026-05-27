using BudgetApp.Api.Modules.Portfolio.Models;

namespace BudgetApp.Api.Shared;

// Utility methods for resolving the most recent value from time-series price/amount history
public static class PortfolioCalculator
{
    // Returns the asset price that was active on or before 'asOf', or 0 if no entries exist yet for that date
    public static decimal ResolveCurrentPrice(List<PriceEntry> history, DateTime asOf)
    {
        // Discard any entries recorded after the reference date (future prices should not affect past calculations)
        var valid = history.Where(e => e.Date <= asOf).ToList();
        if (valid.Count == 0) return 0;
        // Find the most recent date among the valid entries
        var maxDate = valid.Max(e => e.Date);
        // Last in array order wins when multiple entries share the same date
        return valid.LastOrDefault(e => e.Date == maxDate)?.Value ?? 0;
    }

    // Returns the liability balance that was active on or before 'asOf', or 0 if no entries exist yet for that date
    public static decimal ResolveCurrentAmount(List<AmountEntry> history, DateTime asOf)
    {
        var valid = history.Where(e => e.Date <= asOf).ToList();
        if (valid.Count == 0) return 0;
        var maxDate = valid.Max(e => e.Date);
        // Same tie-breaking rule as ResolveCurrentPrice: the last entry in the list wins on equal dates
        return valid.LastOrDefault(e => e.Date == maxDate)?.Value ?? 0;
    }
}
