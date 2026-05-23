using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BudgetApp.Api.Modules.Savings.Models;

public class SavingsGoal
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string UserId { get; set; } = default!;
    public string Name { get; set; } = default!;

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal TargetAmount { get; set; }

    // Running total updated atomically on each contribution add/remove
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal CurrentAmount { get; set; }

    public DateTime Deadline { get; set; }
    public string? Description { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public enum GoalStatus { Active, Completed, Paused, Abandoned }
