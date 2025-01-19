using MongoDB.Driver;
using PoEFiltersBackend.Models;

public class MongoDbContext
{
    private readonly IMongoDatabase m_Database;

    public MongoDbContext(IConfiguration configuration)
    {
        var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
        m_Database = client.GetDatabase("Filters");
    }

    public IMongoCollection<Filter> Filters => m_Database.GetCollection<Filter>("Filters");
    public IMongoCollection<User> Users => m_Database.GetCollection<User>("Users");
    public IMongoCollection<Item> PoEItems => m_Database.GetCollection<Item>("PoEItems");
    public IMongoCollection<Item> PoE2Items => m_Database.GetCollection<Item>("PoE2Items");
    public IMongoCollection<ItemCategory> PoEItemCategories => m_Database.GetCollection<ItemCategory>("PoEItemCategories");
    public IMongoCollection<ItemCategory> PoE2ItemCategories => m_Database.GetCollection<ItemCategory>("PoE2ItemCategories");
    public IMongoCollection<ProviderToken> UserTokens => m_Database.GetCollection<ProviderToken>("UserTokens");
}