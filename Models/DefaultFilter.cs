

using System.Text.Json.Serialization;

public class DefaultFilter : FilterBase
{
    [JsonPropertyName("strictness")]
    public FilterStrictness Strictness { get; set; } = FilterStrictness.REGULAR;
    [JsonPropertyName("sections")]
    public List<FilterSection> Sections { get; set; } = [];
    [JsonPropertyName("structureVersion")]
    public uint StructureVersion { get; set; } = 0;
    [JsonPropertyName("version")]
    public uint Version { get; set; } = 0;
}