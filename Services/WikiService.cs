

using PoEFiltersBackend.Models;
using System.Text.Json.Serialization;

public class CargoQueryObject<T>
{
    public T Title { get; set; } = default!;
}
public class CargoQueryResponse<T>
{
    [JsonPropertyName("cargoquery")]
    public List<CargoQueryObject<T>> CargoQuery { get; set; } = [];

    public List<T> ToList()
    {
        List<T> list = [];
        foreach (CargoQueryObject<T> obj in CargoQuery)
        {
            list.Add(obj.Title);
        }
        return list;
    }
}

public class ClassIdCargoQueryObject
{
    [JsonPropertyName("class id")]
    public string ClassId { get; set; } = string.Empty;
}

public class ItemCargoQueryObject
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("class id")]
    public string Category { get; set; } = string.Empty;
}

public class CountCargoQueryObject
{
    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class WikiService
{
    private readonly string m_APIEndpoint = "https://www.poe2wiki.net/w/api.php?action=cargoquery&format=json";
    private readonly HttpClient m_Http;
    private readonly ItemsService m_ItemsService;
    public WikiService(HttpClient http, ItemsService itemsService)
    {
        m_Http = http;
        m_ItemsService = itemsService;
    }

    public async Task<List<ItemCargoQueryObject>> GetItems()
    {
        int limit = 500;
        List<ItemCategory> ignoredCategories = await m_ItemsService.GetIgnoredCategories(Game.POE2);
        string whereClause = "where=";
        for (int i = 0; i < ignoredCategories.Count; i++)
        {
            ItemCategory category = ignoredCategories[i];
            whereClause += $"class_id!=\"{category.Name}\"";
            if (i < (ignoredCategories.Count - 1))
            {
                whereClause += " OR ";
            }
        }
        string query = m_APIEndpoint + $"&tables=items&fields=COUNT(*)=count&{whereClause}";
        int maxCount = (await (await m_Http.GetAsync(query))
            .Content.ReadFromJsonAsync<CargoQueryResponse<CountCargoQueryObject>>())?.CargoQuery[0].Title.Count ?? 0;
        int offset = 0;

        List<ItemCargoQueryObject> items = [];
        List<ItemCargoQueryObject> queryItems = [];
        while (offset < maxCount)
        {
            var result = await m_Http.GetAsync(m_APIEndpoint + $"&tables=items&fields=name,class_id&limit={limit}&offset={offset}&{whereClause}");
            if (result == null || result.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return items;
            }
            queryItems = (await result.Content.ReadFromJsonAsync<CargoQueryResponse<ItemCargoQueryObject>>())?.ToList() ?? [];

            if (queryItems.Count == 0)
            {
                break;
            }
            items.AddRange(queryItems);
            offset += queryItems.Count;
        }
        return items;
    }

    public async Task<List<string>> GetCategories()
    {
        int limit = 500;
        var result = await m_Http.GetAsync(m_APIEndpoint + $"&tables=items&fields=class_id&group_by=class_id&limit={limit}");
        if (result.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return [];
        }
        
        var str = await result.Content.ReadAsStringAsync();
        var a = await result.Content.ReadFromJsonAsync<CargoQueryResponse<ClassIdCargoQueryObject>>();
        if (a != null)
        {
            return a.ToList().Select(i => i.ClassId).ToList();
        }
        return [];
    }
}