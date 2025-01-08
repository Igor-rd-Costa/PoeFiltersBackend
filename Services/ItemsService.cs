

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PoEFiltersBackend.Models;

public class ItemsService
{
    IMongoCollection<ItemCategory> m_PoEItemCategoryCollection;
    IMongoCollection<ItemCategory> m_PoE2ItemCategoryCollection;

    IMongoCollection<Item> m_PoEItemCollection;
    IMongoCollection<Item> m_PoE2ItemCollection;

    public ItemsService(IOptions<MongoDbConfig> config)
    {
        var client = new MongoClient(
        config.Value.ConnectionString);
        var mongoDb = client.GetDatabase(config.Value.DatabaseName);
        m_PoEItemCategoryCollection = mongoDb.GetCollection<ItemCategory>(
            config.Value.PoEData.ItemCategoryCollectionName);
        m_PoE2ItemCategoryCollection = mongoDb.GetCollection<ItemCategory>(
            config.Value.PoE2Data.ItemCategoryCollectionName);
        m_PoEItemCollection = mongoDb.GetCollection<Item>(
            config.Value.PoEData.ItemCollectionName);
        m_PoE2ItemCollection = mongoDb.GetCollection<Item>(
            config.Value.PoE2Data.ItemCollectionName);
    }

    public async Task<List<Item>> GetItems(Game game, string? category = null, List<string>? ids = null)
    {
        var collection = game == Game.POE1 ? m_PoEItemCollection : m_PoE2ItemCollection;
        var filter = Builders<Item>.Filter.Where(i => 
            ((category == null ? true : i.Category == category) && ((ids == null || ids.Count == 0) ? true : ids.Contains(i.Id)))
        );
       return (await collection.FindAsync(filter)).ToList();
    }

    public async Task AddItems(Game game, List<Item> items)
    {
        var collection = game == Game.POE1 ? m_PoEItemCollection : m_PoE2ItemCollection;
        await collection.InsertManyAsync(items);
    }

    public async Task DeleteAllItems(Game game)
    {
        var collection = game == Game.POE1 ? m_PoEItemCollection : m_PoE2ItemCollection;
        var filter = Builders<Item>.Filter.Where(i => true);
        await collection.DeleteManyAsync(filter);
    }

    public async Task<List<ItemCategory>> GetCategories(Game game, bool includeIgnored = true)
    {
        var collection = game == Game.POE1 ? m_PoEItemCategoryCollection : m_PoE2ItemCategoryCollection;
        var filter = Builders<ItemCategory>.Filter.Where(c => includeIgnored ? true : c.IgnoreItems == false);
        return (await collection.FindAsync(filter)).ToList();
    }

    public async Task AddCategories(Game game, List<ItemCategory> categories)
    {
        var collection = game == Game.POE1 ? m_PoEItemCategoryCollection : m_PoE2ItemCategoryCollection;
        await collection.InsertManyAsync(categories);
    }

    public async Task UpdateCategory(Game game, ItemCategory category)
    {
        var collection = game == Game.POE1 ? m_PoEItemCategoryCollection : m_PoE2ItemCategoryCollection;
        var filter = Builders<ItemCategory>.Filter.Where(c => c.Id == category.Id);
        var update = Builders<ItemCategory>.Update
            .Set(c => c.Name, category.Name)
            .Set(c => c.IgnoreItems, category.IgnoreItems);
        await collection.UpdateOneAsync(filter, update);
    }

    public async Task<List<ItemCategory>> GetIgnoredCategories(Game game)
    {
        var collection = game == Game.POE1 ? m_PoEItemCategoryCollection : m_PoE2ItemCategoryCollection;
        var filter = Builders<ItemCategory>.Filter.Where(c => c.IgnoreItems == true);
        return (await collection.FindAsync(filter)).ToList();
    }

    public async Task<string?> GetCategoryId(Game game, string categoryName)
    {
        var collection = game == Game.POE1 ? m_PoEItemCategoryCollection : m_PoE2ItemCategoryCollection;
        var filter = Builders<ItemCategory>.Filter.Where(c => c.Name.ToLower() == categoryName.ToLower());
        return (await collection.FindAsync(filter)).FirstOrDefault()?.Id;
    }
}