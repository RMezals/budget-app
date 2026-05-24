using BudgetApp.Api.Configuration;
using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Transactions.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Dev;

public class SeedService(IMongoDatabase db) : ISeedService
{
    private readonly IMongoCollection<Transaction> _txCol = db.GetCollection<Transaction>(CollectionNames.Transactions);
    private readonly IMongoCollection<Budget> _budgetCol = db.GetCollection<Budget>(CollectionNames.Budgets);
    private readonly IMongoCollection<SavingsGoal> _goalCol = db.GetCollection<SavingsGoal>(CollectionNames.SavingsGoals);
    private readonly IMongoCollection<GoalContribution> _contribCol = db.GetCollection<GoalContribution>(CollectionNames.GoalContributions);
    private readonly IMongoCollection<Asset> _assetCol = db.GetCollection<Asset>(CollectionNames.Assets);
    private readonly IMongoCollection<Liability> _liabCol = db.GetCollection<Liability>(CollectionNames.Liabilities);

    public async Task<SeedResult> SeedAsync(string userId)
    {
        await ClearUserDataAsync(userId);

        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = thisMonth.AddMonths(-1);
        var twoMonths = thisMonth.AddMonths(-2);

        var transactions = BuildTransactions(userId, thisMonth);
        var budgets = BuildBudgets(userId, thisMonth);
        var (goals, contributions) = BuildGoalsAndContributions(userId, thisMonth, lastMonth, twoMonths);
        var assets = BuildAssets(userId);
        var liabilities = BuildLiabilities(userId);

        await _txCol.InsertManyAsync(transactions);
        await _budgetCol.InsertManyAsync(budgets);
        await _goalCol.InsertManyAsync(goals);
        await _contribCol.InsertManyAsync(contributions);
        await _assetCol.InsertManyAsync(assets);
        await _liabCol.InsertManyAsync(liabilities);

        return new SeedResult(transactions.Count, budgets.Count, goals.Count, contributions.Count, assets.Count, liabilities.Count);
    }

    private async Task ClearUserDataAsync(string userId)
    {
        await _txCol.DeleteManyAsync(x => x.UserId == userId);
        await _budgetCol.DeleteManyAsync(x => x.UserId == userId);
        await _goalCol.DeleteManyAsync(x => x.UserId == userId);
        await _contribCol.DeleteManyAsync(x => x.UserId == userId);
        await _assetCol.DeleteManyAsync(x => x.UserId == userId);
        await _liabCol.DeleteManyAsync(x => x.UserId == userId);
    }

    // Per-month spending profile (Housing, Utilities, Food, Transport, Entertainment, Healthcare, Clothing, Other)
    private static readonly (decimal Housing, decimal Utilities, decimal Food, decimal Transport,
        decimal Entertainment, decimal Healthcare, decimal Clothing, decimal Other, decimal? ExtraIncome, string? ExtraNote)[] MonthProfiles =
    [
        (900m,  92m,  290m,  95m,  45m,   0m,    0m,   0m,   null,  null),                    // M-11
        (900m,  88m,  305m,  95m,  60m,  45m,    0m,   0m,   null,  null),                    // M-10
        (900m,  95m,  280m,  95m,  80m,   0m,  150m,   0m,   null,  null),                    // M-9  winter clothes
        (900m, 110m,  295m,  95m,  40m,   0m,    0m,   0m,   null,  null),                    // M-8
        (900m, 118m,  310m,  95m,  55m,  60m,    0m,   0m,   null,  null),                    // M-7
        (900m, 105m,  300m,  95m,  70m,   0m,    0m, 800m,  400m,  "Freelance project"),      // M-6  big purchase
        (900m,  98m,  290m,  95m,  55m,  45m,    0m,   0m,   null,  null),                    // M-5
        (900m,  90m,  285m,  95m,  40m,   0m,    0m,   0m,   null,  null),                    // M-4
        (900m,  98m,  300m,  95m,  55m,  45m,    0m,   0m,   null,  null),                    // M-3  = twoMonths
        (900m, 105m,  345m, 145m,  75m,  60m,  120m,   0m,  400m,  "Freelance project"),      // M-2  = lastMonth
        (900m,  95m,  270m,  95m,  40m,   0m,    0m,   0m,   null,  null),                    // M-1  (current - partial)
    ];

