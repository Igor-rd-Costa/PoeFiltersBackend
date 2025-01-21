

using MongoDB.Bson.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public class FilterRuleItemInfoConverter : JsonConverter<IFilterRuleItemInfo>
{
    public override IFilterRuleItemInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;
            var type = (FilterRuleItemType)root.GetProperty("type").GetInt32();

            switch (type)
            {
                case FilterRuleItemType.RULE: return JsonSerializer.Deserialize<FilterRuleInfo>(root.GetRawText(), options);
                case FilterRuleItemType.RULE_BLOCK: return JsonSerializer.Deserialize<FilterRuleBlockInfo>(root.GetRawText(), options);
                default: throw new JsonException($"Unknown FilterRuleItemType: {type}");
            };
        }
    }

    public override void Write(Utf8JsonWriter writer, IFilterRuleItemInfo value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}

public class FilterRuleItemConverter : JsonConverter<IFilterRuleItem>
{
    public override IFilterRuleItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;
            var type = (FilterRuleItemType)root.GetProperty("type").GetInt32();

            switch (type)
            {
                case FilterRuleItemType.RULE: return JsonSerializer.Deserialize<FilterRule>(root.GetRawText(), options);
                case FilterRuleItemType.RULE_BLOCK: return JsonSerializer.Deserialize<FilterRuleBlock>(root.GetRawText(), options);
                default: throw new JsonException($"Unknown FilterRuleItemType: {type}");
            };
        }
    }

    public override void Write(Utf8JsonWriter writer, IFilterRuleItem value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}