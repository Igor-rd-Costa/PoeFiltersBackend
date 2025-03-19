

using MongoDB.Driver;

public class FilterStructureService
{
    private readonly MongoDbContext m_Context;

    public FilterStructureService(MongoDbContext context)
    {
        m_Context = context;
    }

    public async Task<FilterStructure> AddAsync(Game game)
    {
        var filter = Builders<FilterStructure>.Filter.Where(f => f.Game == game);
        FilterStructure? fs = (await m_Context.FilterStructures.FindAsync(filter)).FirstOrDefault();
        if (fs != null)
        {
            return fs;
        }
        fs = new()
        {
            Game = game,
            StructureVersion = 0,
            Sections = [
                new() { Name = "Unnamed Section" }
            ]
        };
        await m_Context.FilterStructures.InsertOneAsync(fs);
        return fs;
    }

    public async Task<FilterStructure?> GetStructureAsync(Game game)
    {
        var filter = Builders<FilterStructure>.Filter.Where(f => f.Game == game);
        return await m_Context.FilterStructures.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<uint> GetStructureVersionAsync(Game game)
    {
        var filter = Builders<FilterStructure>.Filter.Where(f => f.Game == game);
        return await m_Context.FilterStructures.Find(filter).Project(f => f.StructureVersion).FirstOrDefaultAsync();
    }

    public async Task ReplaceStructureAsync(Game game, FilterStructure newStructure)
    {
        var filter = Builders<FilterStructure>.Filter.Where(f => f.Game == game);
        await m_Context.FilterStructures.ReplaceOneAsync(filter, newStructure, new ReplaceOptions() { IsUpsert = true });
    }

    public async Task Reset(Game game)
    {
        var collections = (game == Game.POE1) ? m_Context.PoECollections : m_Context.PoE2Collections;
        await m_Context.FilterStructures.DeleteManyAsync(FilterDefinition<FilterStructure>.Empty);
        await collections.StructureDiffs.DeleteManyAsync(FilterDefinition<FilterStructureDiff>.Empty);
        await collections.DefaultFilters.DeleteManyAsync(FilterDefinition<DefaultFilter>.Empty);
        await collections.DefaultDiffs.DeleteManyAsync(FilterDefinition<FilterDiff>.Empty);
    }
}