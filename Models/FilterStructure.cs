using System.Text.Json.Serialization;

public class FilterStructure : FilterBase
{
    [JsonPropertyName("game")]
    public Game Game { get; set; }
    [JsonPropertyName("sections")]
    public List<FilterSectionStructure> Sections { get; set; } = [];
    [JsonPropertyName("structureVersion")]
    public uint StructureVersion { get; set; } = 0;

    public List<FilterSection> MakeFilter()
    {
        List<FilterSection> sections = [];
        for (uint i = 0; i < Sections.Count; i++)
        {
            FilterSectionStructure s = Sections[(int)i];
            FilterSection section = new()
            {
                Id = s.Id,
                Name = s.Name,
                Blocks = [],
                Position = i
            };
            for (uint j = 0; j < s.Blocks.Count; j++)
            {
                FilterBlockStructure b = s.Blocks[(int)j];
                FilterBlock block = new()
                {
                    Id = b.Id,
                    Name = b.Name,
                    ImgSrc = b.ImgSrc,
                    AllowedCategories = [],
                    AllowUserCreatedRules = false,
                    RulesType = b.RulesType,
                    Rules = [],
                    Position = j
                };

                for (uint k = 0; k < b.Rules.Count; k++)
                {
                    if (b.Rules[(int)k].Type == FilterRuleItemType.RULE_BLOCK)
                    {
                        FilterRuleBlockStructure rb = (FilterRuleBlockStructure)b.Rules[(int)k];
                        FilterRuleBlock ruleBlock = new()
                        {
                            Id = rb.Id,
                            Type = FilterRuleItemType.RULE_BLOCK,
                            Name = rb.Name,
                            AllowedCategories = rb.AllowedCategories,
                            AllowUserCreatedRules = rb.AllowUserCreatedRules,
                            RulesType = rb.RulesType,
                            Rules = [],
                            Position = k
                        };
                        for (uint l = 0; l < rb.Rules.Count; l++)
                        {
                            FilterRuleStructure r = rb.Rules[(int)l];
                            FilterRule rule = new()
                            {
                                Id = r.Id,
                                Type = FilterRuleItemType.RULE,
                                Name = r.Name,
                                ImgSrc = r.ImgSrc,
                                State = "Disabled",
                                Style = FilterHelpers.DefaultRuleStyle(),
                                Items = [],
                                Position = l
                            };
                            ruleBlock.Rules.Add(rule);
                        }
                        block.Rules.Add(ruleBlock);
                    } 
                    else
                    {
                        FilterRuleStructure r = (FilterRuleStructure)b.Rules[(int)k];
                        FilterRule rule = new()
                        {
                            Id = r.Id,
                            Type = FilterRuleItemType.RULE,
                            Name = r.Name,
                            ImgSrc = r.ImgSrc,
                            State = "Disabled",
                            Style = FilterHelpers.DefaultRuleStyle(),
                            Items = [],
                            Position = k
                        };
                        block.Rules.Add(rule);
                    }
                }
                section.Blocks.Add(block);
            }
            sections.Add(section);
        }
        return sections;
    }
}