

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

public class FiltersService
{
    private readonly IMongoCollection<Filter> m_FiltersCollection;
    private readonly ItemsService m_ItemService;

    public FiltersService(IOptions<MongoDbConfig> config, ItemsService itemService)
    {
        var client = new MongoClient(
            config.Value.ConnectionString);
        var mongoDb = client.GetDatabase(config.Value.DatabaseName);
        m_FiltersCollection = mongoDb.GetCollection<Filter>(
            config.Value.FiltersCollectionName);
        m_ItemService = itemService;
    }

    public async Task<List<Filter>> GetAsync() =>
        await m_FiltersCollection.Find(_ => true).ToListAsync();

    public async Task<Filter?> GetAsync(string id) =>
        await m_FiltersCollection.Find(f => f.Id == id).FirstOrDefaultAsync();

    public async Task AddAsync(Filter filter)
    {
        await m_FiltersCollection.InsertOneAsync(filter);
    }

    public async Task UpdateAsync(Filter filter)
    {
        var filterDef = Builders<Filter>.Filter.Where(f => f.Id == filter.Id);
        await m_FiltersCollection.ReplaceOneAsync(filterDef, filter);
    }

    public async Task RemoveAsync(string id)
    {
        var filter = Builders<Filter>.Filter.Eq(f => f.Id, id);
        await m_FiltersCollection.DeleteOneAsync(filter);
    }

    public async Task<string?> GenerateFilter(string id)
    {
        Filter? filter = await GetAsync(id);
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
                    filterStr += "\n";
                }
                filterStr += "\n";
            }
            filterStr += "\n";
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
            //TODO Add FontSize
            ruleStr += "\n  SetFontSize 32";
            var style = rule.Style;
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