    private static List<Transaction> BuildTransactions(string userId, DateTime thisMonth)
    {
        var txs = new List<Transaction>();

        for (var i = 0; i < MonthProfiles.Length; i++)
        {
            var m = thisMonth.AddMonths(-(MonthProfiles.Length - i));
            var p = MonthProfiles[i];

            txs.Add(Tx(userId, m.AddDays(0), 3200m, "Salary", "Monthly salary"));
            txs.Add(Tx(userId, m.AddDays(0), -p.Housing, "Housing", "Rent"));
            txs.Add(Tx(userId, m.AddDays(1), -p.Utilities, "Utilities", "Electricity & internet"));
            txs.AddRange(SplitFood(userId, m, p.Food));
            txs.AddRange(SplitTransport(userId, m, p.Transport));
            if (p.Entertainment > 0) txs.Add(Tx(userId, m.AddDays(12), -p.Entertainment, "Entertainment", "Leisure"));
            if (p.Healthcare > 0) txs.Add(Tx(userId, m.AddDays(18), -p.Healthcare, "Healthcare", "Medical"));
            if (p.Clothing > 0) txs.Add(Tx(userId, m.AddDays(3), -p.Clothing, "Clothing", "Clothes"));
            if (p.Other > 0) txs.Add(Tx(userId, m.AddDays(22), -p.Other, "Other", "Equipment"));
            if (p.ExtraIncome > 0) txs.Add(Tx(userId, m.AddDays(25), p.ExtraIncome!.Value, "Freelance", p.ExtraNote));
        }

        // Current month (partial — only a few days in)
        txs.Add(Tx(userId, thisMonth.AddDays(0), 3200m, "Salary", "Monthly salary"));
        txs.Add(Tx(userId, thisMonth.AddDays(0), -900m, "Housing", "Rent"));
        txs.Add(Tx(userId, thisMonth.AddDays(1), -95m, "Utilities", "Electricity & internet"));
        txs.Add(Tx(userId, thisMonth.AddDays(2), -45m, "Food", "Groceries"));
        txs.Add(Tx(userId, thisMonth.AddDays(3), -35m, "Transport", "Fuel"));
        txs.Add(Tx(userId, thisMonth.AddDays(4), -40m, "Entertainment", "Cinema"));
        txs.Add(Tx(userId, thisMonth.AddDays(5), -22m, "Food", "Lunch"));
        txs.Add(Tx(userId, thisMonth.AddDays(6), -15m, "Transport", "Parking"));

        return txs;
    }

    // Spread food spending across ~4 weekly shops so the transaction list looks realistic
    private static IEnumerable<Transaction> SplitFood(string userId, DateTime month, decimal total)
    {
        var share = Math.Round(total / 4, 2);
        yield return Tx(userId, month.AddDays(3), -share, "Food", "Weekly groceries");
        yield return Tx(userId, month.AddDays(7), -share, "Food", "Groceries");
        yield return Tx(userId, month.AddDays(14), -share, "Food", "Groceries");
        yield return Tx(userId, month.AddDays(21), -(total - share * 3), "Food", "Groceries + restaurant");
    }

    private static IEnumerable<Transaction> SplitTransport(string userId, DateTime month, decimal total)
    {
        yield return Tx(userId, month.AddDays(1), -Math.Round(total * 0.6m, 2), "Transport", "Monthly bus pass");
        yield return Tx(userId, month.AddDays(14), -(total - Math.Round(total * 0.6m, 2)), "Transport", "Fuel / taxi");
    }

    private static List<Budget> BuildBudgets(string userId, DateTime thisMonth) =>
    [
        Budget(userId, "Housing",       thisMonth, 950m),
        Budget(userId, "Food",          thisMonth, 350m),
        Budget(userId, "Transport",     thisMonth, 150m),
        Budget(userId, "Entertainment", thisMonth, 100m),
        Budget(userId, "Utilities",     thisMonth, 150m),
        Budget(userId, "Healthcare",    thisMonth,  80m),
        Budget(userId, "Clothing",      thisMonth, 100m),
    ];

