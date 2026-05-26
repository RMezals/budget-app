using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace BudgetApp.Api.Modules.Savings.Models;

[BsonIgnoreExtraElements]
public class GoalContribution
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string GoalId { get; set; } = default!;

    public string UserId { get; set; } = default!;

    // Positive = deposit, negative = withdrawal
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public string? Reason { get; set; }
    public string? Description { get; set; }

    [BsonElement("Note")]
    [BsonIgnoreIfNull]
    [JsonIgnore]
    internal string? Note { get; set; }

    // Goal balance immediately after this contribution (for timeline display)
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal BalanceAfter { get; set; }
}
