using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Text.Json.Serialization;

public enum FilterRuleItemType
{
    RULE,
    RULE_BLOCK,
}

public enum FilterRuleType
{
    RULE_MINIMAL,
    RULE_FULL
}

public enum FilterStrictness
{
    REGULAR, STRICT
}

public interface IRuleItem
{
    public Guid Id { get; set; }
    public FilterRuleItemType Type { get; set; }
}

public interface IRuleItemDiff
{
    public Guid Id { get; set; }
    public FilterRuleItemType Type { get; set; }
}

// Separate interfaces for MongoDB serialization/deserialization
public interface IFilterRuleItem : IRuleItem { }
public interface IFilterRuleStructureItem : IRuleItem { }
public interface IFilterRuleItemDiff : IRuleItemDiff { }
public interface IFilterRuleStructureItemDiff : IRuleItemDiff { }

public abstract class FilterComponent
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public uint Position { get; set; } = 0;
}

public abstract class FilterImageComponent : FilterComponent
{
    [JsonPropertyName("imgSrc")]
    public string ImgSrc { get; set; } = string.Empty;
}

public class ListDiff<T>
{
    public List<T> Added = [];
    public List<T> Removed = [];
}

public abstract class FilterRulesContainer<T> : FilterImageComponent where T : IRuleItem
{
    [JsonPropertyName("rulesType")]
    public FilterRuleType RulesType { get; set; } = FilterRuleType.RULE_MINIMAL;
    [JsonPropertyName("allowedCategories")]
    public List<string> AllowedCategories { get; set; } = [];

    [JsonPropertyName("allowUserCreatedRules")]
    public bool AllowUserCreatedRules { get; set; } = false;

    [JsonPropertyName("rules")]
    public List<T> Rules { get; set; } = [];
}

public abstract class FilterComponentDiff(Guid id)
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = id;

    [BsonIgnoreIfNull]
    [JsonPropertyName("name")]
    public string? Name { get; set; } = null;

    [BsonIgnoreIfNull]
    [JsonPropertyName("position")]
    public uint? Position { get; set; } = null;

    public bool HasChanges()
    {
        return (Name != null || Position != null);
    }
}

public abstract class FilterImageComponentDiff(Guid id) : FilterComponentDiff(id)
{
    [BsonIgnoreIfNull]
    [JsonPropertyName("imgSrc")]
    public string? ImgSrc { get; set; } = null;

    public new bool HasChanges() 
    {
        return (base.HasChanges() || ImgSrc != null);
    }
}

public abstract class FilterRulesContainerDiff<TAdded, TDiff>(Guid id) : FilterImageComponentDiff(id) where TAdded : IRuleItem where TDiff : IRuleItemDiff
{
    [BsonIgnoreIfNull]
    [JsonPropertyName("rulesType")]
    public FilterRuleType? RulesType { get; set; } = null;

    [BsonIgnoreIfNull]
    [JsonPropertyName("allowedCategories")]
    public ListDiff<string>? AllowedCategories { get; set; } = null;

    [BsonIgnoreIfNull]
    [JsonPropertyName("allowUserCreatedRules")]
    public bool? AllowUserCreatedRules { get; set; } = null;

    [BsonIgnoreIfNull]
    [JsonPropertyName("ruleChanges")]
    public FilterUpdateChanges<TAdded, TDiff> RuleChanges { get; set; } = new();

    public new bool HasChanges()
    {
        return (base.HasChanges() || RulesType != null
            || AllowedCategories != null || AllowUserCreatedRules != null
            || RuleChanges.Added != null || RuleChanges.Removed != null 
            || RuleChanges.Changed != null);
    }
}


public abstract class FilterBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
}