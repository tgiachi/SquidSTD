namespace SquidStd.Search.Abstractions.Attributes;

/// <summary>
///     Declares the Elasticsearch index for a type. The name supports environment-variable expansion:
///     <c>${VAR}</c> and <c>${VAR:-default}</c> (e.g. <c>"orders_${ENV:-dev}"</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class SearchIndexAttribute : Attribute
{
    /// <summary>The index name (template).</summary>
    public string Name { get; }

    /// <summary>Initializes the attribute with the index name template.</summary>
    public SearchIndexAttribute(string name)
    {
        Name = name;
    }
}
