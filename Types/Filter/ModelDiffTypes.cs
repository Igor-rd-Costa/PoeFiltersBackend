using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

public class FilterUpdateChanges<TAdded, TDiff>
{
    public List<TAdded>? Added { get; set; } = null;
    public List<string>? Removed { get; set; } = null;
    public List<TDiff>? Changed { get; set; } = null;
}

public class FilterSectionDiff(Guid id) : FilterNullableImageComponent(id)
{
    [JsonPropertyName("blockChanges")]
    public FilterUpdateChanges<FilterBlock, FilterBlockDiff> BlockChanges { get; set; } = new();
}

public class FilterBlockDiff(Guid id) : FilterDiffRulesContainer<IFilterRuleItem, IFilterRuleDiffItem>(id) {}

public class FilterRuleBlockDiff(Guid id) : FilterDiffRulesContainer<FilterRule, FilterRuleDiff>(id), IFilterRuleDiffItem
{
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; } = FilterRuleItemType.RULE_BLOCK;
}

public class FilterRuleDiff(Guid id) : FilterNullableImageComponent(id), IFilterRuleDiffItem
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
    public List<string>? Items { get; set; } = null;
}