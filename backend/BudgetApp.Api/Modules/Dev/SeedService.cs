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

    // ── Secondary profile (test@test.com) ────────────────────────────────────
    // Higher salary, tighter spending, one big goal, equities only, no debt.

    public async Task<SeedResult> SeedSecondaryAsync(string userId)
    {
        await ClearUserDataAsync(userId);

        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = thisMonth.AddMonths(-1);
        var twoMonths = thisMonth.AddMonths(-2);

        var transactions = BuildSecondaryTransactions(userId, thisMonth);
        var budgets = BuildSecondaryBudgets(userId, thisMonth);
        var (goals, contributions) = BuildSecondaryGoalsAndContributions(userId, thisMonth, lastMonth, twoMonths);
        var assets = BuildSecondaryAssets(userId);
        var liabilities = new List<Liability>(); // debt-free

        await _txCol.InsertManyAsync(transactions);
        await _budgetCol.InsertManyAsync(budgets);
        await _goalCol.InsertManyAsync(goals);
        await _contribCol.InsertManyAsync(contributions);
        await _assetCol.InsertManyAsync(assets);

        return new SeedResult(transactions.Count, budgets.Count, goals.Count, contributions.Count, assets.Count, 0);
    }

    // 5 000 salary, low discretionary spend, 6-month history + partial current month
    private static readonly (decimal Food, decimal Transport, decimal Entertainment, decimal Utilities, decimal Healthcare, decimal? ExtraIncome)[] SecondaryProfiles =
    [
        (380m, 80m, 50m, 90m,   0m,    null),   // M-5
        (360m, 85m, 30m, 95m,  70m,    null),   // M-4
        (370m, 80m, 60m, 90m,   0m, 1200m),     // M-3  consulting bonus
        (390m, 80m, 45m, 88m,   0m,    null),   // M-2
        (365m, 85m, 35m, 92m,   0m,    null),   // M-1
    ];

    private static List<Transaction> BuildSecondaryTransactions(string userId, DateTime thisMonth)
    {
        var txs = new List<Transaction>();

        for (var i = 0; i < SecondaryProfiles.Length; i++)
        {
            var m = thisMonth.AddMonths(-(SecondaryProfiles.Length - i));
            var p = SecondaryProfiles[i];

            txs.Add(Tx(userId, m, 5000m, "Salary", "Monthly salary"));
            txs.Add(Tx(userId, m.AddDays(1), -800m, "Housing", "Rent"));
            txs.Add(Tx(userId, m.AddDays(1), -p.Utilities, "Utilities", "Electricity & internet"));
            txs.AddRange(SplitFood(userId, m, p.Food));
            txs.Add(Tx(userId, m.AddDays(2), -p.Transport, "Transport", "Monthly pass"));
            if (p.Entertainment > 0) txs.Add(Tx(userId, m.AddDays(15), -p.Entertainment, "Entertainment", "Leisure"));
            if (p.Healthcare > 0) txs.Add(Tx(userId, m.AddDays(20), -p.Healthcare, "Healthcare", "Medical"));
            if (p.ExtraIncome > 0) txs.Add(Tx(userId, m.AddDays(10), p.ExtraIncome!.Value, "Freelance", "Consulting bonus"));
        }

        // Partial current month
        txs.Add(Tx(userId, thisMonth, 5000m, "Salary", "Monthly salary"));
        txs.Add(Tx(userId, thisMonth.AddDays(1), -800m, "Housing", "Rent"));
        txs.Add(Tx(userId, thisMonth.AddDays(1), -90m, "Utilities", "Electricity & internet"));
        txs.Add(Tx(userId, thisMonth.AddDays(2), -85m, "Food", "Groceries"));
        txs.Add(Tx(userId, thisMonth.AddDays(3), -80m, "Transport", "Monthly pass"));
        txs.Add(Tx(userId, thisMonth.AddDays(4), -42m, "Food", "Lunch"));

        return txs;
    }

    private static List<Budget> BuildSecondaryBudgets(string userId, DateTime thisMonth) =>
    [
        Budget(userId, "Housing",       thisMonth, 850m),
        Budget(userId, "Food",          thisMonth, 400m),
        Budget(userId, "Transport",     thisMonth, 100m),
        Budget(userId, "Entertainment", thisMonth,  80m),
        Budget(userId, "Utilities",     thisMonth, 110m),
        Budget(userId, "Healthcare",    thisMonth, 100m),
    ];

    private static (List<SavingsGoal> Goals, List<GoalContribution> Contributions) BuildSecondaryGoalsAndContributions(
        string userId, DateTime thisMonth, DateTime lastMonth, DateTime twoMonths)
    {
        var houseId = ObjectId.GenerateNewId().ToString();
        var carId = ObjectId.GenerateNewId().ToString();

        var goals = new List<SavingsGoal>
        {
            new() { Id = houseId, UserId = userId, Name = "House Down Payment", TargetAmount = 30000m, CurrentAmount = 18500m, Deadline = thisMonth.AddMonths(14), Description = "20% deposit on a flat", Status = GoalStatus.Active },
            new() { Id = carId,   UserId = userId, Name = "New Car",             TargetAmount = 8000m,  CurrentAmount = 3200m,  Deadline = thisMonth.AddMonths(6),  Description = "Replace old car",       Status = GoalStatus.Active },
        };

        var contributions = new List<GoalContribution>
        {
            Contrib(userId, houseId, twoMonths.AddDays(-60), 1500m, 13000m, "January"),
            Contrib(userId, houseId, twoMonths.AddDays(-30), 1500m, 14500m, "February"),
            Contrib(userId, houseId, twoMonths.AddDays(5),   1500m, 16000m, "March"),
            Contrib(userId, houseId, lastMonth.AddDays(3),   1500m, 17500m, "April"),
            Contrib(userId, houseId, thisMonth.AddDays(1),   1000m, 18500m, "May"),
            Contrib(userId, carId,   twoMonths.AddDays(-30),  600m,  1200m, "February"),
            Contrib(userId, carId,   twoMonths.AddDays(5),    600m,  1800m, "March"),
            Contrib(userId, carId,   lastMonth.AddDays(3),    800m,  2600m, "April — extra"),
            Contrib(userId, carId,   thisMonth.AddDays(1),    600m,  3200m, "May"),
        };

        return (goals, contributions);
    }

    private static List<Asset> BuildSecondaryAssets(string userId) =>
    [
        new Asset
        {
            Id = ObjectId.GenerateNewId().ToString(), UserId = userId,
            Name = "Vanguard S&P 500 (VOO)", Type = "ETF", Quantity = 20m, PurchasePrice = 440m,
            PurchaseDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Price =
            [
                new PriceEntry { Value = 440m, Date = new DateTime(2024, 6,  1, 0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 480m, Date = new DateTime(2025, 1,  1, 0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 510m, Date = new DateTime(2026, 1,  1, 0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 530m, Date = new DateTime(2026, 4,  1, 0, 0, 0, DateTimeKind.Utc) },
            ]
        },
        new Asset
        {
            Id = ObjectId.GenerateNewId().ToString(), UserId = userId,
            Name = "iShares MSCI World (IWDA)", Type = "ETF", Quantity = 30m, PurchasePrice = 85m,
            PurchaseDate = new DateTime(2023, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            Price =
            [
                new PriceEntry { Value = 85m,  Date = new DateTime(2023, 9,  1, 0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 95m,  Date = new DateTime(2024, 6,  1, 0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 105m, Date = new DateTime(2025, 1,  1, 0, 0, 0, DateTimeKind.Utc) },
                new PriceEntry { Value = 112m, Date = new DateTime(2026, 4,  1, 0, 0, 0, DateTimeKind.Utc) },
            ]
        },
    ];

    // ── Profile C (endercave@gmail.com) ──────────────────────────────────────
    // Gig/freelance worker, variable income, no investments, building emergency fund.

    public async Task<SeedResult> SeedTertiaryAsync(string userId)
    {
        await ClearUserDataAsync(userId);

        var now = DateTime.UtcNow;
        var thisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth = thisMonth.AddMonths(-1);
        var twoMonths = thisMonth.AddMonths(-2);

        var transactions = BuildTertiaryTransactions(userId, thisMonth);
        var budgets = BuildTertiaryBudgets(userId, thisMonth);
        var (goals, contributions) = BuildTertiaryGoalsAndContributions(userId, thisMonth, lastMonth, twoMonths);
        var liabilities = BuildTertiaryLiabilities(userId);

        await _txCol.InsertManyAsync(transactions);
        await _budgetCol.InsertManyAsync(budgets);
        await _goalCol.InsertManyAsync(goals);
        await _contribCol.InsertManyAsync(contributions);
        await _liabCol.InsertManyAsync(liabilities);

        return new SeedResult(transactions.Count, budgets.Count, goals.Count, contributions.Count, 0, liabilities.Count);
    }

    // Variable income: base €1 800 + occasional gig/freelance top-ups
    private static readonly (decimal BaseIncome, decimal? GigIncome, decimal Rent, decimal Food,
        decimal Transport, decimal Entertainment, decimal Utilities, decimal Healthcare)[] TertiaryProfiles =
    [
        (1800m,    null,  450m, 320m,  60m,  80m,  75m,   0m),   // M-6
        (1800m,  650m,   450m, 295m,  60m,  55m,  75m,  40m),   // M-5  gig job
        (1800m,    null,  450m, 340m,  60m,  95m,  75m,   0m),   // M-4  high food/entertainment
        (1800m,  800m,   450m, 310m,  60m,  60m,  75m,   0m),   // M-3  gig job
        (1800m,    null,  450m, 305m,  60m,  70m,  75m,  55m),   // M-2
        (1800m,  500m,   450m, 290m,  60m,  50m,  75m,   0m),   // M-1
    ];

    private static List<Transaction> BuildTertiaryTransactions(string userId, DateTime thisMonth)
    {
        var txs = new List<Transaction>();

        for (var i = 0; i < TertiaryProfiles.Length; i++)
        {
            var m = thisMonth.AddMonths(-(TertiaryProfiles.Length - i));
            var p = TertiaryProfiles[i];

            txs.Add(Tx(userId, m, p.BaseIncome, "Salary", "Part-time salary"));
            txs.Add(Tx(userId, m.AddDays(1), -p.Rent, "Housing", "Rent"));
            txs.Add(Tx(userId, m.AddDays(1), -p.Utilities, "Utilities", "Internet & phone"));
            txs.AddRange(SplitFood(userId, m, p.Food));
            txs.Add(Tx(userId, m.AddDays(2), -p.Transport, "Transport", "Monthly pass"));
            if (p.Entertainment > 0) txs.Add(Tx(userId, m.AddDays(15), -p.Entertainment, "Entertainment", "Games / streaming"));
            if (p.Healthcare > 0) txs.Add(Tx(userId, m.AddDays(20), -p.Healthcare, "Healthcare", "Dental"));
            if (p.GigIncome > 0) txs.Add(Tx(userId, m.AddDays(10), p.GigIncome!.Value, "Freelance", "Gig work"));
        }

        // Partial current month
        txs.Add(Tx(userId, thisMonth, 1800m, "Salary", "Part-time salary"));
        txs.Add(Tx(userId, thisMonth.AddDays(1), -450m, "Housing", "Rent"));
        txs.Add(Tx(userId, thisMonth.AddDays(1), -75m, "Utilities", "Internet & phone"));
        txs.Add(Tx(userId, thisMonth.AddDays(2), -65m, "Food", "Groceries"));
        txs.Add(Tx(userId, thisMonth.AddDays(3), -60m, "Transport", "Monthly pass"));
        txs.Add(Tx(userId, thisMonth.AddDays(4), -28m, "Food", "Takeaway"));
        txs.Add(Tx(userId, thisMonth.AddDays(5), -18m, "Entertainment", "Streaming"));

        return txs;
    }

    private static List<Budget> BuildTertiaryBudgets(string userId, DateTime thisMonth) =>
    [
        Budget(userId, "Housing",       thisMonth, 500m),
        Budget(userId, "Food",          thisMonth, 300m),
        Budget(userId, "Transport",     thisMonth,  80m),
        Budget(userId, "Entertainment", thisMonth, 100m),
        Budget(userId, "Utilities",     thisMonth,  90m),
        Budget(userId, "Healthcare",    thisMonth,  60m),
    ];

    private static (List<SavingsGoal> Goals, List<GoalContribution> Contributions) BuildTertiaryGoalsAndContributions(
        string userId, DateTime thisMonth, DateTime lastMonth, DateTime twoMonths)
    {
        var emergencyId = ObjectId.GenerateNewId().ToString();
        var phoneId = ObjectId.GenerateNewId().ToString();
        var courseId = ObjectId.GenerateNewId().ToString();

        var goals = new List<SavingsGoal>
        {
            new() { Id = emergencyId, UserId = userId, Name = "Emergency Fund",   TargetAmount = 1500m, CurrentAmount =  420m, Deadline = thisMonth.AddMonths(10), Description = "Cover 1 month of expenses", Status = GoalStatus.Active },
            new() { Id = phoneId,     UserId = userId, Name = "New Smartphone",   TargetAmount =  700m, CurrentAmount =  310m, Deadline = thisMonth.AddMonths(4),  Description = "Upgrade old phone",         Status = GoalStatus.Active },
            new() { Id = courseId,    UserId = userId, Name = "Design Course",    TargetAmount =  400m, CurrentAmount =  160m, Deadline = thisMonth.AddMonths(3),  Description = "UI/UX online course",       Status = GoalStatus.Active },
        };

        var contributions = new List<GoalContribution>
        {
            Contrib(userId, emergencyId, twoMonths.AddDays(-60),  80m,  160m, "January"),
            Contrib(userId, emergencyId, twoMonths.AddDays(-30),  80m,  240m, "February"),
            Contrib(userId, emergencyId, twoMonths.AddDays(5),    60m,  300m, "March"),
            Contrib(userId, emergencyId, lastMonth.AddDays(2),    60m,  360m, "April"),
            Contrib(userId, emergencyId, thisMonth.AddDays(1),    60m,  420m, "May"),
            Contrib(userId, phoneId, twoMonths.AddDays(-30),  80m,  80m,  "February"),
            Contrib(userId, phoneId, twoMonths.AddDays(5),    80m, 160m,  "March"),
            Contrib(userId, phoneId, lastMonth.AddDays(3),    80m, 240m,  "April"),
            Contrib(userId, phoneId, thisMonth.AddDays(1),    70m, 310m,  "May"),
            Contrib(userId, courseId, twoMonths.AddDays(5),   60m,  60m,  "March — decided"),
            Contrib(userId, courseId, lastMonth.AddDays(3),   50m, 110m,  "April"),
            Contrib(userId, courseId, thisMonth.AddDays(1),   50m, 160m,  "May"),
        };

        return (goals, contributions);
    }

    private static List<Liability> BuildTertiaryLiabilities(string userId) =>
    [
        new Liability
        {
            Id = ObjectId.GenerateNewId().ToString(), UserId = userId,
            Name = "Credit Card", Type = "Credit Card",
            Amount =
            [
                new AmountEntry { Value = 2200m, Date = new DateTime(2025, 9,  1, 0, 0, 0, DateTimeKind.Utc) },
                new AmountEntry { Value = 1900m, Date = new DateTime(2026, 1,  1, 0, 0, 0, DateTimeKind.Utc) },
                new AmountEntry { Value = 1500m, Date = new DateTime(2026, 4,  1, 0, 0, 0, DateTimeKind.Utc) },
            ]
        },
    ];

    // ── Shared helpers ────────────────────────────────────────────────────────

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
