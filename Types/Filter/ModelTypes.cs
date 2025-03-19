using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;


public class FilterSection : FilterComponent
{
    public FilterSection() { }

    public FilterSection(FilterSectionStructure s) 
    {
        Id = s.Id;
        Name = s.Name;
        Blocks = [];
        Position = s.Position;
        for (int i = 0; i < s.Blocks.Count; i++)
        {
            Blocks.Add(new FilterBlock(s.Blocks[i]));
        }

    }

    [JsonPropertyName("blocks")]
    public List<FilterBlock> Blocks { get; set; } = [];
}

public class FilterBlock : FilterRulesContainer<IFilterRuleItem> 
{
    public FilterBlock() { }

    public FilterBlock(FilterBlockStructure s)
    {
        Id = s.Id;
        Name = s.Name;
        ImgSrc = s.ImgSrc;
        AllowedCategories = s.AllowedCategories;
        AllowUserCreatedRules = s.AllowUserCreatedRules;
        RulesType = s.RulesType;
        Rules = [];
        for (int i = 0; i < s.Rules.Count; i++)
        {
            if (s.Rules[i].Type == FilterRuleItemType.RULE)
            {
                Rules.Add(new FilterRule((FilterRuleStructure)s.Rules[i]));
            }
            else
            {
                Rules.Add(new FilterRuleBlock((FilterRuleBlockStructure)s.Rules[i]));
            }
        }
    }
}

public class FilterRuleBlock : FilterRulesContainer<FilterRule>, IFilterRuleItem 
{
    public FilterRuleBlock() { }

    public FilterRuleBlock(FilterRuleBlockStructure s)
    {
        Id = s.Id;
        Name = s.Name;
        ImgSrc = s.ImgSrc;
        AllowedCategories = s.AllowedCategories;
        AllowUserCreatedRules = s.AllowUserCreatedRules;
        RulesType = s.RulesType;
        Rules = [];
        for (int i = 0; i < s.Rules.Count; i++)
        {
            Rules.Add(new FilterRule(s.Rules[i]));
        }
    }

    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; } = FilterRuleItemType.RULE_BLOCK;
}

public class FilterRule : FilterImageComponent, IFilterRuleItem
{
    public FilterRule() { }

    public FilterRule(FilterRuleStructure s)
    {
        Id = s.Id;
        Name = s.Name;
        ImgSrc = s.ImgSrc;
    }

    [JsonPropertyName("type")]
    public FilterRuleItemType Type { get; set; } = FilterRuleItemType.RULE;
    [JsonPropertyName("state")]
    public string State { get; set; } = "Disabled";

    [JsonPropertyName("style")]
    public FilterRuleStyle Style { get; set; } = FilterHelpers.DefaultRuleStyle();

    [JsonPropertyName("items")]
    public List<string> Items { get; set; } = [];
}