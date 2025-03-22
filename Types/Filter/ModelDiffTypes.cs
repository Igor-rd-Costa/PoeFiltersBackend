using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

public class FilterUpdateChanges<TAdded, TDiff>
{
    public List<TAdded>? Added { get; set; } = null;
    public List<string>? Removed { get; set; } = null;
    public List<TDiff>? Changed { get; set; } = null;
}

public class FilterSectionDiff(Guid id) : FilterComponentDiff(id)
{
    [JsonPropertyName("blockChanges")]
    public FilterUpdateChanges<FilterBlock, FilterBlockDiff> BlockChanges { get; set; } = new();

    public new bool HasChanges()
    {
        return (base.HasChanges() || BlockChanges.Added != null
            || BlockChanges.Removed != null || BlockChanges.Changed != null);
    }
}

public class FilterBlockDiff(Guid id) : FilterRulesContainerDiff<IFilterRuleItem, IFilterRuleItemDiff>(id) {}

public class FilterRuleBlockDiff(Guid id) : FilterRulesContainerDiff<FilterRule, FilterRuleDiff>(id), IFilterRuleItemDiff
{
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; } = FilterRuleItemType.RULE_BLOCK;
}

public class FilterRuleDiff(Guid id) : FilterImageComponentDiff(id), IFilterRuleItemDiff
{
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; } = FilterRuleItemType.RULE;

    [BsonIgnoreIfNull]
    [JsonPropertyName("state")]
    public string? State { get; set; } = null;

    [BsonIgnoreIfNull]
    [JsonPropertyName("style")]
    public FilterRuleStyle? Style { get; set; } = null;

    [BsonIgnoreIfNull]
    [JsonPropertyName("items")]
    public ListDiff<string>? Items { get; set; } = null;

    public new bool HasChanges()
    {
        return (base.HasChanges() || State != null || Style != null
            || Items != null);
    }
}