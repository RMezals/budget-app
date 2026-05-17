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

        var transactions = BuildTransactions(userId, thisMonth, lastMonth, twoMonths);
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

    private static List<Transaction> BuildTransactions(string userId, DateTime thisMonth, DateTime lastMonth, DateTime twoMonths) =>
    [
        Tx(userId, twoMonths.AddDays(0),  3200m,  "Salary",        "March salary"),
        Tx(userId, twoMonths.AddDays(0),  -900m,  "Housing",       "Rent"),
        Tx(userId, twoMonths.AddDays(1),  -98m,   "Utilities",     "Electricity & internet"),
        Tx(userId, twoMonths.AddDays(3),  -62m,   "Food",          "Weekly groceries"),
        Tx(userId, twoMonths.AddDays(7),  -55m,   "Food",          "Groceries"),
        Tx(userId, twoMonths.AddDays(8),  -28m,   "Food",          "Restaurant"),
        Tx(userId, twoMonths.AddDays(10), -95m,   "Transport",     "Monthly bus pass"),
        Tx(userId, twoMonths.AddDays(12), -55m,   "Entertainment", "Cinema & dinner"),
        Tx(userId, twoMonths.AddDays(14), -60m,   "Food",          "Groceries"),
        Tx(userId, twoMonths.AddDays(18), -45m,   "Healthcare",    "Pharmacy"),
        Tx(userId, twoMonths.AddDays(21), -65m,   "Food",          "Groceries"),
        Tx(userId, twoMonths.AddDays(25), -40m,   "Entertainment", "Streaming + games"),
        Tx(userId, twoMonths.AddDays(28), -55m,   "Food",          "Groceries + restaurant"),
        Tx(userId, lastMonth.AddDays(0),  3200m,  "Salary",        "April salary"),
        Tx(userId, lastMonth.AddDays(0),  -900m,  "Housing",       "Rent"),
        Tx(userId, lastMonth.AddDays(1),  -105m,  "Utilities",     "Electricity & internet"),
        Tx(userId, lastMonth.AddDays(2),  -120m,  "Clothing",      "Spring clothes"),
        Tx(userId, lastMonth.AddDays(3),  -68m,   "Food",          "Weekly groceries"),
        Tx(userId, lastMonth.AddDays(5),  -35m,   "Transport",     "Fuel"),
        Tx(userId, lastMonth.AddDays(7),  -22m,   "Food",          "Lunch out"),
        Tx(userId, lastMonth.AddDays(9),  -75m,   "Entertainment", "Concert tickets"),
        Tx(userId, lastMonth.AddDays(10), -60m,   "Food",          "Groceries"),
        Tx(userId, lastMonth.AddDays(12), -60m,   "Healthcare",    "Doctor visit"),
        Tx(userId, lastMonth.AddDays(14), -110m,  "Transport",     "Monthly bus pass + taxi"),
        Tx(userId, lastMonth.AddDays(16), -72m,   "Food",          "Groceries"),
        Tx(userId, lastMonth.AddDays(20), -58m,   "Food",          "Groceries + coffee"),
        Tx(userId, lastMonth.AddDays(22), -800m,  "Other",         "Freelance equipment"),
        Tx(userId, lastMonth.AddDays(25), 400m,   "Freelance",     "Freelance project payment"),
        Tx(userId, lastMonth.AddDays(28), -45m,   "Food",          "Weekend restaurant"),
        Tx(userId, thisMonth.AddDays(0),  3200m,  "Salary",        "May salary"),
        Tx(userId, thisMonth.AddDays(0),  -900m,  "Housing",       "Rent"),
        Tx(userId, thisMonth.AddDays(1),  -95m,   "Utilities",     "Electricity & internet"),
        Tx(userId, thisMonth.AddDays(2),  -45m,   "Food",          "Groceries"),
        Tx(userId, thisMonth.AddDays(3),  -35m,   "Transport",     "Fuel"),
        Tx(userId, thisMonth.AddDays(4),  -40m,   "Entertainment", "Cinema"),
        Tx(userId, thisMonth.AddDays(5),  -22m,   "Food",          "Lunch"),
        Tx(userId, thisMonth.AddDays(6),  -15m,   "Transport",     "Parking"),
    ];

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
