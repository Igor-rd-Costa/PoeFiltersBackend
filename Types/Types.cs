
using System.Text.Json.Serialization;

public enum Game
{
    POE1, POE2
}

public class AddBaseItemCategoryInfo
{
    [JsonPropertyName("categoryName")]
    public string Name { get; set; } = string.Empty;
}

public class AddItemCategoryInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; } = string.Empty;
}
