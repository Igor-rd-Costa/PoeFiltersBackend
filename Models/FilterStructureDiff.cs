

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

public class FilterStructureDiff(uint version)
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [JsonPropertyName("version")]
    public uint Version { get; set; } = version;

    [BsonIgnoreIfNull()]
    [JsonPropertyName("sectionChanges")]
    public FilterUpdateChanges<FilterSectionStructure, FilterSectionStructureDiff> SectionChanges { get; set; } = new();
}