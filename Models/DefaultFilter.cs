

using System.Text.Json.Serialization;

public class DefaultFilter : FilterBase
{
    [JsonPropertyName("strictness")]
    public FilterStrictness Strictness { get; set; } = FilterStrictness.REGULAR;
    [JsonPropertyName("sections")]
    public List<FilterSection> Sections { get; set; } = [];
    [JsonPropertyName("structureVersion")]
    public uint StructureVersion { get; set; } = 0;
    [JsonPropertyName("defaultVersion")]
    public uint DefaultVersion { get; set; } = 0;
}