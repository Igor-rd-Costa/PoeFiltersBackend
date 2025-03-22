

using System.Text.Json.Serialization;

public class FilterSectionStructureDiff(Guid id) : FilterComponentDiff(id)
{
    public FilterUpdateChanges<FilterBlockStructure, FilterBlockStructureDiff> BlockChanges { get; set; } = new();
}
public class FilterBlockStructureDiff(Guid id) : FilterRulesContainerDiff<IFilterRuleStructureItem, IFilterRuleStructureItemDiff>(id) { }

public class FilterRuleBlockStructureDiff(Guid id) : FilterRulesContainerDiff<FilterRuleStructure, FilterRuleStructureDiff>(id), IFilterRuleStructureItemDiff
{
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; } = FilterRuleItemType.RULE_BLOCK;
}

public class FilterRuleStructureDiff(Guid id) : FilterImageComponentDiff(id), IFilterRuleStructureItemDiff
{
    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; } = FilterRuleItemType.RULE;
}