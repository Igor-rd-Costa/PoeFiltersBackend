using MongoDB.Bson;
using MongoDB.Driver;

public class FilterDiffService
{
    private readonly MongoDbContext m_Context;

    public FilterDiffService(MongoDbContext context)
    {
        m_Context = context;
    }

    public Task<FilterStructureDiff> MakeDiffAsync(FilterStructure oldStructure, FilterStructure newStructure)
    {
        return Task<FilterStructureDiff>.Factory.StartNew(() =>
        {
            FilterStructureDiff diff = new(newStructure.StructureVersion);

            var oldSectionIds = oldStructure.Sections.Select(s => s.Id);
            var newSectionIds = newStructure.Sections.Select(s => s.Id);

            var addedSections = newStructure.Sections.Where(s => !oldSectionIds.Contains(s.Id))
                .ToList();
            var removedSectionIds = oldSectionIds.Where(s => !newSectionIds.Contains(s))
                .Select(s => s.ToString())
                .ToList();

            diff.SectionChanges.Added = (addedSections.Count == 0) ? null : addedSections;
            diff.SectionChanges.Removed = (removedSectionIds.Count == 0) ? null : removedSectionIds;
            
            for (int i = 0; i < oldStructure.Sections.Count; i++)
            {
                var oldSection = oldStructure.Sections[i];
                for (int j = 0; j < oldStructure.Sections.Count; j++)
                {
                    var newSection = newStructure.Sections[j];
                    if (oldSection.Id == newSection.Id)
                    {
                        FilterSectionStructureDiff sectionDiff = new(newSection.Id); 
                        MakeDiff(oldSection, newSection, sectionDiff);
                        if (sectionDiff.HasChanges())
                        {
                            diff.SectionChanges.Changed ??= [];
                            diff.SectionChanges.Changed.Add(sectionDiff);
                        }
                        break;
                    }
                }
            }
            return diff;
        });
    }

    public Task<FilterDiff> MakeDiffAsync(DefaultFilter oldFilter, DefaultFilter newFilter)
    {
        return Task.Factory.StartNew(() =>
        {
            FilterDiff diff = new(newFilter.Version + 1);

            HashSet<string> removeSectionsIds = new HashSet<string>(oldFilter.Sections.Where(
               s => !newFilter.Sections.Select(ns => ns.Id).Contains(s.Id)
           ).Select(os => os.Id.ToString()));
            HashSet<Guid> addSectionsIds = new HashSet<Guid>(newFilter.Sections.Where(
                s => !oldFilter.Sections.Select(os => os.Id).Contains(s.Id)
            ).Select(ns => ns.Id));

            if (removeSectionsIds.Count > 0 || addSectionsIds.Count > 0)
            {
                diff.SectionChanges = new();
                if (removeSectionsIds.Count > 0)
                {
                    diff.SectionChanges.Removed = [];
                    diff.SectionChanges.Removed.AddRange(removeSectionsIds);
                }
                if (addSectionsIds.Count > 0)
                {
                    diff.SectionChanges.Added = [];
                    diff.SectionChanges.Added.AddRange(
                        newFilter.Sections.Where(ns => addSectionsIds.Contains(ns.Id))
                    );
                }
            }
            for (int i = 0; i < oldFilter.Sections.Count; i++)
            {
                FilterSection oldSection = oldFilter.Sections[i];
                for (int j = 0; j < newFilter.Sections.Count; j++)
                {
                    FilterSection newSection = newFilter.Sections[j];
                    if (oldSection.Id == newSection.Id)
                    {
                        FilterSectionDiff sectionDiff = new(newSection.Id);
                        MakeDiff(oldSection, newSection, sectionDiff);
                        if (sectionDiff.HasChanges())
                        {
                            diff.SectionChanges.Changed ??= [];
                            diff.SectionChanges.Changed.Add(sectionDiff);
                        }
                        break;
                    }
                }
            }

            return diff;
        });
    }

    public Task ApplyDiffAsync(DefaultFilter filter, FilterStructureDiff diff)
    {
        return Task.Factory.StartNew(() =>
        {
            for (int i = 0; i < (diff.SectionChanges.Added?.Count ?? 0); i++)
            {
                FilterSection s = new FilterSection(diff.SectionChanges.Added![i]);
                filter.Sections.Add(s);
            }

            for (int i = 0; i < (diff.SectionChanges.Removed?.Count ?? 0); i++)
            {
                for (int j = 0; j < filter.Sections.Count; j++)
                {
                    if (diff.SectionChanges.Removed![i] == filter.Sections[j].Id.ToString())
                    {
                        filter.Sections.RemoveAt(j);
                        j--;
                        break;
                    }
                }
            }

            for (int i = 0; i < (diff.SectionChanges.Changed?.Count ?? 0); i++)
            {
                for (int j = 0; j < filter.Sections.Count; j++)
                {
                    if (diff.SectionChanges.Changed![i].Id == filter.Sections[j].Id)
                    {
                        ApplyDiff(filter.Sections[j], diff.SectionChanges.Changed[i]);
                    }
                }
            }
        });
    }

