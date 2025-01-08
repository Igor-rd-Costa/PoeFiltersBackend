
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


public class Filter
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    [BsonElement("user")]
    public string? User { get; set; }
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    [BsonElement("game")]
    public string Game { get; set; } = string.Empty;
    [BsonElement("sections")]
    public List<FilterSection> Sections { get; set; } = [];
    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }
    [BsonElement("modified_at")]
    public DateTime ModifiedAt { get; set; }
}