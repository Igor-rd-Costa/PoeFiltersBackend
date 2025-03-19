using MongoDB.Driver;
using PoEFiltersBackend.Models;

public class GameCollections
{
    public GameCollections(Game game, IMongoDatabase database)
    {
        string prefix = (game == Game.POE1) ? "PoE" : "PoE2";
        ItemCategories = database.GetCollection<ItemCategory>(prefix + "ItemCategories");
        BaseItemCategories = database.GetCollection<ItemCategory>(prefix + "BaseItemCategories");
        Items = database.GetCollection<Item>(prefix + "Items");
        DefaultFilters = database.GetCollection<DefaultFilter>(prefix + "DefaultFilters");
        Filters = database.GetCollection<Filter>(prefix + "Filters");
        DefaultDiffs = database.GetCollection<FilterDiff>(prefix + "DefaultDiffs");
        StructureDiffs = database.GetCollection<FilterStructureDiff>(prefix + "StructureDiffs");
    }

    public readonly IMongoCollection<DefaultFilter> DefaultFilters;
    public readonly IMongoCollection<Filter> Filters;
    public readonly IMongoCollection<ItemCategory> ItemCategories;
    public readonly IMongoCollection<ItemCategory> BaseItemCategories;
    public readonly IMongoCollection<Item> Items;
    public readonly IMongoCollection<FilterDiff> DefaultDiffs;
    public readonly IMongoCollection<FilterStructureDiff> StructureDiffs;
}

public class MongoDbContext
{
    private readonly IMongoDatabase m_Database;

    public MongoDbContext(IConfiguration configuration)
    {
        var client = new MongoClient(configuration.GetConnectionString("MongoDb"));
        m_Database = client.GetDatabase("Filters");
        PoECollections = new GameCollections(Game.POE1, m_Database);
        PoE2Collections = new GameCollections(Game.POE2, m_Database);
        Users = m_Database.GetCollection<User>("Users");
        FilterStructures = m_Database.GetCollection<FilterStructure>("FilterStructures");
        UserTokens = m_Database.GetCollection<ProviderToken>("UserTokens");
    }

    public readonly IMongoCollection<User> Users;
    public readonly IMongoCollection<FilterStructure> FilterStructures;
    public readonly IMongoCollection<ProviderToken> UserTokens;
    public readonly GameCollections PoECollections;
    public readonly GameCollections PoE2Collections;
}