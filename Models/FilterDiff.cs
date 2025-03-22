

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;
using System;

public class FilterDiff(uint version)
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [JsonPropertyName("id")]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [JsonPropertyName("version")]
    public uint Version { get; set; } = version;

    [BsonIgnoreIfNull()]
    [JsonPropertyName("sectionChanges")]
    public FilterUpdateChanges<FilterSection, FilterSectionDiff> SectionChanges { get; set; } = new();
}