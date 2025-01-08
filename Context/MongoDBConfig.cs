public class GameDbData
{
    public string ItemCategoryCollectionName { get; set; } = string.Empty;
    public string ItemCollectionName { get; set; } = string.Empty;
}

public class MongoDbConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string FiltersCollectionName { get; set; } = string.Empty;
    public GameDbData PoEData { get; set; } = default!;
    public GameDbData PoE2Data { get; set; } = default!;
}