
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


public class Filter
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    [BsonElement("user")]
    public string User { get; set; } = string.Empty;
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
    [BsonElement("game")]
    public string Game { get; set; } = string.Empty;
    [BsonElement("sections")]
    public List<FilterSection> Sections { get; set; } = [];
    [BsonElement("created_at")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime CreatedAt { get; set; }
    [BsonElement("modified_at")]
    [BsonRepresentation(BsonType.DateTime)]
    public DateTime ModifiedAt { get; set; }

    public async Task<string> ToFilterString(ItemsService itemsService)
    {
        string filterStr = "";
        for (int i = (Sections.Count - 1); i >= 0; i--)
        {
            filterStr += await Sections[i].ToFilterString(itemsService);
        }
        return filterStr;
    }
}