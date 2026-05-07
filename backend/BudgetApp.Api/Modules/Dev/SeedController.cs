using BudgetApp.Api.Controllers;
using BudgetApp.Api.Modules.Portfolio.Models;
using BudgetApp.Api.Modules.Savings.Models;
using BudgetApp.Api.Modules.Transactions.Models;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace BudgetApp.Api.Modules.Dev;

[ApiController]
[Route("api/dev")]
public class SeedController(IMongoDatabase db, IWebHostEnvironment env) : ApiControllerBase
{
    /// <summary>
    /// Clears and re-seeds realistic sample data for the authenticated user.
    /// Only available in Development.
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        if (!env.IsDevelopment())
            return Forbid();

        var txCol     = db.GetCollection<Transaction>("transactions");
        var budgetCol = db.GetCollection<Budget>("budgets");
        var goalCol   = db.GetCollection<SavingsGoal>("savings_goals");
        var contribCol= db.GetCollection<GoalContribution>("goal_contributions");
        var assetCol  = db.GetCollection<Asset>("assets");
        var liabCol   = db.GetCollection<Liability>("liabilities");

        // Clear existing data for this user across all collections
        await txCol.DeleteManyAsync(x => x.UserId == UserId);
        await budgetCol.DeleteManyAsync(x => x.UserId == UserId);
        await goalCol.DeleteManyAsync(x => x.UserId == UserId);
        await contribCol.DeleteManyAsync(x => x.UserId == UserId);
        await assetCol.DeleteManyAsync(x => x.UserId == UserId);
        await liabCol.DeleteManyAsync(x => x.UserId == UserId);

        var now        = DateTime.UtcNow;
        var thisMonth  = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonth  = thisMonth.AddMonths(-1);
        var twoMonths  = thisMonth.AddMonths(-2);

        // ── Transactions ────────────────────────────────────────────────

        var transactions = new List<Transaction>
        {
            // Two months ago
            Tx(twoMonths.AddDays(0),  3200m,   "Salary",        "March salary"),
            Tx(twoMonths.AddDays(0),  -900m,   "Housing",       "Rent"),
            Tx(twoMonths.AddDays(1),  -98m,    "Utilities",     "Electricity & internet"),
            Tx(twoMonths.AddDays(3),  -62m,    "Food",          "Weekly groceries"),
            Tx(twoMonths.AddDays(7),  -55m,    "Food",          "Groceries"),
            Tx(twoMonths.AddDays(8),  -28m,    "Food",          "Restaurant"),
            Tx(twoMonths.AddDays(10), -95m,    "Transport",     "Monthly bus pass"),
            Tx(twoMonths.AddDays(12), -55m,    "Entertainment", "Cinema & dinner"),
            Tx(twoMonths.AddDays(14), -60m,    "Food",          "Groceries"),
            Tx(twoMonths.AddDays(18), -45m,    "Healthcare",    "Pharmacy"),
            Tx(twoMonths.AddDays(21), -65m,    "Food",          "Groceries"),
            Tx(twoMonths.AddDays(25), -40m,    "Entertainment", "Streaming + games"),
            Tx(twoMonths.AddDays(28), -55m,    "Food",          "Groceries + restaurant"),

            // Last month
            Tx(lastMonth.AddDays(0),  3200m,   "Salary",        "April salary"),
            Tx(lastMonth.AddDays(0),  -900m,   "Housing",       "Rent"),
            Tx(lastMonth.AddDays(1),  -105m,   "Utilities",     "Electricity & internet"),
            Tx(lastMonth.AddDays(2),  -120m,   "Clothing",      "Spring clothes"),
            Tx(lastMonth.AddDays(3),  -68m,    "Food",          "Weekly groceries"),
            Tx(lastMonth.AddDays(5),  -35m,    "Transport",     "Fuel"),
            Tx(lastMonth.AddDays(7),  -22m,    "Food",          "Lunch out"),
            Tx(lastMonth.AddDays(9),  -75m,    "Entertainment", "Concert tickets"),
            Tx(lastMonth.AddDays(10), -60m,    "Food",          "Groceries"),
            Tx(lastMonth.AddDays(12), -60m,    "Healthcare",    "Doctor visit"),
            Tx(lastMonth.AddDays(14), -110m,   "Transport",     "Monthly bus pass + taxi"),
            Tx(lastMonth.AddDays(16), -72m,    "Food",          "Groceries"),
            Tx(lastMonth.AddDays(20), -58m,    "Food",          "Groceries + coffee"),
            Tx(lastMonth.AddDays(22), -800m,   "Other",         "Freelance equipment"),
            Tx(lastMonth.AddDays(25), 400m,    "Freelance",     "Freelance project payment"),
            Tx(lastMonth.AddDays(28), -45m,    "Food",          "Weekend restaurant"),

            // This month (partial — first week)
            Tx(thisMonth.AddDays(0),  3200m,   "Salary",        "May salary"),
            Tx(thisMonth.AddDays(0),  -900m,   "Housing",       "Rent"),
            Tx(thisMonth.AddDays(1),  -95m,    "Utilities",     "Electricity & internet"),
            Tx(thisMonth.AddDays(2),  -45m,    "Food",          "Groceries"),
            Tx(thisMonth.AddDays(3),  -35m,    "Transport",     "Fuel"),
            Tx(thisMonth.AddDays(4),  -40m,    "Entertainment", "Cinema"),
            Tx(thisMonth.AddDays(5),  -22m,    "Food",          "Lunch"),
            Tx(thisMonth.AddDays(6),  -15m,    "Transport",     "Parking"),
        };

