

using MongoDB.Bson.Serialization.Attributes;
using System.Drawing;
using System.Text.Json.Serialization;

public interface IPositionable
{
    int Position { get; }
}

public class ColorRGBA
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }
    [JsonPropertyName("r")]
    public short R { get; set; }
    [JsonPropertyName("g")]
    public short G { get; set; }
    [JsonPropertyName("b")]
    public short B { get; set; }
    [JsonPropertyName("a")]
    public float A { get; set; }

    public override string ToString()
    {
        if (Active)
        {
            return $"{R} {G} {B} {A * 255}";
        }
        return "0 0 0 0";
    }
}

public class CreateFilterInfo
{
    public string Name { get; set; } = string.Empty;
}

public class DeleteFilterInfo
{
    public string Id { get; set; } = string.Empty;
}

public class DropPlayEffect
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }
    [JsonPropertyName("temp")]
    public bool Temp { get; set; }
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    public override string ToString()
    {
        if (Active)
        {
            string temp = Temp ? " Temp" : "";
            return $"{Color}{temp}";
        }
        return "None";
    }
}
public class DropIcon
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }
    [JsonPropertyName("size")]
    public int Size { get; set; }
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
    [JsonPropertyName("shape")]
    public string Shape { get; set; } = string.Empty;

    public override string ToString()
    {
        if (Active)
        {
            return $"{Size} {Color} {Shape}";
        }
        return "None";
    }
}

public class DropSound
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }
    [JsonPropertyName("positional")]
    public bool Positional { get; set; }
    [JsonPropertyName("sound")]
    public int Sound { get; set; }
    [JsonPropertyName("volume")]
    public int Volume { get; set; }

    public override string ToString()
    {
        if (Active)
        {
            return $"{Sound} {Volume}";
        }
        return "None";
    }
}

public class FilterRuleStyle
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("fontSize")]
    public int FontSize { get; set; } = 32;
    [JsonPropertyName("textColor")]
    public ColorRGBA TextColor { get; set; } = default!;
    [JsonPropertyName("borderColor")]
    public ColorRGBA BorderColor { get; set; } = default!;
    [JsonPropertyName("backgroundColor")]
    public ColorRGBA BackgroundColor { get; set; } = default!;
    [JsonPropertyName("dropSound")]
    public DropSound DropSound { get; set; } = default!;
    [JsonPropertyName("dropIcon")]
    public DropIcon DropIcon { get; set; } = default!;
    [JsonPropertyName("dropPlayEffect")]
    public DropPlayEffect DropPlayEffect { get; set; } = default!;
}

public class FilterRule
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("imgSrc")]
    public string ImgSrc { get; set; } = string.Empty;
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
    [JsonPropertyName("items")]
    public List<string> Items { get; set; } = [];
    [JsonPropertyName("allowedCategories")]
    public List<string> AllowedCategories { get; set; } = [];
    [JsonPropertyName("style")]
    public FilterRuleStyle Style { get; set; } = default!;
}

public class FilterBlock
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("imgSrc")]
    public string ImgSrc { get; set; } = string.Empty;
    [JsonPropertyName("allowedCategories")]
    public List<string> AllowedCategories { get; set; } = [];
    [JsonPropertyName("rules")]
    public List<FilterRule> Rules { get; set; } = [];
}

public class FilterSection
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("blocks")]
    public List<FilterBlock> Blocks { get; set; } = [];
}

public class FilterRuleInfo : IPositionable
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("imgSrc")]
    public string ImgSrc { get; set; } = string.Empty;
    [JsonPropertyName("position")]
    public int Position { get; set; }
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;
    [JsonPropertyName("items")]
    public List<string> Items { get; set; } = [];
    [JsonPropertyName("allowedCategories")]
    public List<string> AllowedCategories { get; set; } = [];
    [JsonPropertyName("style")]
    public FilterRuleStyle Style { get; set; } = default!;
}


public class FilterBlockInfo : IPositionable
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("imgSrc")]
    public string ImgSrc { get; set; } = string.Empty;
    [JsonPropertyName("position")]
    public int Position { get; set; }
    [JsonPropertyName("allowedCategories")]
    public List<string> AllowedCategories { get; set; } = [];
    [JsonPropertyName("rules")]
    public List<FilterRuleInfo> Rules { get; set; } = [];

}

public class FilterSectionInfo : IPositionable
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("position")]
    public int Position { get; set; }
    [JsonPropertyName("blocks")]
    public List<FilterBlockInfo> Blocks { get; set; } = [];
}

public class FilterInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = default;
    [JsonPropertyName("modified_at")]
    public DateTime ModifiedAt { get; set; } = default;
    [JsonPropertyName("game")]
    public string Game { get; set; } = string.Empty;
    [JsonPropertyName("sections")]
    public List<FilterSectionInfo> Sections { get; set; } = [];
}