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
        return await filter.ToFilterString(m_ItemService);
    }
}