

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Data;
using System.Drawing;
using System.Text.Json.Serialization;
using static System.Collections.Specialized.BitVector32;

//Model types

public class CreateFilterInfo
{
    public string Name { get; set; } = string.Empty;
    public FilterStrictness Strictness { get; set; }
}

public class DeleteFilterInfo
{
    public string Id { get; set; } = string.Empty;
}
