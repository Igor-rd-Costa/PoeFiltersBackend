

using MongoDB.Bson.Serialization.Attributes;

public class ItemCategory
{
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IgnoreItems { get; set; } = false;
}