

using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

public class User
{

    [JsonPropertyName("id")]
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("providerId")]
    public int ProviderId { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } = false;

}