    private static (List<SavingsGoal> Goals, List<GoalContribution> Contributions) BuildGoalsAndContributions(
        string userId, DateTime thisMonth, DateTime lastMonth, DateTime twoMonths)
    {
        var emergencyId = ObjectId.GenerateNewId().ToString();
        var laptopId = ObjectId.GenerateNewId().ToString();
        var vacationId = ObjectId.GenerateNewId().ToString();

        var goals = new List<SavingsGoal>
        {
            new() { Id = emergencyId, UserId = userId, Name = "Emergency Fund",  TargetAmount = 5000m, CurrentAmount = 2800m, Deadline = thisMonth.AddMonths(8), Description = "3 months of living expenses", Status = GoalStatus.Active },
            new() { Id = laptopId,    UserId = userId, Name = "New Laptop",       TargetAmount = 1500m, CurrentAmount = 950m,  Deadline = thisMonth.AddMonths(3), Description = "Dev machine replacement",    Status = GoalStatus.Active },
            new() { Id = vacationId,  UserId = userId, Name = "Summer Vacation",  TargetAmount = 2000m, CurrentAmount = 350m,  Deadline = thisMonth.AddMonths(2), Description = "Trip to Portugal",           Status = GoalStatus.Active },
        };

        var contributions = new List<GoalContribution>
        {
            Contrib(userId, emergencyId, twoMonths.AddDays(-60), 500m,  500m,  "January deposit"),
            Contrib(userId, emergencyId, twoMonths.AddDays(-30), 500m,  1000m, "February deposit"),
            Contrib(userId, emergencyId, twoMonths.AddDays(5),   500m,  1500m, "March deposit"),
            Contrib(userId, emergencyId, lastMonth.AddDays(2),   800m,  2300m, "April — bonus added"),
            Contrib(userId, emergencyId, lastMonth.AddDays(15),  500m,  2800m, "April deposit"),
            Contrib(userId, laptopId, twoMonths.AddDays(-30), 200m, 200m, "February"),
            Contrib(userId, laptopId, twoMonths.AddDays(5),   250m, 450m, "March"),
            Contrib(userId, laptopId, lastMonth.AddDays(3),   300m, 750m, "April"),
            Contrib(userId, laptopId, thisMonth.AddDays(1),   200m, 950m, "May"),
            Contrib(userId, vacationId, twoMonths.AddDays(5), 150m, 150m, "March — trip decided"),
            Contrib(userId, vacationId, lastMonth.AddDays(3), 100m, 250m, "April"),
            Contrib(userId, vacationId, thisMonth.AddDays(1), 100m, 350m, "May"),
        };

        return (goals, contributions);
    }

    private static List<Asset> BuildAssets(string userId) =>
    [
        new Asset
        {
            Id = ObjectId.GenerateNewId().ToString(), UserId = userId,
            Name = "Apple (AAPL)", Type = "Stocks", Quantity = 15m, PurchasePrice = 170m,
            PurchaseDate = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            Price =
            [
                new PriceEntry { Value = 170m, Date = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 185m, Date = new DateTime(2025, 9,  1,  0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 195m, Date = new DateTime(2026, 1,  1,  0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 210m, Date = new DateTime(2026, 4,  1,  0, 0, 0, DateTimeKind.Utc) },
            ]
        },
        new Asset
        {
            Id = ObjectId.GenerateNewId().ToString(), UserId = userId,
            Name = "Vanguard S&P 500 (VOO)", Type = "ETF", Quantity = 8m, PurchasePrice = 450m,
            PurchaseDate = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            Price =
            [
                new PriceEntry { Value = 450m, Date = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 480m, Date = new DateTime(2025, 7,  1,  0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 510m, Date = new DateTime(2026, 1,  1,  0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 530m, Date = new DateTime(2026, 4,  1,  0, 0, 0, DateTimeKind.Utc) },
            ]
        },
        new Asset
        {
            Id = ObjectId.GenerateNewId().ToString(), UserId = userId,
            Name = "Bitcoin (BTC)", Type = "Crypto", Quantity = 0.05m, PurchasePrice = 65000m,
            PurchaseDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Price =
            [
                new PriceEntry { Value = 65000m, Date = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 95000m, Date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 82000m, Date = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc) },
            ]
        },
    ];

    private static List<Liability> BuildLiabilities(string userId) =>
    [
        new Liability
        {
            Id = ObjectId.GenerateNewId().ToString(), UserId = userId,
            Name = "Student Loan", Type = "Loan",
            Amount =
            [
                new AmountEntry { Value = 8500m, Date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new AmountEntry { Value = 7800m, Date = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc) },
                new AmountEntry { Value = 7100m, Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new AmountEntry { Value = 6500m, Date = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) },
            ]
        },
        new Liability
        {
            Id = ObjectId.GenerateNewId().ToString(), UserId = userId,
            Name = "Credit Card", Type = "Credit Card",
            Amount =
            [
                new AmountEntry { Value = 1200m, Date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
                new AmountEntry { Value =  800m, Date = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) },
            ]
        },
    ];

    private static Transaction Tx(string userId, DateTime date, decimal amount, string category, string? description = null) => new()
    {
        Id = ObjectId.GenerateNewId().ToString(),
        UserId = userId,
        Amount = amount,
        Date = date,
        Category = category,
        Description = description,
    };

    private static Budget Budget(string userId, string category, DateTime monthStart, decimal limit) => new()
    {
        Id = ObjectId.GenerateNewId().ToString(),
        UserId = userId,
        Category = category,
        Date = monthStart,
        LimitAmount = limit,
    };

    private static GoalContribution Contrib(string userId, string goalId, DateTime date, decimal amount, decimal balanceAfter, string? description = null) => new()
    {
        Id = ObjectId.GenerateNewId().ToString(),
        GoalId = goalId,
        UserId = userId,
        Amount = amount,
        Date = date,
        BalanceAfter = balanceAfter,
        Description = description,
    };
}
