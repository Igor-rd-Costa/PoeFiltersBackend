
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;


public class Filter: FilterBase
{
    [BsonElement("user")]
    public string User { get; set; } = string.Empty;
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("strictness")]
    public FilterStrictness Strictness { get; set; } = FilterStrictness.REGULAR;
    [BsonElement("created_at")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime CreatedAt { get; set; }
    [BsonElement("modified_at")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime ModifiedAt { get; set; }
    [BsonElement("sections")]
    public List<FilterSection> Sections { get; set; } = [];
    [JsonPropertyName("defaultVersion")]
    public uint DefaultVersion { get; set; } = 0;
}