        await txCol.InsertManyAsync(transactions);

        // ── Budgets (current month only) ────────────────────────────────

        var budgets = new[]
        {
            Budget("Housing",       thisMonth, 950m),
            Budget("Food",          thisMonth, 350m),
            Budget("Transport",     thisMonth, 150m),
            Budget("Entertainment", thisMonth, 100m),
            Budget("Utilities",     thisMonth, 150m),
            Budget("Healthcare",    thisMonth,  80m),
            Budget("Clothing",      thisMonth, 100m),
        };

        await budgetCol.InsertManyAsync(budgets);

        // ── Savings Goals + Contributions ───────────────────────────────

        var emergencyId = ObjectId.GenerateNewId().ToString();
        var laptopId    = ObjectId.GenerateNewId().ToString();
        var vacationId  = ObjectId.GenerateNewId().ToString();

        var goals = new[]
        {
            new SavingsGoal
            {
                Id            = emergencyId,
                UserId        = UserId,
                Name          = "Emergency Fund",
                TargetAmount  = 5000m,
                CurrentAmount = 2800m,
                Deadline      = thisMonth.AddMonths(8),
                Description   = "3 months of living expenses",
                Status        = GoalStatus.Active
            },
            new SavingsGoal
            {
                Id            = laptopId,
                UserId        = UserId,
                Name          = "New Laptop",
                TargetAmount  = 1500m,
                CurrentAmount = 950m,
                Deadline      = thisMonth.AddMonths(3),
                Description   = "Dev machine replacement",
                Status        = GoalStatus.Active
            },
            new SavingsGoal
            {
                Id            = vacationId,
                UserId        = UserId,
                Name          = "Summer Vacation",
                TargetAmount  = 2000m,
                CurrentAmount = 350m,
                Deadline      = thisMonth.AddMonths(2),
                Description   = "Trip to Portugal",
                Status        = GoalStatus.Active
            },
        };

        await goalCol.InsertManyAsync(goals);

        // Contributions must sum to CurrentAmount for each goal
        var contributions = new List<GoalContribution>
        {
            // Emergency Fund: 500+500+500+800+500 = 2800
            Contrib(emergencyId, twoMonths.AddDays(-60), 500m,  500m,  "January deposit"),
            Contrib(emergencyId, twoMonths.AddDays(-30), 500m,  1000m, "February deposit"),
            Contrib(emergencyId, twoMonths.AddDays(5),   500m,  1500m, "March deposit"),
            Contrib(emergencyId, lastMonth.AddDays(2),   800m,  2300m, "April — bonus added"),
            Contrib(emergencyId, lastMonth.AddDays(15),  500m,  2800m, "April deposit"),

            // New Laptop: 200+250+300+200 = 950
            Contrib(laptopId, twoMonths.AddDays(-30), 200m, 200m, "February"),
            Contrib(laptopId, twoMonths.AddDays(5),   250m, 450m, "March"),
            Contrib(laptopId, lastMonth.AddDays(3),   300m, 750m, "April"),
            Contrib(laptopId, thisMonth.AddDays(1),   200m, 950m, "May"),

            // Summer Vacation: 150+100+100 = 350
            Contrib(vacationId, twoMonths.AddDays(5),  150m, 150m, "March — trip decided"),
            Contrib(vacationId, lastMonth.AddDays(3),  100m, 250m, "April"),
            Contrib(vacationId, thisMonth.AddDays(1),  100m, 350m, "May"),
        };

