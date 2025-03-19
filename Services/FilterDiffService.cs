

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


            HashSet<string> removeSectionsIds = new HashSet<string>(oldStructure.Sections.Where(
                s => !newStructure.Sections.Select(ns => ns.Id).Contains(s.Id)
            ).Select(os => os.Id.ToString()));
            HashSet<Guid> addSectionsIds = new HashSet<Guid>(newStructure.Sections.Where(
                s => !oldStructure.Sections.Select(os => os.Id).Contains(s.Id)
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
                        newStructure.Sections.Where(ns => addSectionsIds.Contains(ns.Id))
                    );
                }
            }
            for (int i = 0; i < newStructure.Sections.Count; i++)
            {
                FilterSectionStructure newS = newStructure.Sections[i];
                for (int j = 0; j < oldStructure.Sections.Count; j++)
                {
                    FilterSectionStructure oldS = oldStructure.Sections[j];
                    if (newS.Id == oldS.Id)
                    {
                        FilterSectionStructureDiff? sectionDiff = MakeDiff(newS, oldS);
                        if (sectionDiff != null)
                        {
                            if (diff.SectionChanges.Changed == null)
                            {
                                diff.SectionChanges.Changed = [];
                            }
                            diff.SectionChanges.Changed.Add(sectionDiff);
                        }
                        break;
                    }
                }
            }
            return diff;
        });
    }

    public async Task SaveAsync(Game game, FilterStructureDiff diff)
    {
        var collection = (game == Game.POE1) ? m_Context.PoECollections.StructureDiffs : m_Context.PoE2Collections.StructureDiffs;
        await collection.InsertOneAsync(diff);
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

    private void ApplyDiff(FilterSection section, FilterSectionStructureDiff s)
    {
        if (section.Id != s.Id)
        {
            return;
        }
        section.Name = (s.Name == null) ? section.Name : s.Name;
        section.Position = (s.Position == null) ? section.Position : (uint)s.Position;
        
        for (int i = 0; i < (s.BlockChanges.Added?.Count ?? 0); i++)
        {
            section.Blocks.Add(new (s.BlockChanges.Added![i]));
        }

        for (int i = 0; i < (s.BlockChanges.Removed?.Count ?? 0); i++)
        {
            for (int j = 0; j < section.Blocks.Count; j++)
            {
                if (s.BlockChanges.Removed![i] == section.Blocks[j].Id.ToString())
                {
                    section.Blocks.RemoveAt(j);
                    j--;
                    break;
                }
            }
        }

        for (int i = 0; i < (s.BlockChanges.Changed?.Count ?? 0); i++)
        {
            for (int j = 0; j < section.Blocks.Count; j++)
            {
                if (s.BlockChanges.Changed![i].Id == section.Blocks[j].Id)
                {
                    ApplyDiff(section.Blocks[j], s.BlockChanges.Changed![i]);
                }
            }
        }
    }

    private void ApplyDiff(FilterBlock block, FilterBlockStructureDiff b)
    {
        if (block.Id != b.Id)
        {
            return;
        }

        block.Name = (b.Name == null) ? block.Name : b.Name;
        block.ImgSrc = (b.ImgSrc == null) ? block.ImgSrc : b.ImgSrc;
        block.Position = (b.Position == null) ? block.Position : (uint)b.Position;
        block.AllowUserCreatedRules = (b.AllowUserCreatedRules == null) ? block.AllowUserCreatedRules : (bool)b.AllowUserCreatedRules;
        block.RulesType = (b.RulesType == null) ? block.RulesType : (FilterRuleType)b.RulesType;
        
        if (b.AllowedCategories != null)
        {
            for (int i = 0; i < b.AllowedCategories.Removed.Count; i++)
            {
                for (int j = 0; j < block.AllowedCategories.Count; j++)
                {
                    if (block.AllowedCategories[j] == b.AllowedCategories.Removed[i])
                    {
                        block.AllowedCategories.RemoveAt(j);
                        j--;
                        break;
                    }
                }
            }

            block.AllowedCategories.AddRange(b.AllowedCategories.Added);
        }

        for (int i = 0; i < (b.RuleChanges.Removed?.Count ?? 0); i++)
        {
            for (int j = 0; j < block.Rules.Count; j++)
            {
                if (b.RuleChanges.Removed![i] == block.Rules[j].Id.ToString())
                {
                    block.Rules.RemoveAt(j);
                    j--;
                    break;
                }
            }
        }

        for (int i = 0; i < (b.RuleChanges.Added?.Count ?? 0); i++)
        {
            var r = b.RuleChanges.Added![i];
            if (r.Type == FilterRuleItemType.RULE)
            {
                block.Rules.Add(new FilterRule((FilterRuleStructure)r));
            }
            else
            {
                block.Rules.Add(new FilterRuleBlock((FilterRuleBlockStructure)r));
            }
        }

        for (int i = 0; i < (b.RuleChanges.Changed?.Count ?? 0); i++)
        {
            for (int j = 0; j < block.Rules.Count; j++)
            {
                if (block.Rules[j].Id == b.RuleChanges.Changed![i].Id)
                {
                    if (block.Rules[j].Type == FilterRuleItemType.RULE)
                    {
                        ApplyDiff((FilterRule)block.Rules[j], (FilterRuleStructureDiff)b.RuleChanges.Changed[i]);
                    }
                    else
                    {
                        ApplyDiff((FilterRuleBlock)block.Rules[j], (FilterRuleBlockStructureDiff)b.RuleChanges.Changed[i]);
                    }
                }
            }
        }
    }

    private void ApplyDiff(FilterRuleBlock ruleBlock, FilterRuleBlockStructureDiff rb)
    {
        if (ruleBlock.Id != rb.Id)
        {
            return;
        }

        ruleBlock.Name = (rb.Name == null) ? ruleBlock.Name : rb.Name;
        ruleBlock.ImgSrc = (rb.ImgSrc == null) ? ruleBlock.ImgSrc : rb.ImgSrc;
        ruleBlock.Position = (rb.Position == null) ? ruleBlock.Position : (uint)rb.Position;
        ruleBlock.RulesType = (rb.RulesType == null) ? ruleBlock.RulesType : (FilterRuleType)rb.RulesType;
        ruleBlock.AllowUserCreatedRules = (rb.AllowUserCreatedRules == null) ? ruleBlock.AllowUserCreatedRules : (bool)rb.AllowUserCreatedRules;
        
        if (rb.AllowedCategories != null)
        {
            for (int i = 0; i < rb.AllowedCategories.Removed.Count; i++)
            {
                for (int j = 0; j < ruleBlock.AllowedCategories.Count; j++)
                {
                    if (rb.AllowedCategories.Removed[i] == ruleBlock.AllowedCategories[j])
                    {
                        ruleBlock.AllowedCategories.RemoveAt(j);
                        j--;
                        break;
                    }
                }
            }

            for (int i = 0; i < rb.AllowedCategories.Added.Count; i++)
            {
                ruleBlock.AllowedCategories.AddRange(rb.AllowedCategories.Added);
            }
        }

        for (int i = 0; i < (rb.RuleChanges.Removed?.Count ?? 0); i++)
        {
            for (int j = 0; j < ruleBlock.Rules.Count; j++)
            {
                if (rb.RuleChanges.Removed![i] == ruleBlock.Rules[j].Id.ToString())
                {
                    ruleBlock.Rules.RemoveAt(j);
                    j--;
                    break;
                }
            }
        }

        for (int i = 0; i < (rb.RuleChanges.Added?.Count ?? 0); i++)
        {
            ruleBlock.Rules.Add(new (rb.RuleChanges.Added![i]));   
        }

        for (int i = 0; i < (rb.RuleChanges.Changed?.Count ?? 0); i++)
        {
            for (int j = 0; j < ruleBlock.Rules.Count; j++)
            {
                if (ruleBlock.Rules[j].Id == rb.RuleChanges.Changed![i].Id)
                {
                    ApplyDiff(ruleBlock.Rules[j], rb.RuleChanges.Changed[i]);
                }
            }
        }
    }

    private void ApplyDiff(FilterRule rule, FilterRuleStructureDiff r)
    {
        if (rule.Id != r.Id)
        {
            return;
        }

        rule.Name = r.Name ?? rule.Name;
        rule.ImgSrc = r.ImgSrc ?? rule.ImgSrc;
        rule.Position = r.Position ?? rule.Position;
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


    private void ApplyDiff(FilterSectionStructure section, FilterSectionStructureDiff diff)
    {
        section.Name = (diff.Name == null) ? section.Name : diff.Name;
        section.Position = (diff.Position == null) ? section.Position : (uint)diff.Position;

        if (diff.BlockChanges.Added != null)
        {
            section.Blocks.AddRange(diff.BlockChanges.Added);
        }

        for (int i = 0; i < (diff.BlockChanges.Removed?.Count ?? 0); i++)
        {
            var removedId = diff.BlockChanges.Removed![i];
            for (int j = 0; j < section.Blocks.Count; j++)
            {
                if (section.Blocks[j].Id.ToString() == removedId)
                {
                    section.Blocks.RemoveAt(j);
                    break;
                }
            }
        }

        for (int i = 0; i < (diff.BlockChanges.Changed?.Count ?? 0); i++)
        {
            FilterBlockStructureDiff blockDiff = diff.BlockChanges.Changed![i];
            for (int j = 0; j < (diff.BlockChanges.Added?.Count ?? 0); j++)
            {
                if (diff.BlockChanges.Added![j].Id == blockDiff.Id)
                {
                    ApplyDiff(diff.BlockChanges.Added![j], blockDiff);
                    break;
                }
            }
        }
    }

    private void ApplyDiff(FilterBlockStructure block, FilterBlockStructureDiff diff)
    {
        block.Name = (diff.Name == null) ? block.Name : diff.Name;
        block.ImgSrc = (diff.ImgSrc == null) ? block.ImgSrc : diff.ImgSrc;
        block.Position = (diff.Position == null) ? block.Position : (uint)diff.Position;
        block.AllowUserCreatedRules = (diff.AllowUserCreatedRules == null) ? block.AllowUserCreatedRules : (bool)diff.AllowUserCreatedRules;
        block.RulesType = (diff.RulesType == null) ? block.RulesType : (FilterRuleType)diff.RulesType;
        
        if (diff.AllowedCategories != null)
        {
            block.AllowedCategories.AddRange(diff.AllowedCategories.Added);
            
            for (int i = 0; i < diff.AllowedCategories.Removed.Count; i++)
            {
                for (int j = 0; j < block.AllowedCategories.Count; j++)
                {
                    if (diff.AllowedCategories.Removed[i] == block.AllowedCategories[j])
                    {
                        block.AllowedCategories.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        if (diff.RuleChanges.Added != null)
        {
            block.Rules.AddRange(diff.RuleChanges.Added);
        }

        for (int i = 0; i < (diff.RuleChanges.Removed?.Count ?? 0); i++)
        {
            for (int j = 0; j < block.Rules.Count; j++)
            {
                if (block.Rules[j].Id.ToString() == diff.RuleChanges.Removed![i])
                {
                    block.Rules.RemoveAt(j);
                    break;
                }
            }
        }

        for (int i = 0; i < (diff.RuleChanges.Changed?.Count ?? 0); i++)
        {
            for (int j = 0; j < block.Rules.Count; j++)
            {
                if (block.Rules[j].Id == diff.RuleChanges.Changed![i].Id)
                {
                    if (block.Rules[j].Type == FilterRuleItemType.RULE)
                    {
                        ApplyDiff((FilterRuleStructure)block.Rules[j], (FilterRuleStructureDiff)diff.RuleChanges.Changed![i]);
                    }
                    else
                    {
                        ApplyDiff((FilterRuleBlockStructure)block.Rules[j], (FilterRuleBlockStructureDiff)diff.RuleChanges.Changed![i]);
                    }
                    break;
                }
            }
        }
    }

    private void ApplyDiff(FilterRuleBlockStructure ruleBlock, FilterRuleBlockStructureDiff diff)
    {
        ruleBlock.Name = (diff.Name == null) ? ruleBlock.Name : diff.Name;
        ruleBlock.ImgSrc = (diff.ImgSrc == null) ? ruleBlock.ImgSrc : diff.ImgSrc;
        ruleBlock.Position = (diff.Position == null) ? ruleBlock.Position : (uint)diff.Position;
        ruleBlock.RulesType = (diff.RulesType == null) ? ruleBlock.RulesType : (FilterRuleType)diff.RulesType;
        ruleBlock.AllowUserCreatedRules = (diff.AllowUserCreatedRules == null) ? ruleBlock.AllowUserCreatedRules : (bool)diff.AllowUserCreatedRules;
        
        if (diff.AllowedCategories != null)
        {
            ruleBlock.AllowedCategories.AddRange(diff.AllowedCategories.Added);
            for (int i = 0; i < diff.AllowedCategories.Removed.Count; i++)
            {
                for (int j = 0; j < ruleBlock.AllowedCategories.Count; j++)
                {
                    if (ruleBlock.AllowedCategories[j] == diff.AllowedCategories.Removed[i])
                    {
                        ruleBlock.AllowedCategories.RemoveAt(j);
                        break;
                    }
                }
            }
        }

        if (diff.RuleChanges.Added != null)
        {
            ruleBlock.Rules.AddRange(diff.RuleChanges.Added);
        }

        for (int i = 0; i < (diff.RuleChanges.Removed?.Count ?? 0); i++)
        {
            for (int j = 0; j < ruleBlock.Rules.Count; j++)
            {
                if (ruleBlock.Rules[j].Id.ToString() == diff.RuleChanges.Removed![i])
                {
                    ruleBlock.Rules.RemoveAt(j);
                    break;
                }
            }
        }

        for (int i = 0; i < (diff.RuleChanges.Changed?.Count ?? 0); i++)
        {
            for (int j = 0; j < ruleBlock.Rules.Count; j++)
            {
                if (ruleBlock.Rules[j].Id == diff.RuleChanges.Changed![i].Id)
                {
                    ApplyDiff(ruleBlock.Rules[j], diff.RuleChanges.Changed![i]);
                    break;
                }
            }
        }
    }

    private void ApplyDiff(FilterRuleStructure rule, FilterRuleStructureDiff diff)
    {
        if (rule.Id != diff.Id)
        {
            return;
        }

        rule.Name = diff.Name ?? rule.Name;
        rule.ImgSrc = diff.ImgSrc ?? rule.ImgSrc;
        rule.Position = diff.Position ?? rule.Position;
    }

    private FilterSectionStructureDiff? MakeDiff(FilterSectionStructure newS, FilterSectionStructure oldS)
    {
        FilterSectionStructureDiff diff = new(newS.Id);
        if (newS.Name != oldS.Name)
        {
            diff.Name = newS.Name;
        }
        if (newS.Position != oldS.Position)
        {
            diff.Position = newS.Position;
        }

        var addedBlocks = newS.Blocks.Where(b => !oldS.Blocks.Select(ob => ob.Id).Contains(b.Id)).ToList();
        var removedBlocks = oldS.Blocks.Where(b => !newS.Blocks.Select(ns => ns.Id).Contains(b.Id))
            .Select(b => b.Id.ToString()).ToList();
        
        if (addedBlocks.Count > 0)
        {
            diff.BlockChanges.Added = addedBlocks;
        }
        if (removedBlocks.Count > 0)
        {
            diff.BlockChanges.Removed = removedBlocks;
        }

        for (int i = 0; i < newS.Blocks.Count; i++)
        {
            var ns = newS.Blocks[i];
            for (int j = 0; j < oldS.Blocks.Count; j++)
            {
                var os = oldS.Blocks[j];
                if (ns.Id == os.Id)
                {
                    FilterBlockStructureDiff? blockDiff = MakeDiff(ns, os);
                    if (blockDiff != null)
                    {
                        if (diff.BlockChanges.Changed == null)
                        {
                            diff.BlockChanges.Changed = [];
                        }
                        diff.BlockChanges.Changed.Add(blockDiff);
                    }
                    break;
                }
            }
        }

        if (diff.Name != null || diff.Position != null || diff.BlockChanges.Added != null
            || diff.BlockChanges.Removed != null || diff.BlockChanges.Changed != null)
        {
            return diff;
        }

        return null;
    }

    private FilterBlockStructureDiff? MakeDiff(FilterBlockStructure newS, FilterBlockStructure oldS)
    {
        FilterBlockStructureDiff diff = new(newS.Id);

        diff.Name = (newS.Name == oldS.Name) ? null : newS.Name;
        diff.ImgSrc = (newS.ImgSrc == oldS.ImgSrc) ? null : newS.ImgSrc;
        diff.RulesType = (newS.RulesType == oldS.RulesType) ? null : newS.RulesType;
        diff.Position = (newS.Position == oldS.Position) ? null : newS.Position;
        diff.AllowUserCreatedRules = (newS.AllowUserCreatedRules == oldS.AllowUserCreatedRules) ? null : newS.AllowUserCreatedRules;

        var addedCategories = newS.AllowedCategories.Where(ac => !oldS.AllowedCategories.Contains(ac)).ToList();
        var removedCategories = oldS.AllowedCategories.Where(ac => !newS.AllowedCategories.Contains(ac)).ToList();

        if (addedCategories.Count > 0 || addedCategories.Count > 0)
        {
            diff.AllowedCategories = new()
            {
                Added = addedCategories,
                Removed = removedCategories
            };
        }

        var addedRules = newS.Rules.Where(r => !oldS.Rules.Select(or => or.Id).Contains(r.Id)).ToList();
        var removedRules = oldS.Rules.Where(r => !newS.Rules.Select(ns => ns.Id).Contains(r.Id))
            .Select(r => r.Id.ToString()).ToList();

        if (addedRules.Count > 0)
        {
            diff.RuleChanges.Added = addedRules;
        }
        if (removedRules.Count > 0)
        {
            diff.RuleChanges.Removed = removedRules;
        }

        for (int i = 0; i < newS.Rules.Count; i++)
        {
            var ns = newS.Rules[i];
            for (int j = 0; j < oldS.Rules.Count; j++)
            {
                var os = oldS.Rules[j];
                if (ns.Id == os.Id)
                {
                    IFilterRuleStructureDiffItem? ruleDiff = null;
                    if (ns.Type == FilterRuleItemType.RULE)
                    {
                        ruleDiff = MakeDiff((FilterRuleStructure)ns, (FilterRuleStructure)os);
                    }
                    else
                    {
                        ruleDiff = MakeDiff((FilterRuleBlockStructure)ns, (FilterRuleBlockStructure)os);
                    }

                    if (ruleDiff != null)
                    {
                        if (diff.RuleChanges.Changed == null)
                        {
                            diff.RuleChanges.Changed = [];
                        }
                        diff.RuleChanges.Changed.Add(ruleDiff);
                    }
                    break;
                }
            }
        }
        
        if (diff.Name != null || diff.ImgSrc != null || diff.RulesType != null || diff.Position != null
            || diff.AllowedCategories != null || diff.RuleChanges.Added != null || diff.RuleChanges.Removed != null
            || diff.RuleChanges.Changed != null)
        {
            return diff;
        }

        return null;
    }

    private FilterRuleBlockStructureDiff? MakeDiff(FilterRuleBlockStructure newS, FilterRuleBlockStructure oldS)
    {
        FilterRuleBlockStructureDiff diff = new(newS.Id);

        diff.Name = (newS.Name == oldS.Name) ? null : newS.Name;
        diff.ImgSrc = (newS.ImgSrc == oldS.ImgSrc) ? null : newS.ImgSrc;
        diff.Position = (newS.Position == oldS.Position) ? null : newS.Position;
        diff.AllowUserCreatedRules = (newS.AllowUserCreatedRules == oldS.AllowUserCreatedRules) ? null : newS.AllowUserCreatedRules;
        diff.RulesType = (newS.RulesType == oldS.RulesType) ? null : newS.RulesType;

        var addedCategories = newS.AllowedCategories.Where(ac => !oldS.AllowedCategories.Contains(ac)).ToList();
        var removedCategories = oldS.AllowedCategories.Where(ac => !newS.AllowedCategories.Contains(ac)).ToList();

        if (addedCategories.Count > 0 || addedCategories.Count > 0)
        {
            diff.AllowedCategories = new()
            {
                Added = addedCategories,
                Removed = removedCategories
            };
        }

        var addedRules = newS.Rules.Where(r => !oldS.Rules.Select(or => or.Id).Contains(r.Id)).ToList();
        var removedRules = oldS.Rules.Where(r => !newS.Rules.Select(ns => ns.Id).Contains(r.Id))
            .Select(r => r.Id.ToString()).ToList();

        if (addedRules.Count > 0)
        {
            diff.RuleChanges.Added = addedRules;
        }
        if (removedRules.Count > 0)
        {
            diff.RuleChanges.Removed = removedRules;
        }

        for (int i = 0; i < newS.Rules.Count; i++)
        {
            var ns = newS.Rules[i];
            for (int j = 0; j < oldS.Rules.Count; j++)
            {
                var os = oldS.Rules[j];
                if (ns.Id == os.Id)
                {
                    FilterRuleStructureDiff? ruleDiff = MakeDiff(ns, os);
                    
                    if (ruleDiff != null)
                    {
                        if (diff.RuleChanges.Changed == null)
                        {
                            diff.RuleChanges.Changed = [];
                        }
                        diff.RuleChanges.Changed.Add(ruleDiff);
                    }
                    break;
                }
            }
        }

        if (diff.Name != null || diff.ImgSrc != null || diff.RulesType != null || diff.Position != null
            || diff.AllowedCategories != null || diff.RuleChanges.Added != null || diff.RuleChanges.Removed != null
            || diff.RuleChanges.Changed != null)
        {
            return diff;
        }

        return null;
    }

    private FilterRuleStructureDiff? MakeDiff(FilterRuleStructure newS, FilterRuleStructure oldS)
    {
        FilterRuleStructureDiff diff = new(newS.Id);

        diff.Name = (newS.Name == oldS.Name) ? null : newS.Name;
        diff.ImgSrc = (newS.ImgSrc == oldS.ImgSrc) ? null : newS.ImgSrc;
        diff.Position = (newS.Position == oldS.Position) ? null : newS.Position;

        if (diff.Name != null || diff.ImgSrc != null || diff.Position != null)
        {
            return diff;
        }

        return null;
    }
}