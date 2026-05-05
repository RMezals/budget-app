using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BudgetApp.Api.Modules.Portfolio.Models;

public class Asset
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    public string UserId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Quantity { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal PurchasePrice { get; set; }

    public DateTime PurchaseDate { get; set; }

    // Append-only; current price = most recent entry with Date <= today
    public List<PriceEntry> Price { get; set; } = [];
}

public class PriceEntry
{
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Value { get; set; }

    public DateTime Date { get; set; }
}
