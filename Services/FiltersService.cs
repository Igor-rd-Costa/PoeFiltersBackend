using MongoDB.Driver;

public class FiltersService
{
    private readonly ItemsService m_ItemService;
    private readonly MongoDbContext m_Context;

    public FiltersService(ItemsService itemService, MongoDbContext context)
    {
        m_ItemService = itemService;
        m_Context = context;
    }

    public async Task<List<Filter>> GetAsync(Game game, string userId)
    {
        var collection = (game == Game.POE1) ? m_Context.PoECollections.Filters : m_Context.PoE2Collections.Filters;
        return await collection.Find(f => f.User == userId).ToListAsync();
    }

    public async Task<Filter?> GetAsync(Game game, string userId, string id)
    {
        var collection = (game == Game.POE1) ? m_Context.PoECollections.Filters : m_Context.PoE2Collections.Filters;
        return await collection.Find(f => f.Id == id && f.User == userId).FirstOrDefaultAsync();
    }

    public async Task AddAsync(Game game, Filter filter)
    {
        var collection = (game == Game.POE1) ? m_Context.PoECollections.Filters : m_Context.PoE2Collections.Filters;
        await collection.InsertOneAsync(filter);
    }   

    public async Task UpdateAsync(Game game, Filter filter)
    {
        var collection = (game == Game.POE1) ? m_Context.PoECollections.Filters : m_Context.PoE2Collections.Filters;
        var filterDef = Builders<Filter>.Filter.Where(f => f.Id == filter.Id && f.User == filter.User);
        await collection.ReplaceOneAsync(filterDef, filter);
    }
        
    public async Task RemoveAsync(Game game, string userId, string id)
    {
        var collection = (game == Game.POE1) ? m_Context.PoECollections.Filters : m_Context.PoE2Collections.Filters;
        var filter = Builders<Filter>.Filter.Where(f => f.Id == id && f.User == userId);
        await collection.DeleteOneAsync(filter);
    }

    public async Task<string?> GenerateFilter(Game game, string userId, string id)
    {
        Filter? filter = await GetAsync(game, userId, id);
        if (filter == null)
        {
            return null;
        }
        return await ToFilterString(filter);
    }

    public async Task<string> GenerateFilter(Filter filter)
    {
        return await ToFilterString(filter);
    }

    private async Task<string> ToFilterString(Filter filter)
    {
        string filterStr = "";
        for (int i = (filter.Sections.Count - 1); i >= 0; i--)
        {
            filterStr += await ToFilterString(filter.Sections[i]);
        }
        return filterStr;
    }

    private async Task<string> ToFilterString(FilterSection section)
    {
        string sectionStr = $"#\n#Section {section.Id}({section.Name})\n#\n\n";
        for (int i = (section.Blocks.Count - 1); i >= 0; i--)
        {
            sectionStr += await ToFilterString(section.Blocks[i]);
        }
        sectionStr += $"#\n#EndSection {section.Id}({section.Name})\n#\n\n";
        return sectionStr;
    }

    private async Task<string> ToFilterString(FilterBlock block)
    {
        string blockStr = $"#\n#Block {block.Id}({block.Name})\n#\n\n";
        for (int i = (block.Rules.Count - 1); i >= 0; i--)
        {
            if (block.Rules[i].Type == FilterRuleItemType.RULE)
            {
                blockStr += await ToFilterString((FilterRule)block.Rules[i]);
            } 
            else
            {
                blockStr += await ToFilterString((FilterRuleBlock)block.Rules[i]);
            }
        }
        blockStr += $"#\n#EndBlock {block.Id}({block.Name})\n#\n\n";
        return blockStr;
    }

    private async Task<string> ToFilterString(FilterRuleBlock ruleBlock)
    {
        string ruleBlockStr = $"#\n#RuleBlock {ruleBlock.Id}({ruleBlock.Name})\n#\n\n";
        for (int i = (ruleBlock.Rules.Count - 1); i >= 0; i--)
        {
            ruleBlockStr += await ToFilterString((FilterRule)ruleBlock.Rules[i]);
        }
        ruleBlockStr += $"#\n#EndRuleBlock {ruleBlock.Id}({ruleBlock.Name})\n#\n\n";
        return ruleBlockStr;
    }

    private async Task<string> ToFilterString(FilterRule rule)
    {
        if (rule.State == "Disabled")
        {
            return "";
        }
        string ruleStr = $"#Rule {rule.Id}({rule.Name})\n{rule.State}\n";
        if (rule.Items.Count > 0)
        {
            var itemsTask = m_ItemService.GetItems(Game.POE2, null, rule.Items);
            ruleStr += "  BaseType";
            var items = await itemsTask;
            for (int i = 0; i < items.Count; i++)
            {
                ruleStr += $" \"{items[i].Name}\"";
            }
            var style = rule.Style;
            if (style.Id == null)
            {
                ruleStr += $"\n  SetFontSize {Math.Clamp((int)style.FontSize!, 1, 45)}";
                if (style.TextColor != null)
                {
                    ruleStr += $"\n  SetTextColor {style.TextColor}";
                }
                if (style.BorderColor != null)
                {
                    ruleStr += $"\n  SetBorderColor {style.BorderColor}";
                }
                if (style.BackgroundColor != null)
                {
                    ruleStr += $"\n  SetBackgroundColor {style.BackgroundColor}";
                }
            }
            if (style.DropSound != null)
            {
                if (style.DropSound.Positional)
                {
                    ruleStr += $"\n  PlayAlertSoundPositional {style.DropSound}";
                }
                else
                {
                    ruleStr += $"\n  PlayAlertSound {style.DropSound}";
                }
            }
            if (style.DropPlayEffect != null)
            {
                ruleStr += $"\n  PlayEffect {style.DropPlayEffect}";
            }
            if (style.DropIcon != null)
            {
                ruleStr += $"\n  MinimapIcon {style.DropIcon}";
            }

            ruleStr += "\n\n";
        }
        return ruleStr;
    }
}