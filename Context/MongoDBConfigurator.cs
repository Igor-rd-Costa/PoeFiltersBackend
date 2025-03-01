using MongoDB.Bson;
using MongoDB.Bson.Serialization;

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