

using MongoDB.Bson.Serialization.Attributes;
using System.Data;
using System.Drawing;
using System.Text.Json.Serialization;
using static System.Collections.Specialized.BitVector32;

public enum FilterRuleItemType
{
    RULE, RULE_BLOCK
}

public interface IPositionable
{
    [JsonPropertyName("position")]
    int Position { get; }
}

public interface IFilterRuleItem
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }

    public Task<string> ToFilterString(ItemsService itemsService);
}

public interface IFilterRuleItemInfo : IPositionable
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; }
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
            return $"{R} {G} {B} {Math.Round(A * 255)}";
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
        return "-1";
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
        if (Active && Sound > 0 && Sound < 27)
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

[BsonDiscriminator("FilterRule")]
public class FilterRule : IFilterRuleItem
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; }
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

    public async Task<string> ToFilterString(ItemsService itemsService)
    {
        if (State == "Disabled")
        {
            return "";
        }
        string ruleStr = $"#Rule {Id}({Name})\n{State}\n";
        if (Items.Count > 0)
        {
            var itemsTask = itemsService.GetItems(Game.POE2, null, Items);
            ruleStr += "  BaseType";
            var items = await itemsTask;
            for (int i = 0; i < items.Count; i++)
            {
                ruleStr += $" \"{items[i].Name}\"";
            }
            var style = Style;
            ruleStr += $"\n  SetFontSize {Math.Clamp(style.FontSize, 1, 45)}";
            ruleStr += $"\n  SetTextColor {style.TextColor}";
            ruleStr += $"\n  SetBorderColor {style.BorderColor}";
            ruleStr += $"\n  SetBackgroundColor {style.BackgroundColor}";
            if (style.DropSound.Positional)
            {
                ruleStr += $"\n  PlayAlertSoundPositional {style.DropSound}";
            }
            else
            {
                ruleStr += $"\n  PlayAlertSound {style.DropSound}";
            }
            ruleStr += $"\n  PlayEffect {style.DropPlayEffect}";
            ruleStr += $"\n  MinimapIcon {style.DropIcon}";

            ruleStr += "\n\n";
        }
        return ruleStr;
    }
}

[BsonDiscriminator("FilterRuleBlock")]
public class FilterRuleBlock : IFilterRuleItem
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("rules")]
    public List<FilterRule> Rules { get; set; } = [];
    [JsonPropertyName("allowUserCreatedRules")]
    public bool AllowUserCreatedRules { get; set; }

    public async Task<string> ToFilterString(ItemsService itemsService)
    {
        string ruleBlockStr = $"#\n#RuleBlock {Id}({Name})\n#\n\n";
        for (int i = (Rules.Count - 1); i >= 0; i--)
        {
            ruleBlockStr += await Rules[i].ToFilterString(itemsService);
        }
        ruleBlockStr += $"#\n#EndRuleBlock {Id}({Name})\n#\n\n";
        return ruleBlockStr;
    }
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
    public List<IFilterRuleItem> Rules { get; set; } = [];

    public async Task<string> ToFilterString(ItemsService itemsService)
    {
        string blockStr = $"#\n#Block {Id}({Name})\n#\n\n";
        for (int i = (Rules.Count - 1); i >= 0; i--)
        {
            blockStr += await Rules[i].ToFilterString(itemsService);
        }
        blockStr += $"#\n#EndBlock {Id}({Name})\n#\n\n";
        return blockStr;
    }
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
    public async Task<string> ToFilterString(ItemsService itemsService)
    {
        string sectionStr = $"#\n#Section {Id}({Name})\n#\n\n";
        for (int i = (Blocks.Count - 1); i >= 0; i--)
        {
            sectionStr += await Blocks[i].ToFilterString(itemsService);
        }
        sectionStr += $"#\n#EndSection {Id}({Name})\n#\n\n";
        return sectionStr;
    }
}

[BsonDiscriminator("FilterRuleInfo")]
public class FilterRuleInfo : IFilterRuleItemInfo
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; }
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

[BsonDiscriminator("FilterRuleBlockInfo")]
public class FilterRuleBlockInfo : IFilterRuleItemInfo
{
    [BsonGuidRepresentation(MongoDB.Bson.GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("rules")]
    public List<FilterRuleInfo> Rules { get; set; } = [];
    [JsonPropertyName("allowUserCreatedRules")]
    public bool AllowUserCreatedRules { get; set; }
    [JsonPropertyName("position")]
    public int Position { get; set; }
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
    public List<IFilterRuleItemInfo> Rules { get; set; } = [];

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