    public Task ApplyDiffAsync(Filter filter, FilterDiff diff)
    {
        return Task.Factory.StartNew(() =>
        {

        });
    }

    public async Task SaveAsync(Game game, FilterStructureDiff diff)
    {
        var collection = (game == Game.POE1) ? m_Context.PoECollections.StructureDiffs : m_Context.PoE2Collections.StructureDiffs;
        await collection.InsertOneAsync(diff);
    }

    public async Task<FilterStructureDiff?> GetStructureDiffAsync(Game game, uint currentVersion)
    {
        var collection = (game == Game.POE1) ? m_Context.PoECollections.StructureDiffs : m_Context.PoE2Collections.StructureDiffs;
        var filter = Builders<FilterStructureDiff>.Filter.Where(f => f.Version > currentVersion);
        List<FilterStructureDiff> diffs = await collection.Find(filter).ToListAsync();

        if (diffs.Count == 0)
        {
            return null;
        }

        return MergeDiffs(diffs);
    }

    public FilterStructureDiff MergeDiffs(List<FilterStructureDiff> diffs)
    {
        diffs.Sort((FilterStructureDiff a, FilterStructureDiff b) =>
        {
            if (a.Version < b.Version)
            {
                return -1;
            }
            return 1;
        });


        FilterStructureDiff diff = new(diffs[diffs.Count - 1].Version);
        for (int i = 0; i < diffs.Count; i++)
        {
            FilterStructureDiff d = diffs[i];
            if (d.SectionChanges.Added != null)
            {
                if (diff.SectionChanges.Added == null)
                {
                    diff.SectionChanges.Added = [];
                }
                diff.SectionChanges.Added.AddRange(d.SectionChanges.Added);
            }

            for (int j = 0; j < (d.SectionChanges.Removed?.Count ?? 0); j++)
            {
                string removedSectionId = d.SectionChanges.Removed![j];
                bool found = false;
                for (int k = 0; k < (diff.SectionChanges.Added?.Count ?? 0); k++)
                {
                    FilterSectionStructure s = diff.SectionChanges.Added![k];
                    if (s.Id.ToString() == removedSectionId)
                    {
                        diff.SectionChanges.Added.Remove(s);
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    if (diff.SectionChanges.Removed == null)
                    {
                        diff.SectionChanges.Removed = [];
                    }
                    diff.SectionChanges.Removed.Add(removedSectionId);
                }
            }

            for (int j = 0; j < (d.SectionChanges.Changed?.Count ?? 0); j++)
            {
                FilterSectionStructureDiff sd = d.SectionChanges.Changed![j];
                bool found = false;
                for (int k = 0; k < (diff.SectionChanges.Added?.Count ?? 0); k++)
                {
                    var diffSection = diff.SectionChanges.Added![k];
                    if (diffSection.Id == sd.Id)
                    {
                        ApplyDiff(diffSection, sd);
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    if (diff.SectionChanges.Changed == null)
                    {
                        diff.SectionChanges.Changed = [];
                    }
                    diff.SectionChanges.Changed.Add(sd);
                }
            }
        }

        return diff;
    }




    // Make Diff Functions
    private void MakeDiff(FilterComponent oldComponent, FilterComponent newComponent, FilterComponentDiff outDiff)
    {
        outDiff.Name = oldComponent.Name.Equals(newComponent.Name) ? null : newComponent.Name;
        outDiff.Position = (oldComponent.Position == newComponent.Position) ? null : newComponent.Position;
    }

    private void MakeDiff(FilterImageComponent oldComponent, FilterImageComponent newComponent, 
        FilterImageComponentDiff outDiff)
    {
        MakeDiff((FilterComponent)oldComponent, (FilterComponent)newComponent, (FilterComponentDiff)outDiff);
        outDiff.ImgSrc = (oldComponent.ImgSrc.Equals(newComponent.ImgSrc)) ? null : newComponent.ImgSrc; 
    }

    private void MakeDiff(FilterRulesContainer<IFilterRuleItem> oldContainer, 
        FilterRulesContainer<IFilterRuleItem> newContainer,
        FilterRulesContainerDiff<IFilterRuleItem, IFilterRuleItemDiff> outDiff)
    {
        MakeDiff((FilterImageComponent)oldContainer, (FilterImageComponent)newContainer, 
            (FilterImageComponentDiff)outDiff);
        outDiff.RulesType = (oldContainer.RulesType == newContainer.RulesType) 
            ? null : newContainer.RulesType;
        outDiff.AllowUserCreatedRules = (oldContainer.AllowUserCreatedRules == newContainer.AllowUserCreatedRules)
            ? null : newContainer.AllowUserCreatedRules;

        List<string> addedCategories = new(
            newContainer.AllowedCategories.Where(c => !oldContainer.AllowedCategories.Contains(c))
        );
        List<string> removedCategories = new(
            oldContainer.AllowedCategories.Where(c => !newContainer.AllowedCategories.Contains(c))
        );
        if (addedCategories.Count != 0 || removedCategories.Count != 0)
        {
            outDiff.AllowedCategories = new()
            {
                Added = addedCategories,
                Removed = removedCategories
            };
        }

        var newContainerRuleItemIds = newContainer.Rules.Select(r => r.Id);
        var oldContainerRuleItemIds = oldContainer.Rules.Select(r => r.Id);
        List<IFilterRuleItem> addedRuleItems = newContainer.Rules.Where(r => !oldContainerRuleItemIds.Contains(r.Id))
            .ToList();
        List<string> removedRuleItemIds = oldContainer.Rules.Where(r => !newContainerRuleItemIds.Contains(r.Id))
            .Select(r => r.Id.ToString())
            .ToList();

        outDiff.RuleChanges.Added = (addedRuleItems.Count == 0) ? null : addedRuleItems;
        outDiff.RuleChanges.Removed = (removedRuleItemIds.Count == 0) ? null : removedRuleItemIds;

        for (int i = 0; i < oldContainer.Rules.Count; i++)
        {
            var oldRuleItem = oldContainer.Rules[i];
            for (int j = 0; j < newContainer.Rules.Count; j++)
            {
                var newRuleItem = newContainer.Rules[j];
                if (oldRuleItem.Id == newRuleItem.Id)
                {
                    if (newRuleItem.Type == FilterRuleItemType.RULE)
                    {
                        FilterRuleDiff ruleDiff = new(newRuleItem.Id);
                        MakeDiff((FilterRule)oldRuleItem, (FilterRule)newRuleItem, ruleDiff);
                        if (ruleDiff.HasChanges())
                        {
                            outDiff.RuleChanges.Changed ??= [];
                            outDiff.RuleChanges.Changed.Add(ruleDiff);
                        }
                        break;
                    }
                    FilterRuleBlockDiff ruleBlockDiff = new(newRuleItem.Id);
                    MakeDiff((FilterRuleBlock)oldRuleItem, (FilterRuleBlock)newRuleItem, ruleBlockDiff);
                    if (ruleBlockDiff.HasChanges())
                    {
                        outDiff.RuleChanges.Changed ??= [];
                        outDiff.RuleChanges.Changed.Add(ruleBlockDiff);
                    }
                    break;
                }
            }
        }
    }

    private void MakeDiff(FilterRulesContainer<FilterRule> oldContainer,
        FilterRulesContainer<FilterRule> newContainer,
        FilterRulesContainerDiff<FilterRule, FilterRuleDiff> outDiff)
    {
        MakeDiff((FilterImageComponent)oldContainer, (FilterImageComponent)newContainer,
            (FilterImageComponentDiff)outDiff);
        outDiff.RulesType = (oldContainer.RulesType == newContainer.RulesType)
            ? null : newContainer.RulesType;
        outDiff.AllowUserCreatedRules = (oldContainer.AllowUserCreatedRules == newContainer.AllowUserCreatedRules)
            ? null : newContainer.AllowUserCreatedRules;

        List<string> addedCategories = new(
            newContainer.AllowedCategories.Where(c => !oldContainer.AllowedCategories.Contains(c))
        );
        List<string> removedCategories = new(
            oldContainer.AllowedCategories.Where(c => !newContainer.AllowedCategories.Contains(c))
        );
        if (addedCategories.Count != 0 || removedCategories.Count != 0)
        {
            outDiff.AllowedCategories = new()
            {
                Added = addedCategories,
                Removed = removedCategories
            };
        }

        var newContainerRuleItemIds = newContainer.Rules.Select(r => r.Id);
        var oldContainerRuleItemIds = oldContainer.Rules.Select(r => r.Id);
        List<FilterRule> addedRuleItems = newContainer.Rules.Where(r => !oldContainerRuleItemIds.Contains(r.Id))
            .ToList();
        List<string> removedRuleItemIds = oldContainer.Rules.Where(r => !newContainerRuleItemIds.Contains(r.Id))
            .Select(r => r.Id.ToString())
            .ToList();

        outDiff.RuleChanges.Added = (addedRuleItems.Count == 0) ? null : addedRuleItems;
        outDiff.RuleChanges.Removed = (removedRuleItemIds.Count == 0) ? null : removedRuleItemIds;

        for (int i = 0; i < oldContainer.Rules.Count; i++)
        {
            var oldRuleItem = oldContainer.Rules[i];
            for (int j = 0; j < newContainer.Rules.Count; j++)
            {
                var newRuleItem = newContainer.Rules[j];
                if (oldRuleItem.Id == newRuleItem.Id)
                {
                    FilterRuleDiff ruleDiff = new(newRuleItem.Id);
                    MakeDiff(oldRuleItem, newRuleItem, ruleDiff);
                    if (ruleDiff.HasChanges())
                    {
                        outDiff.RuleChanges.Changed ??= [];
                        outDiff.RuleChanges.Changed.Add(ruleDiff);
                    }
                    break;
                }
            }
        }
    }

    private void MakeDiff(FilterSection oldSection, FilterSection newSection, FilterSectionDiff outDiff)
    {
        MakeDiff((FilterComponent)oldSection, (FilterComponent)newSection, (FilterComponentDiff)outDiff);

        var oldBlocksIds = oldSection.Blocks.Select(b => b.Id);
        var newBlocksIds = newSection.Blocks.Select(b => b.Id);
        List<FilterBlock> addedBlocks = newSection.Blocks.Where(b => !oldBlocksIds.Contains(b.Id)).ToList();
        List<string> removedBlocksIds = oldSection.Blocks.Where(b => !newBlocksIds.Contains(b.Id))
            .Select(b => b.Id.ToString()).ToList();
        outDiff.BlockChanges.Added = (addedBlocks.Count == 0) ? null : addedBlocks;
        outDiff.BlockChanges.Removed = (removedBlocksIds.Count == 0) ? null : removedBlocksIds;

        for (int i = 0; i < oldSection.Blocks.Count; i++)
        {
            var oldBlock = oldSection.Blocks[i];
            for (int j = 0; j < newSection.Blocks.Count; j++)
            {
                var newBlock = newSection.Blocks[j];
                if (oldBlock.Id == newBlock.Id)
                {
                    FilterBlockDiff blockDiff = new(newSection.Id);
                    MakeDiff(oldBlock, newBlock, blockDiff);
                    if (blockDiff.HasChanges())
                    {
                        outDiff.BlockChanges.Changed ??= [];
                        outDiff.BlockChanges.Changed.Add(blockDiff);
                    }
                    break;
                }
            }
        }
    }

    private void MakeDiff(FilterBlock oldBlock, FilterBlock newBlock, FilterBlockDiff outDiff)
    {
        if (oldBlock.Id != newBlock.Id)
        {
            return;
        }

        MakeDiff((FilterRulesContainer<IFilterRuleItem>)oldBlock, (FilterRulesContainer<IFilterRuleItem>)newBlock, 
            (FilterRulesContainerDiff<IFilterRuleItem, IFilterRuleItemDiff>)outDiff);
    }

    private void MakeDiff(FilterRuleBlock oldRuleBlock, FilterRuleBlock newRuleBlock, FilterRuleBlockDiff outDiff)
    {
        if (oldRuleBlock.Id != newRuleBlock.Id)
        {
            return;
        }

        MakeDiff((FilterRulesContainer<FilterRule>)oldRuleBlock, (FilterRulesContainer<FilterRule>)newRuleBlock,
            (FilterRulesContainerDiff<FilterRule, FilterRuleDiff>)outDiff);
    }

    private void MakeDiff(FilterRule oldRule, FilterRule newRule, FilterRuleDiff outDiff)
    {
        if (newRule.Id != oldRule.Id)
        {
            return;
        }

        MakeDiff((FilterImageComponent)oldRule, (FilterImageComponent)newRule, 
            (FilterImageComponentDiff)outDiff);

        outDiff.State = (oldRule.State == newRule.State) ? null : newRule.State;
        outDiff.Style = (oldRule.Style == newRule.Style) ? null : newRule.Style;

        var addedItems = newRule.Items.Where(i => !oldRule.Items.Contains(i)).ToList();
        var removedItems = oldRule.Items.Where(i => !newRule.Items.Contains(i)).ToList();

        if (addedItems.Count != 0 || removedItems.Count != 0)
        {
            outDiff.Items = new()
            {
                Added = addedItems,
                Removed = removedItems
            };
        }
    }

    private void MakeDiff(FilterRulesContainer<IFilterRuleStructureItem> oldContainer, FilterRulesContainer<IFilterRuleStructureItem> newContainer,
        FilterRulesContainerDiff<IFilterRuleStructureItem, IFilterRuleStructureItemDiff> outDiff)
    {
        MakeDiff((FilterImageComponent)oldContainer, (FilterImageComponent)newContainer,
            (FilterImageComponentDiff)outDiff);

        outDiff.RulesType = (oldContainer.RulesType == newContainer.RulesType) 
            ? null : newContainer.RulesType;
        outDiff.AllowUserCreatedRules = (oldContainer.AllowUserCreatedRules == newContainer.AllowUserCreatedRules) 
            ? null : newContainer.AllowUserCreatedRules;

        var addedCategories = newContainer.AllowedCategories.Where(c => !oldContainer.AllowedCategories.Contains(c)).ToList();
        var removedCategories = oldContainer.AllowedCategories.Where(c => !newContainer.AllowedCategories.Contains(c)).ToList();
        if (addedCategories.Count != 0 || removedCategories.Count != 0)
        {
            outDiff.AllowedCategories = new()
            {
                Added = addedCategories,
                Removed = removedCategories
            };
        }

        var newRulesIds = newContainer.Rules.Select(r => r.Id);
        var oldRuleIds = oldContainer.Rules.Select(r => r.Id);

        var addedRules = newContainer.Rules.Where(r => !oldRuleIds.Contains(r.Id))
            .ToList();
        var removedRuleIds = oldRuleIds.Where(r => !newRulesIds.Contains(r))
            .Select(r => r.ToString())
            .ToList();

        outDiff.RuleChanges.Added = (addedRules.Count == 0) ? null : addedRules;
        outDiff.RuleChanges.Removed = (removedRuleIds.Count == 0) ? null : removedRuleIds;

        for (int i = 0; i < oldContainer.Rules.Count; i++)
        {
            var oldRuleItem = oldContainer.Rules[i];
            for (int j = 0; j < newContainer.Rules.Count; j++)
            {
                var newRuleItem = newContainer.Rules[j];
                if (oldRuleItem.Id == newRuleItem.Id)
                {
                    if (newRuleItem.Type == FilterRuleItemType.RULE)
                    {
                        FilterRuleStructureDiff ruleDiff = new(newRuleItem.Id);
                        MakeDiff((FilterRuleStructure)oldRuleItem, (FilterRuleStructure)newRuleItem,
                            (FilterRuleStructureDiff)ruleDiff);
                        if (ruleDiff.HasChanges())
                        {
                            outDiff.RuleChanges.Changed ??= [];
                            outDiff.RuleChanges.Changed.Add(ruleDiff);
                        }
                        break;
                    }
                    FilterRuleBlockStructureDiff ruleBlockDiff = new(newRuleItem.Id);
                    MakeDiff((FilterRuleBlockStructure)oldRuleItem, (FilterRuleBlockStructure)newRuleItem,
                        (FilterRuleBlockStructureDiff)ruleBlockDiff);
                    if (ruleBlockDiff.HasChanges())
                    {
                        outDiff.RuleChanges.Changed ??= [];
                        outDiff.RuleChanges.Changed.Add(ruleBlockDiff);
                    }
                    break;
                }
            }
        }
    }

    private void MakeDiff(FilterRulesContainer<FilterRuleStructure> oldRuleContainer, FilterRulesContainer<FilterRuleStructure> newRuleContainer,
            FilterRulesContainerDiff<FilterRuleStructure, FilterRuleStructureDiff> outDiff)
    {
        MakeDiff((FilterImageComponent)oldRuleContainer, (FilterImageComponent)newRuleContainer,
            (FilterImageComponentDiff)outDiff);

        outDiff.RulesType = (oldRuleContainer.RulesType == newRuleContainer.RulesType) 
            ? null : newRuleContainer.RulesType;
        outDiff.AllowUserCreatedRules = (oldRuleContainer.AllowUserCreatedRules == newRuleContainer.AllowUserCreatedRules) 
            ? null : newRuleContainer.AllowUserCreatedRules;

        var addedCategories = newRuleContainer.AllowedCategories.Where(c => !oldRuleContainer.AllowedCategories.Contains(c)).ToList();
        var removedCategories = oldRuleContainer.AllowedCategories.Where(c => !newRuleContainer.AllowedCategories.Contains(c)).ToList();

        if (addedCategories.Count != 0 || removedCategories.Count != 0)
        {
            outDiff.AllowedCategories = new()
            {
                Added = addedCategories,
                Removed = removedCategories
            };
        }

        var newRulesIds = newRuleContainer.Rules.Select(r => r.Id);
        var oldRuleIds = oldRuleContainer.Rules.Select(r => r.Id);

        var addedRules = newRuleContainer.Rules.Where(r => !oldRuleIds.Contains(r.Id))
            .ToList();
        var removedRuleIds = oldRuleIds.Where(r => !newRulesIds.Contains(r))
            .Select(r => r.ToString())
            .ToList();

        outDiff.RuleChanges.Added = (addedRules.Count == 0) ? null : addedRules;
        outDiff.RuleChanges.Removed = (removedRuleIds.Count == 0) ? null : removedRuleIds;

        for (int i = 0; i < oldRuleContainer.Rules.Count; i++)
        {
            var oldRule = oldRuleContainer.Rules[i];
            for (int j = 0; j < newRuleContainer.Rules.Count; j++)
            {
                var newRule = newRuleContainer.Rules[j];
                if (newRule.Id == oldRule.Id)
                {
                    FilterRuleStructureDiff ruleDiff = new(newRule.Id);
                    MakeDiff(oldRule, newRule, ruleDiff);
                    if (ruleDiff.HasChanges())
                    {
                        outDiff.RuleChanges.Changed ??= [];
                        outDiff.RuleChanges.Changed.Add(ruleDiff);
                    }
                    break;
                }
            }
        }
    }


    private void MakeDiff(FilterSectionStructure oldSection, FilterSectionStructure newSection, 
        FilterSectionStructureDiff outDiff)
    {
        if (newSection.Id != oldSection.Id)
        {
            return;
        }

        MakeDiff((FilterComponent)oldSection, (FilterComponent)newSection, 
            (FilterComponentDiff)outDiff);

        var oldBlockIds = oldSection.Blocks.Select(b => b.Id);
        var newBlockIds = newSection.Blocks.Select(b => b.Id);

        var addedBlocks = newSection.Blocks.Where(b => !oldBlockIds.Contains(b.Id))
            .ToList();
        var removedBlockIds = oldBlockIds.Where(b => !newBlockIds.Contains(b))
            .Select(b => b.ToString())
            .ToList();

        outDiff.BlockChanges.Added = (addedBlocks.Count == 0) ? null : addedBlocks;
        outDiff.BlockChanges.Removed = (removedBlockIds.Count == 0) ? null : removedBlockIds;

        for (int i = 0; i < oldSection.Blocks.Count; i++)
        {
            var oldBlock = oldSection.Blocks[i];
            for (int j = 0; j < newSection.Blocks.Count; j++)
            {
                var newBlock = newSection.Blocks[j];
                if (oldBlock.Id == newBlock.Id)
                {
                    FilterBlockStructureDiff blockDiff = new(newBlock.Id);
                    MakeDiff(oldBlock, newBlock, blockDiff);
                    if (blockDiff.HasChanges())
                    {
                        outDiff.BlockChanges.Changed ??= [];
                        outDiff.BlockChanges.Changed.Add(blockDiff);
                    }
                    break;
                }
            }
        }
    }

    private void MakeDiff(FilterBlockStructure oldBlock, FilterBlockStructure newBlock, 
        FilterBlockStructureDiff outDiff)
    {
        if (oldBlock.Id != newBlock.Id)
        {
            return;
        }

        MakeDiff((FilterRulesContainer<IFilterRuleStructureItem>)oldBlock,
            (FilterRulesContainer<IFilterRuleStructureItem>)newBlock,
            (FilterRulesContainerDiff<IFilterRuleStructureItem, IFilterRuleStructureItemDiff>)outDiff);
    }

    private void MakeDiff(FilterRuleBlockStructure oldRuleBlock, FilterRuleBlockStructure newRuleBlock,
        FilterRuleBlockStructureDiff outDiff)
    {
        if (oldRuleBlock.Id != newRuleBlock.Id)
        {
            return;
        }

        MakeDiff((FilterRulesContainer<FilterRuleStructure>)oldRuleBlock, (FilterRulesContainer<FilterRuleStructure>)newRuleBlock,
            (FilterRulesContainerDiff<FilterRuleStructure, FilterRuleStructureDiff>)outDiff);
    }

    private void MakeDiff(FilterRuleStructure newRule, FilterRuleStructure oldRule,
        FilterRuleStructureDiff outDiff)
    {
        if (newRule.Id != oldRule.Id)
        {
            return;
        }

        MakeDiff((FilterImageComponent)oldRule, (FilterImageComponent)newRule,
            (FilterImageComponentDiff)outDiff);
    }

    

    // Apply Diff Functions
    private void ApplyDiff(FilterComponent component, FilterComponentDiff diff)
    {
        component.Name = diff.Name ?? component.Name;
        component.Position = diff.Position ?? component.Position;
    }

    private void ApplyDiff(FilterImageComponent component, FilterImageComponentDiff diff)
    {
        ApplyDiff((FilterComponent)component, (FilterComponentDiff)diff);
        component.ImgSrc = diff.ImgSrc ?? component.ImgSrc;
    }

    private void ApplyDiff(FilterRulesContainer<IFilterRuleItem> container, 
        FilterRulesContainerDiff<IFilterRuleStructureItem, IFilterRuleStructureItemDiff> diff)
    {
        ApplyDiff((FilterImageComponent)container, (FilterImageComponentDiff)diff);
        
        container.RulesType = diff.RulesType ?? container.RulesType;
        container.AllowUserCreatedRules = diff.AllowUserCreatedRules ?? container.AllowUserCreatedRules;

        if (diff.AllowedCategories != null)
        {
            for (int i = 0; i < diff.AllowedCategories.Removed.Count; i++)
            {
                for (int j = 0; j < container.AllowedCategories.Count; j++)
                {
                    if (diff.AllowedCategories.Removed[i] == container.AllowedCategories[j])
                    {
                        container.AllowedCategories.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        for (int i = 0; i < container.Rules.Count; i++)
        {
            var ruleItem = container.Rules[i];
            for (int j = 0; j < (diff.RuleChanges.Changed?.Count ?? 0); j++)
            {
                var ruleItemDiff = diff.RuleChanges.Changed![j];
                if (ruleItem.Id == ruleItemDiff.Id)
                {
                    if (ruleItem.Type == FilterRuleItemType.RULE)
                    {
                        ApplyDiff((FilterRule)ruleItem, (FilterRuleStructureDiff)ruleItemDiff);
                        break;
                    }
                    ApplyDiff((FilterRuleBlock)ruleItem, (FilterRuleBlockStructureDiff)ruleItemDiff);
                }
            }
        }

        if (diff.RuleChanges.Added != null)
        {
            container.Rules.AddRange(diff.RuleChanges.Added.Select<IFilterRuleStructureItem, IFilterRuleItem>(r =>
            {
                if (r.Type == FilterRuleItemType.RULE)
                {
                    return new FilterRule((FilterRuleStructure)r);
                }
                return new FilterRuleBlock((FilterRuleBlockStructure)r);
            }));
        }
    }

    private void ApplyDiff(FilterRulesContainer<FilterRule> container,
            FilterRulesContainerDiff<FilterRuleStructure, FilterRuleStructureDiff> diff)
    {
        ApplyDiff((FilterImageComponent)container, (FilterImageComponentDiff)diff);

        container.RulesType = diff.RulesType ?? container.RulesType;
        container.AllowUserCreatedRules = diff.AllowUserCreatedRules ?? container.AllowUserCreatedRules;

        if (diff.AllowedCategories != null)
        {
            for (int i = 0; i < diff.AllowedCategories.Removed.Count; i++)
            {
                for (int j = 0; j < container.AllowedCategories.Count; j++)
                {
                    if (container.AllowedCategories[j] == diff.AllowedCategories.Removed[i])
                    {
                        container.AllowedCategories.RemoveAt(j);
                        break;
                    }
                }
            }

            container.AllowedCategories.AddRange(diff.AllowedCategories.Added);
        }

        for (int i = 0; i < (diff.RuleChanges.Removed?.Count ?? 0); i++)
        {
            var removedRuleId = diff.RuleChanges.Removed![i];
            for (int j = 0; j < container.Rules.Count; j++)
            {
                if (container.Rules[j].Id.ToString() == removedRuleId)
                {
                    container.Rules.RemoveAt(j);
                    break;
                }
            }
        }

        for (int i = 0; i < (diff.RuleChanges.Changed?.Count ?? 0); i++)
        {
            var ruleDiff = diff.RuleChanges.Changed![i];
            for (int j = 0; j < container.Rules.Count; ++j)
            {
                var rule = container.Rules[j];
                if (rule.Id == ruleDiff.Id)
                {
                    ApplyDiff(rule, ruleDiff);
                    break;
                }
            }
        }

        if (diff.RuleChanges.Added != null)
        {
            container.Rules.AddRange(diff.RuleChanges.Added.Select(r => new FilterRule(r)));
        }
    }

    private void ApplyDiff(FilterSection section, FilterSectionStructureDiff diff)
    {
        if (section.Id != diff.Id)
        {
            return;
        }

        ApplyDiff((FilterComponent)section, (FilterComponentDiff)diff);

        for (int i = 0; i < (diff.BlockChanges.Removed?.Count ?? 0); i++)
        {
            var removedBlockId = diff.BlockChanges.Removed![i];
            for (int j = 0; j < section.Blocks.Count; j++)
            {
                if (section.Blocks[j].Id.ToString() == removedBlockId)
                {
                    section.Blocks.RemoveAt(j);
                    break;
                }
            }
        }
        
        for (int i = 0; i < (diff.BlockChanges.Changed?.Count ?? 0); i++)
        {
            var blockDiff = diff.BlockChanges.Changed![i];
            for (int j = 0; j < section.Blocks.Count; j++)
            {
                var block = section.Blocks[j];
                if (block.Id == blockDiff.Id)
                {
                    ApplyDiff(block, blockDiff);
                    break;
                }
            }
        }

        if (diff.BlockChanges.Added != null)
        {
            section.Blocks.AddRange(diff.BlockChanges.Added.Select(b => new FilterBlock(b)));
        }
    }

    private void ApplyDiff(FilterBlock block, FilterBlockStructureDiff diff)
    {
        if (block.Id != diff.Id)
        {
            return;
        }

        ApplyDiff((FilterRulesContainer<IFilterRuleItem>)block,
            (FilterRulesContainerDiff<IFilterRuleStructureItem, IFilterRuleStructureItemDiff>)diff);
    }

    private void ApplyDiff(FilterRuleBlock ruleBlock, FilterRuleBlockStructureDiff diff)
    {
        if (ruleBlock.Id != diff.Id)
        {
            return;
        }

        ApplyDiff((FilterRulesContainer<FilterRule>)ruleBlock,
            (FilterRulesContainerDiff<FilterRuleStructure, FilterRuleStructureDiff>)diff);
    }

    private void ApplyDiff(FilterRule rule, FilterRuleStructureDiff diff)
    {
        if (rule.Id != diff.Id)
        {
            return;
        }
        
        ApplyDiff((FilterImageComponent)rule, (FilterImageComponentDiff)diff);
    }

    private void ApplyDiff(FilterRulesContainer<IFilterRuleStructureItem> container,
            FilterRulesContainerDiff<IFilterRuleStructureItem, IFilterRuleStructureItemDiff> diff)
    {
        ApplyDiff((FilterImageComponent)container, (FilterImageComponentDiff)diff);

        container.RulesType = diff.RulesType ?? container.RulesType;
        container.AllowUserCreatedRules = diff.AllowUserCreatedRules ?? container.AllowUserCreatedRules;
        
        if (diff.AllowedCategories != null)
        {
            for (int i = 0; i < diff.AllowedCategories.Removed.Count; i++)
            {
                var removedRuleItemId = diff.AllowedCategories.Removed[i];
                for (int j = 0; j < container.AllowedCategories.Count; j++)
                {
                    var category = container.AllowedCategories[j];
                    if (category == removedRuleItemId)
                    {
                        container.AllowedCategories.RemoveAt(j);
                        break;
                    }
                }
            }

            container.AllowedCategories.AddRange(diff.AllowedCategories.Added);
        }

        for (int i = 0; i < (diff.RuleChanges.Removed?.Count ?? 0); i++)
        {
            var removedRuleItemId = diff.RuleChanges.Removed![i];
            for (int j = 0; j < container.Rules.Count; j++)
            {
                var ruleItem = container.Rules[j];
                if (ruleItem.Id.ToString() == removedRuleItemId)
                {
                    container.Rules.RemoveAt(j);
                    break;
                }
            }
        }

        for (int i = 0; i < (diff.RuleChanges.Changed?.Count ?? 0); i++)
        {
            var ruleItemDiff = diff.RuleChanges.Changed![i];
            for (int j = 0; j < container.Rules.Count; j++)
            {
                var ruleItem = container.Rules[j];
                if (ruleItem.Id == ruleItemDiff.Id)
                {
                    if (ruleItem.Type == FilterRuleItemType.RULE)
                    {
                        ApplyDiff((FilterRuleStructure)ruleItem, (FilterRuleStructureDiff)ruleItemDiff);
                        break;
                    }
                    ApplyDiff((FilterRuleBlockStructure)ruleItem, (FilterRuleBlockStructureDiff)ruleItemDiff);
                    break;
                }
            }
        }

        if (diff.RuleChanges.Added != null)
        {
            container.Rules.AddRange(diff.RuleChanges.Added);
        }
    }

    private void ApplyDiff(FilterRulesContainer<FilterRuleStructure> container,
            FilterRulesContainerDiff<FilterRuleStructure, FilterRuleStructureDiff> diff)
    {
        ApplyDiff((FilterImageComponent)container, (FilterImageComponentDiff)diff);

        container.RulesType = diff.RulesType ?? container.RulesType;
        container.AllowUserCreatedRules = diff.AllowUserCreatedRules ?? container.AllowUserCreatedRules;
        
        if (diff.AllowedCategories != null)
        {
            for (int i = 0; i < diff.AllowedCategories.Removed.Count; i++)
            {
                var removedCategoryId = diff.AllowedCategories.Removed[i];
                for (int j = 0; j < container.AllowedCategories.Count; j++)
                {
                    var category = container.AllowedCategories[j];
                    if (category == removedCategoryId)
                    {
                        container.AllowedCategories.RemoveAt(j);
                        break;
                    }
                }

                container.AllowedCategories.AddRange(diff.AllowedCategories.Added);
            }
        }

        for (int i = 0; i < (diff.RuleChanges.Removed?.Count ?? 0); i++)
        {
            var removedRuleId = diff.RuleChanges.Removed![i];
            for (int j = 0; j < container.Rules.Count; j++)
            {
                var rule = container.Rules[j];
                if (rule.Id.ToString() == removedRuleId)
                {
                    container.Rules.RemoveAt(j);
                    break;
                }
            }
        }

        for (int i = 0; i < (diff.RuleChanges.Changed?.Count ?? 0); i++)
        {
            var ruleDiff = diff.RuleChanges.Changed![i];
            for (int j = 0; j < container.Rules.Count; j++)
            {
                var rule = container.Rules[j];
                if (rule.Id == ruleDiff.Id)
                {
                    ApplyDiff(rule, ruleDiff);
                    break;
                }
            }
        }

        if (diff.RuleChanges.Added != null)
        {
            container.Rules.AddRange(diff.RuleChanges.Added);
        }
    }

    private void ApplyDiff(FilterSectionStructure section, FilterSectionStructureDiff diff)
    {
        if (section.Id != diff.Id)
        {
            return;
        }

        ApplyDiff((FilterComponent)section, (FilterComponentDiff)diff);

        for (int i = 0; i < (diff.BlockChanges.Removed?.Count ?? 0); i++)
        {
            var removedBlockId = diff.BlockChanges.Removed![i];
            for (int j = 0; j < section.Blocks.Count; j++)
            {
                var block = section.Blocks[j];
                if (block.Id.ToString() == removedBlockId)
                {
                    section.Blocks.RemoveAt(j);
                    break;
                }
            }
        }

        for (int i = 0; i < (diff.BlockChanges.Changed?.Count ?? 0); i++)
        {
            var blockDiff = diff.BlockChanges.Changed![i];
            for (int j = 0; j < section.Blocks.Count; j++)
            {
                var block = section.Blocks[j];
                if (block.Id == blockDiff.Id)
                {
                    ApplyDiff(block, blockDiff);
                    break;
                }
            }
        }
    }

    private void ApplyDiff(FilterBlockStructure block, FilterBlockStructureDiff diff)
    {
        if (block.Id != diff.Id)
        {
            return;
        }

        ApplyDiff((FilterRulesContainer<IFilterRuleStructureItem>)block, 
            (FilterRulesContainerDiff<IFilterRuleStructureItem, IFilterRuleStructureItemDiff>)diff);
    }

    private void ApplyDiff(FilterRuleBlockStructure ruleBlock, FilterRuleBlockStructureDiff diff)
    {
        if (ruleBlock.Id != diff.Id)
        {
            return;
        }

        ApplyDiff((FilterRulesContainer<FilterRuleStructure>)ruleBlock, 
            (FilterRulesContainerDiff<FilterRuleStructure, FilterRuleStructureDiff>)diff);
    }

    private void ApplyDiff(FilterRuleStructure rule, FilterRuleStructureDiff diff)
    {
        if (rule.Id != diff.Id)
        {
            return;
        }

        ApplyDiff((FilterImageComponent)rule, (FilterImageComponentDiff)diff);
    }
}