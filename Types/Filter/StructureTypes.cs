

using System.Text.Json.Serialization;

public class FilterSectionStructure : FilterComponent
{
    [JsonPropertyName("blocks")]
    public List<FilterBlockStructure> Blocks { get; set; } = [];
}

public class FilterBlockStructure : FilterRulesContainer<IFilterRuleStructureItem> { }

public class FilterRuleBlockStructure : FilterRulesContainer<FilterRuleStructure>, IFilterRuleStructureItem
{
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; } = FilterRuleItemType.RULE_BLOCK;
}

public class FilterRuleStructure : FilterImageComponent, IFilterRuleStructureItem
{
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; } = FilterRuleItemType.RULE;
}