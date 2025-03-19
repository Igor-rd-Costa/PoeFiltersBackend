using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text.Json.Serialization;
using System.Text.Json;

// MongoDB Serializers
public class FilterRuleItemSerializer : IBsonSerializer<IFilterRuleItem>
{
    public Type ValueType => typeof(IFilterRuleItem);

    public IFilterRuleItem Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
        var discriminator = (FilterRuleItemType)document["Type"].AsInt32;

        return discriminator switch
        {
            FilterRuleItemType.RULE => BsonSerializer.Deserialize<FilterRule>(document),
            FilterRuleItemType.RULE_BLOCK => BsonSerializer.Deserialize<FilterRuleBlock>(document),
            _ => throw new NotSupportedException($"Unknown discriminator: {discriminator}")
        };
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IFilterRuleItem value)
    {
        BsonSerializer.Serialize(context.Writer, value.GetType(), value);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        BsonSerializer.Serialize(context.Writer, value.GetType(), value);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
        var discriminator = (FilterRuleItemType)document["Type"].AsInt32;

        return discriminator switch
        {
            FilterRuleItemType.RULE => BsonSerializer.Deserialize<FilterRule>(document),
            FilterRuleItemType.RULE_BLOCK => BsonSerializer.Deserialize<FilterRuleBlock>(document),
            _ => throw new NotSupportedException($"Unknown discriminator: {discriminator}")
        };
    }
}

public class FilterRuleStructureItemSerializer : IBsonSerializer<IFilterRuleStructureItem>
{
    public Type ValueType => typeof(IFilterRuleStructureItem);

    public IFilterRuleStructureItem Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
        var discriminator = (FilterRuleItemType)document["Type"].AsInt32;

        return discriminator switch
        {
            FilterRuleItemType.RULE => BsonSerializer.Deserialize<FilterRuleStructure>(document),
            FilterRuleItemType.RULE_BLOCK => BsonSerializer.Deserialize<FilterRuleBlockStructure>(document),
            _ => throw new NotSupportedException($"Unknown discriminator: {discriminator}")
        };
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IFilterRuleStructureItem value)
    {
        BsonSerializer.Serialize(context.Writer, value.GetType(), value);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        BsonSerializer.Serialize(context.Writer, value.GetType(), value);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
        var discriminator = (FilterRuleItemType)document["Type"].AsInt32;

        return discriminator switch
        {
            FilterRuleItemType.RULE => BsonSerializer.Deserialize<FilterRuleStructure>(document),
            FilterRuleItemType.RULE_BLOCK => BsonSerializer.Deserialize<FilterRuleBlockStructure>(document),
            _ => throw new NotSupportedException($"Unknown discriminator: {discriminator}")
        };
    }
}

public class FilterRuleStructureDiffItemSerializer : IBsonSerializer<IFilterRuleStructureDiffItem>
{
    public Type ValueType => typeof(IFilterRuleStructureDiffItem);

    public IFilterRuleStructureDiffItem Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
        var discriminator = (FilterRuleItemType)document["Type"].AsInt32;

        return discriminator switch
        {
            FilterRuleItemType.RULE => BsonSerializer.Deserialize<FilterRuleStructureDiff>(document),
            FilterRuleItemType.RULE_BLOCK => BsonSerializer.Deserialize<FilterRuleBlockStructureDiff>(document),
            _ => throw new NotSupportedException($"Unknown discriminator: {discriminator}")
        };
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IFilterRuleStructureDiffItem value)
    {
        BsonSerializer.Serialize(context.Writer, value.GetType(), value);
    }

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        BsonSerializer.Serialize(context.Writer, value.GetType(), value);
    }

    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var document = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
        var discriminator = (FilterRuleItemType)document["Type"].AsInt32;

        return discriminator switch
        {
            FilterRuleItemType.RULE => BsonSerializer.Deserialize<FilterRuleStructureDiff>(document),
            FilterRuleItemType.RULE_BLOCK => BsonSerializer.Deserialize<FilterRuleBlockStructureDiff>(document),
            _ => throw new NotSupportedException($"Unknown discriminator: {discriminator}")
        };
    }
}



//Json Converters
public class FilterRuleItemConverter : JsonConverter<IFilterRuleItem>
{
    public override IFilterRuleItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;
            var type = (FilterRuleItemType)root.GetProperty("type").GetInt32();

            return type switch
            {
                FilterRuleItemType.RULE => JsonSerializer.Deserialize<FilterRule>(root.GetRawText(), options),
                FilterRuleItemType.RULE_BLOCK => JsonSerializer.Deserialize<FilterRuleBlock>(root.GetRawText(), options),
                _ => throw new JsonException($"Unknown FilterRuleItemType: {type}")
            };
        }
    }

    public override void Write(Utf8JsonWriter writer, IFilterRuleItem value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}

public class FilterRuleStructureItemConverter : JsonConverter<IFilterRuleStructureItem>
{
    public override IFilterRuleStructureItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            var root = doc.RootElement;
            var type = (FilterRuleItemType)root.GetProperty("type").GetInt32();

            return type switch
            {
                FilterRuleItemType.RULE => JsonSerializer.Deserialize<FilterRuleStructure>(root.GetRawText(), options),
                FilterRuleItemType.RULE_BLOCK => JsonSerializer.Deserialize<FilterRuleBlockStructure>(root.GetRawText(), options),
                _ => throw new JsonException($"Unknown FilterRuleItemType: {type}")
            };
        }
    }

    public override void Write(Utf8JsonWriter writer, IFilterRuleStructureItem value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, options);
    }
}