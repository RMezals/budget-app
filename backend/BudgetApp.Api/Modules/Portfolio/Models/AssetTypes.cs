namespace BudgetApp.Api.Modules.Portfolio.Models;

public static class AssetTypes
{
    public static readonly IReadOnlyList<string> All =
    [
        "Stock", "ETF", "Bond", "Crypto", "RealEstate", "Cash", "Commodity", "Other"
    ];

    public static bool IsValid(string type) =>
        All.Contains(type, StringComparer.OrdinalIgnoreCase);
}
