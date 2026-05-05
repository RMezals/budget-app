using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BudgetApp.Api.Modules.Transactions.Models;

public class Transaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string UserId { get; set; } = default!;

    // Positive = income, negative = expense
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    public DateTime Date { get; set; }
    public string Category { get; set; } = default!;
    public string? Description { get; set; }
}
