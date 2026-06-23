using System.Text.Json.Nodes;

namespace SquidStd.Search.Elasticsearch.Linq;

/// <summary>The translated Elasticsearch search request body plus paging/terminal intent.</summary>
public sealed class ElasticQuery
{
    /// <summary>The <c>query</c> clause (defaults to <c>match_all</c>).</summary>
    public JsonObject Query { get; set; } = new() { ["match_all"] = new JsonObject() };

    /// <summary>The <c>sort</c> array, or null.</summary>
    public JsonArray? Sort { get; set; }

    /// <summary>The <c>from</c> offset, or null.</summary>
    public int? From { get; set; }

    /// <summary>The <c>size</c> limit, or null.</summary>
    public int? Size { get; set; }

    /// <summary>Builds the full request body JSON.</summary>
    public JsonObject ToRequestBody()
    {
        var body = new JsonObject { ["query"] = Query.DeepClone() };

        if (Sort is not null)
        {
            body["sort"] = Sort.DeepClone();
        }

        if (From is not null)
        {
            body["from"] = From.Value;
        }

        if (Size is not null)
        {
            body["size"] = Size.Value;
        }

        return body;
    }
}
