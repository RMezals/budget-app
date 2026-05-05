using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BudgetApp.Api.Modules.Portfolio.Models;

public class Liability
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string UserId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;

    // Append-only; current balance = most recent entry with Date <= today
    public List<AmountEntry> Amount { get; set; } = [];
}

public class AmountEntry
{
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Value { get; set; }

    public DateTime Date { get; set; }
}
