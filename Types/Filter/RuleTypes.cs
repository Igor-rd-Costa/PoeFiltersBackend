using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

public class ColorRGBA
{
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
        return $"{R} {G} {B} {Math.Round(A * 255)}";
    }
}

public class DropPlayEffect
{
    [JsonPropertyName("temp")]
    public bool Temp { get; set; }
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    public override string ToString()
    {
        string temp = Temp ? " Temp" : "";
        return $"{Color}{temp}";
    }
}
public class DropIcon
{
    [JsonPropertyName("size")]
    public int Size { get; set; }
    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
    [JsonPropertyName("shape")]
    public string Shape { get; set; } = string.Empty;

    public override string ToString()
    {
       return $"{Size} {Color} {Shape}";
    }
}

public class DropSound
{
    [JsonPropertyName("positional")]
    public bool Positional { get; set; }
    [JsonPropertyName("sound")]
    public int Sound { get; set; }
    [JsonPropertyName("volume")]
    public int Volume { get; set; }

    public override string ToString()
    {
        if (Sound > 0 && Sound < 27)
        {
            string soundStr = Sound <= 16 ? Sound.ToString() : GetShaperSoundStr();
            return $"{soundStr} {Volume}";
        }
        return "None";
    }

    private string GetShaperSoundStr()
    {
        return Sound switch
        {
            17 => "ShAlchemy",
            18 => "ShBlessed",
            19 => "ShChaos",
            20 => "ShDivine",
            21 => "ShExalted",
            22 => "ShFusing",
            23 => "ShGeneral",
            24 => "ShMirror",
            25 => "ShRegal",
            26 => "ShVaal",
            _ => "None"
        };
    }
}

public class FilterRuleStyle
{
    [BsonIgnoreIfNull]
    [JsonPropertyName("id")]
    public Guid? Id { get; set; } = null;

    [JsonPropertyName("fontSize")]
    [BsonIgnoreIfNull]
    public int? FontSize { get; set; } = 32;

    [BsonIgnoreIfNull]
    [JsonPropertyName("textColor")]
    public ColorRGBA? TextColor { get; set; } = null;

    [BsonIgnoreIfNull]
    [JsonPropertyName("borderColor")]
    public ColorRGBA? BorderColor { get; set; } = null;

    [BsonIgnoreIfNull]
    [JsonPropertyName("backgroundColor")]
    public ColorRGBA? BackgroundColor { get; set; } = null;

    [BsonIgnoreIfNull]
    [JsonPropertyName("dropSound")]
    public DropSound? DropSound { get; set; } = null;

    [BsonIgnoreIfNull]
    [JsonPropertyName("dropIcon")]
    public DropIcon? DropIcon { get; set; } = null;

    [BsonIgnoreIfNull]
    [JsonPropertyName("dropPlayEffect")]
    public DropPlayEffect? DropPlayEffect { get; set; } = null;
}