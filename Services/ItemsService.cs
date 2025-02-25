

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

    public async Task<Item?> GetItem(Game game, string id)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItems : m_Context.PoE2Items;
        var filter = Builders<Item>.Filter.Where(i => i.Id == id);
        return (await collection.FindAsync(filter)).FirstOrDefault();
    }

    public async Task<List<Item>> GetItems(Game game, string? category = null, List<string>? ids = null)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItems : m_Context.PoE2Items;
        var filter = Builders<Item>.Filter.Where(i => 
            ((category == null ? true : i.BaseCategory == category) && ((ids == null || ids.Count == 0) ? true : ids.Contains(i.Id)))
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

    public async Task<ItemCategory> GetItemCategory(Game game, string id)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItemCategories : m_Context.PoE2ItemCategories;
        var filter = Builders<ItemCategory>.Filter.Where(c => c.Id == id);
        return (await collection.FindAsync(filter)).FirstOrDefault();
    }

    public async Task<List<ItemCategory>> GetItemCategories(Game game)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItemCategories : m_Context.PoE2ItemCategories;
        var filter = Builders<ItemCategory>.Filter.Empty;
        return (await collection.FindAsync(filter)).ToList();
    }

    public async Task<ItemCategory> GetBaseItemCategory(Game game, string id)
    {
        var collection = game == Game.POE1 ? m_Context.PoEBaseItemCategories : m_Context.PoE2BaseItemCategories;
        var filter = Builders<ItemCategory>.Filter.Where(c => c.Id == id);
        return (await collection.FindAsync(filter)).FirstOrDefault();
    }

    public async Task<List<ItemCategory>> GetBaseItemCategories(Game game)
    {
        var collection = game == Game.POE1 ? m_Context.PoEBaseItemCategories : m_Context.PoE2BaseItemCategories;
        var filter = Builders<ItemCategory>.Filter.Empty;
        return (await collection.FindAsync(filter)).ToList();
    }

    public async Task<List<ItemCategory>> GetAllCategories(Game game)
    {
        IMongoCollection<ItemCategory> baseCategories = m_Context.PoEBaseItemCategories;
        IMongoCollection<ItemCategory> categories = m_Context.PoEItemCategories;
        if (game == Game.POE2)
        {
            baseCategories = m_Context.PoE2BaseItemCategories;
            categories = m_Context.PoE2ItemCategories;
        }
        var baseCategoriesTask = baseCategories.Find(Builders<ItemCategory>.Filter.Empty).ToListAsync();
        var categoriesTask = categories.Find(Builders<ItemCategory>.Filter.Empty).ToListAsync();

        await Task.WhenAll(baseCategoriesTask, categoriesTask);
        return baseCategoriesTask.Result.Concat(categoriesTask.Result).ToList();
    }

    public async Task<string> AddBaseCategory(Game game, ItemCategory category)
    {
        var collection = game == Game.POE1 ? m_Context.PoEBaseItemCategories : m_Context.PoE2BaseItemCategories;
        await collection.InsertOneAsync(category);
        return category.Id;
    }

    public async Task AddBaseCategories(Game game, List<ItemCategory> categories)
    {
        var collection = game == Game.POE1 ? m_Context.PoEBaseItemCategories : m_Context.PoE2BaseItemCategories;
        await collection.InsertManyAsync(categories);
    }

    public async Task<string?> AddItemCategory(Game game, string categoryName)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItemCategories : m_Context.PoE2ItemCategories;
        var filter = Builders<ItemCategory>.Filter.Where(c => c.Name.ToLower() == categoryName.ToLower());
        var category = (await collection.FindAsync(filter)).FirstOrDefault();
        if (category != null)
        {
            return null;
        }
        ItemCategory newCategory = new()
        {
            Name = categoryName
        };
        await collection.InsertOneAsync(newCategory);
        return newCategory.Id;
    }

    public async Task UpdateCategory(Game game, ItemCategory category)
    {
        var collection = game == Game.POE1 ? m_Context.PoEItemCategories : m_Context.PoE2ItemCategories;
        var filter = Builders<ItemCategory>.Filter.Where(c => c.Id == category.Id);
        var update = Builders<ItemCategory>.Update
            .Set(c => c.Name, category.Name);
        await collection.UpdateOneAsync(filter, update);
    }

    public async Task<string?> GetBaseCategoryId(Game game, string categoryName)
    {
        var collection = game == Game.POE1 ? m_Context.PoEBaseItemCategories : m_Context.PoE2BaseItemCategories;
        var filter = Builders<ItemCategory>.Filter.Where(c => c.Name.ToLower() == categoryName.ToLower());
        return (await collection.FindAsync(filter)).FirstOrDefault()?.Id;
    }

    public async Task<bool> AddItemCategoryToItem(Game game, string itemId, string categoryId)
    {
        ItemCategory? category = await GetItemCategory(game, categoryId);
        if (category == null)
        {
            return false;
        }
        Item? item = await GetItem(game, itemId);
        if (item == null)
        {
            return false;
        }
        for (int i = 0; i < item.Categories.Count; i++)
        {
            if (item.Categories[i] == category.Id)
            {
                return false;
            }
        }
        item.Categories.Add(category.Id);
        var collection = game == Game.POE1 ? m_Context.PoEItems : m_Context.PoE2Items;
        var filter = Builders<Item>.Filter.Where(i => i.Id == item.Id);
        await collection.ReplaceOneAsync(filter, item);
        return true;
    }
}