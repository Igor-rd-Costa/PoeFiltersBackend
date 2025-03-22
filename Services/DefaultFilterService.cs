

using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;

public class DefaultFilterService
{
    private readonly MongoDbContext m_Context;
    private readonly FilterStructureService m_FilterStructureService;

    public DefaultFilterService(MongoDbContext context, FilterStructureService filterStructureService)
    {
        m_Context = context;
        m_FilterStructureService = filterStructureService;
    }

    public async Task SaveAsync(Game game, DefaultFilter defaultFilter)
    {
        var collection = Collection(game);
        var filter = Builders<DefaultFilter>.Filter.Where(f => f.Strictness == defaultFilter.Strictness);
        await collection.ReplaceOneAsync(filter, defaultFilter);
    }

    public async Task<DefaultFilter?> GetAsync(Game game, FilterStrictness strictness)
    {
        var collection = Collection(game);
        var filter = Builders<DefaultFilter>.Filter.Where(f => f.Strictness == strictness);
        return (await collection.FindAsync(filter)).FirstOrDefault();
    }

    public async Task<uint> GetVersionAsync(Game game, FilterStrictness strictness)
    {
        var collection = Collection(game);
        var filter = Builders<DefaultFilter>.Filter.Where(f => f.Strictness == strictness);
        return (await collection.Find(filter).Project(f => f.StructureVersion).FirstOrDefaultAsync());
    }

    public List<FilterStrictness> GetStrictnessesAsync(Game game)
    {
        var collection = Collection(game);
        return collection.AsQueryable()
            .GroupBy(f => f.Strictness)
            .Select(g => g.First().Strictness)
            .ToList();
    }

    public async Task<DefaultFilter> Add(Game game, FilterStrictness strictness)
    {
        var collection = Collection(game);
        var filter = Builders<DefaultFilter>.Filter.Where(f => f.Strictness == strictness);
        DefaultFilter? df = (await collection.FindAsync(filter)).FirstOrDefault();
        if (df != null)
        {
            return df;
        }
        FilterStructure? fs = await m_FilterStructureService.GetStructureAsync(game);
        if (fs == null)
        {
            fs = await m_FilterStructureService.AddAsync(game);
        }
        df = new()
        {
            StructureVersion = fs.StructureVersion,
            Strictness = strictness,
            Sections = fs.MakeFilter()
        };
        await collection.InsertOneAsync(df);
        return df;
    }

    private IMongoCollection<DefaultFilter> Collection(Game game)
    {
        return (game == Game.POE1) ? m_Context.PoECollections.DefaultFilters : m_Context.PoE2Collections.DefaultFilters;
    }
}