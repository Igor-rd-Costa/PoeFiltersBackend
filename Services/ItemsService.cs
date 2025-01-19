

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PoEFiltersBackend.Models;

public class ItemsService
{
    private readonly MongoDbContext m_Context;

    public ItemsService(MongoDbContext context)
    {
       m_Context = context;
    }

    public async Task<List<Item>> GetItems(Game game, string? category = null, List<string>? ids = null)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItems : m_Context.PoE2Items;
        var filter = Builders<Item>.Filter.Where(i => 
            ((category == null ? true : i.Category == category) && ((ids == null || ids.Count == 0) ? true : ids.Contains(i.Id)))
        );
       return (await collection.FindAsync(filter)).ToList();
    }

    public async Task AddItems(Game game, List<Item> items)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItems : m_Context.PoE2Items;
        await collection.InsertManyAsync(items);
    }

    public async Task DeleteAllItems(Game game)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItems : m_Context.PoE2Items;
        var filter = Builders<Item>.Filter.Where(i => true);
        await collection.DeleteManyAsync(filter);
    }

    public async Task<List<ItemCategory>> GetCategories(Game game, bool includeIgnored = true)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItemCategories : m_Context.PoE2ItemCategories;
        var filter = Builders<ItemCategory>.Filter.Where(c => includeIgnored ? true : c.IgnoreItems == false);
        return (await collection.FindAsync(filter)).ToList();
    }

    public async Task AddCategories(Game game, List<ItemCategory> categories)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItemCategories : m_Context.PoE2ItemCategories;
        await collection.InsertManyAsync(categories);
    }

    public async Task UpdateCategory(Game game, ItemCategory category)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItemCategories : m_Context.PoE2ItemCategories;
        var filter = Builders<ItemCategory>.Filter.Where(c => c.Id == category.Id);
        var update = Builders<ItemCategory>.Update
            .Set(c => c.Name, category.Name)
            .Set(c => c.IgnoreItems, category.IgnoreItems);
        await collection.UpdateOneAsync(filter, update);
    }

    public async Task<List<ItemCategory>> GetIgnoredCategories(Game game)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItemCategories : m_Context.PoE2ItemCategories;
        var filter = Builders<ItemCategory>.Filter.Where(c => c.IgnoreItems == true);
        return (await collection.FindAsync(filter)).ToList();
    }

    public async Task<string?> GetCategoryId(Game game, string categoryName)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItemCategories : m_Context.PoE2ItemCategories;
        var filter = Builders<ItemCategory>.Filter.Where(c => c.Name.ToLower() == categoryName.ToLower());
        return (await collection.FindAsync(filter)).FirstOrDefault()?.Id;
    }
}