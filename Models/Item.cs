using MongoDB.Bson.Serialization.Attributes;

namespace PoEFiltersBackend.Models
{
    public class Item
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;
        public string BaseCategory { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = [];
        public string Name { get; set; } = string.Empty;
        public string Rarity { get; set; } = string.Empty;
    }
}
