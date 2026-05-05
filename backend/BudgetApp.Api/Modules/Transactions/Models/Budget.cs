using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BudgetApp.Api.Modules.Transactions.Models;

public class Budget
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string UserId { get; set; } = default!;
    public string Category { get; set; } = default!;

    // First day of the month this budget applies to
    public DateTime Date { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal LimitAmount { get; set; }
}
