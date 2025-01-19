using Microsoft.Extensions.Options;
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

    public async Task<List<Filter>> GetAsync(string userId) =>
        await m_Context.Filters.Find(f => f.User == userId).ToListAsync();

    public async Task<Filter?> GetAsync(string userId, string id) =>
        await m_Context.Filters.Find(f => f.Id == id && f.User == userId).FirstOrDefaultAsync();

    public async Task AddAsync(Filter filter)
    {
        await m_Context.Filters.InsertOneAsync(filter);
    }

    public async Task UpdateAsync(Filter filter)
    {
        var filterDef = Builders<Filter>.Filter.Where(f => f.Id == filter.Id);
        await m_Context.Filters.ReplaceOneAsync(filterDef, filter);
    }

    public async Task RemoveAsync(string userId, string id)
    {
        var filter = Builders<Filter>.Filter.Where(f => f.Id == id && f.User == userId);
        await m_Context.Filters.DeleteOneAsync(filter);
    }

    public async Task<string?> GenerateFilter(string userId, string id)
    {
        Filter? filter = await GetAsync(userId, id);
        if (filter == null)
        {
            return null;
        }
        string filterStr = "";
        for (int i = 0; i < filter.Sections.Count; i++)
        {
            var section = filter.Sections[i];
            filterStr += $"#\n#Section {section.Id}({section.Name})\n#\n\n";
            for (int j = 0; j < section.Blocks.Count; j++)
            {
                var block = section.Blocks[j];
                filterStr += $"#\n#Block {block.Id}({block.Name})\n#\n\n";
                for (int k = 0; k < block.Rules.Count; k++)
                {
                    var rule = block.Rules[k];
                    if (rule.State == "Disabled")
                    {
                        continue;
                    }
                    filterStr += $"#Rule {rule.Id}({rule.Name})\n";
                    filterStr += await RuleToRuleString(rule);
                    filterStr += "\n\n";
                }
                filterStr += "\n\n";
            }
            filterStr += "\n\n";
        }
        return filterStr;
    }

    private async Task<string> RuleToRuleString(FilterRule rule)
    {
        if (rule.State == "Disabled")
        {
            return "";
        }
        string ruleStr = $"{rule.State}\n";
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
            ruleStr += $"\n  SetFontSize {Math.Clamp(style.FontSize, 1, 45)}";
            ruleStr += $"\n  SetTextColor {style.TextColor}";
            ruleStr += $"\n  SetBorderColor {style.BorderColor}";
            ruleStr += $"\n  SetBackgroundColor {style.BackgroundColor}";
            if (style.DropSound.Positional)
            {
                ruleStr += $"\n  PlayAlertSoundPositional {style.DropSound}";
            }
            else
            {
                ruleStr += $"\n  PlayAlertSound {style.DropSound}";
            }
            ruleStr += $"\n  PlayEffect {style.DropPlayEffect}";
            ruleStr += $"\n  MinimapIcon {style.DropIcon}";
        }
        return ruleStr;
    }
}