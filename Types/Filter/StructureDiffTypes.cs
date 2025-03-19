

using System.Text.Json.Serialization;

public class FilterSectionStructureDiff(Guid id) : FilterNullableComponent(id)
{
    public FilterUpdateChanges<FilterBlockStructure, FilterBlockStructureDiff> BlockChanges { get; set; } = new();
}
public class FilterBlockStructureDiff(Guid id) : FilterDiffRulesContainer<IFilterRuleStructureItem, IFilterRuleStructureDiffItem>(id) { }

public class FilterRuleBlockStructureDiff(Guid id) : FilterDiffRulesContainer<FilterRuleStructure, FilterRuleStructureDiff>(id), IFilterRuleStructureDiffItem 
{
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; } = FilterRuleItemType.RULE_BLOCK;
}

public class FilterRuleStructureDiff(Guid id) : FilterNullableImageComponent(id), IFilterRuleStructureDiffItem
{
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; } = FilterRuleItemType.RULE;
}