        await contribCol.InsertManyAsync(contributions);

        // ── Assets ──────────────────────────────────────────────────────

        var assets = new[]
        {
            new Asset
            {
                Id            = ObjectId.GenerateNewId().ToString(),
                UserId        = UserId,
                Name          = "Apple (AAPL)",
                Type          = "Stocks",
                Quantity      = 15m,
                PurchasePrice = 170m,
                PurchaseDate  = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                Price         =
                [
                    new PriceEntry { Value = 170m, Date = new DateTime(2025, 3, 15, 0, 0, 0, DateTimeKind.Utc) },
                    new PriceEntry { Value = 185m, Date = new DateTime(2025, 9, 1,  0, 0, 0, DateTimeKind.Utc) },
                    new PriceEntry { Value = 195m, Date = new DateTime(2026, 1, 1,  0, 0, 0, DateTimeKind.Utc) },
                    new PriceEntry { Value = 210m, Date = new DateTime(2026, 4, 1,  0, 0, 0, DateTimeKind.Utc) },
                ]
            },
            new Asset
            {
                Id            = ObjectId.GenerateNewId().ToString(),
                UserId        = UserId,
                Name          = "Vanguard S&P 500 (VOO)",
                Type          = "ETF",
                Quantity      = 8m,
                PurchasePrice = 450m,
                PurchaseDate  = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                Price         =
                [
                    new PriceEntry { Value = 450m, Date = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc) },
                    new PriceEntry { Value = 480m, Date = new DateTime(2025, 7, 1,  0, 0, 0, DateTimeKind.Utc) },
                    new PriceEntry { Value = 510m, Date = new DateTime(2026, 1, 1,  0, 0, 0, DateTimeKind.Utc) },
                    new PriceEntry { Value = 530m, Date = new DateTime(2026, 4, 1,  0, 0, 0, DateTimeKind.Utc) },
                ]
            },
            new Asset
            {
                Id            = ObjectId.GenerateNewId().ToString(),
                UserId        = UserId,
                Name          = "Bitcoin (BTC)",
                Type          = "Crypto",
                Quantity      = 0.05m,
                PurchasePrice = 65000m,
                PurchaseDate  = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                Price         =
                [
                    new PriceEntry { Value = 65000m, Date = new DateTime(2024, 6,  1, 0, 0, 0, DateTimeKind.Utc) },
                    new PriceEntry { Value = 95000m, Date = new DateTime(2025, 1,  1, 0, 0, 0, DateTimeKind.Utc) },
                    new PriceEntry { Value = 82000m, Date = new DateTime(2026, 4,  1, 0, 0, 0, DateTimeKind.Utc) },
                ]
            },
        };

        await assetCol.InsertManyAsync(assets);

        // ── Liabilities ─────────────────────────────────────────────────

        var liabilities = new[]
        {
            new Liability
            {
                Id     = ObjectId.GenerateNewId().ToString(),
                UserId = UserId,
                Name   = "Student Loan",
                Type   = "Loan",
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
                Id     = ObjectId.GenerateNewId().ToString(),
                UserId = UserId,
                Name   = "Credit Card",
                Type   = "Credit Card",
                Amount =
                [
                    new AmountEntry { Value = 1200m, Date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new AmountEntry { Value =  800m, Date = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc) },
                ]
            },
        };

        await liabCol.InsertManyAsync(liabilities);

        return Ok(new
        {
            message      = "Seed complete",
            userId       = UserId,
            transactions = transactions.Count,
            budgets      = budgets.Length,
            goals        = goals.Length,
            contributions= contributions.Count,
            assets       = assets.Length,
            liabilities  = liabilities.Length,
        });
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private Transaction Tx(DateTime date, decimal amount, string category, string? description = null) => new()
    {
        Id          = ObjectId.GenerateNewId().ToString(),
        UserId      = UserId,
        Amount      = amount,
        Date        = date,
        Category    = category,
        Description = description,
    };

    private Budget Budget(string category, DateTime monthStart, decimal limit) => new()
    {
        Id          = ObjectId.GenerateNewId().ToString(),
        UserId      = UserId,
        Category    = category,
        Date        = monthStart,
        LimitAmount = limit,
    };

    private GoalContribution Contrib(string goalId, DateTime date, decimal amount, decimal balanceAfter, string? description = null) => new()
    {
        Id           = ObjectId.GenerateNewId().ToString(),
        GoalId       = goalId,
        UserId       = UserId,
        Amount       = amount,
        Date         = date,
        BalanceAfter = balanceAfter,
        Description  = description,
    };
}
