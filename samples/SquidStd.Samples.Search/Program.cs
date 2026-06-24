using SquidStd.Core.Data.Bootstrap;
using SquidStd.Search.Abstractions.Attributes;
using SquidStd.Search.Abstractions.Interfaces;
using SquidStd.Search.Elasticsearch.Data.Config;
using SquidStd.Search.Elasticsearch.Extensions;
using SquidStd.Search.Elasticsearch.Linq;
using SquidStd.Services.Core.Services.Bootstrap;

var bootstrap = SquidStdBootstrap.Create(new SquidStdOptions
{
    ConfigName = "squidstd",
    RootDirectory = AppContext.BaseDirectory
});

#region step-1
bootstrap.ConfigureServices(container => container.AddElasticsearch(new ElasticsearchOptions
{
    Uri = new Uri("http://localhost:9200")
}));
#endregion

await bootstrap.StartAsync();

var search = bootstrap.Resolve<ISearchService>();

#region step-3
await search.IndexAsync(new Order("1", "open", 150), refresh: true);

var open = await search.Query<Order>()
    .Where(o => o.Status == "open")
    .ToListAsync();

Console.WriteLine($"found {open.Count} open order(s)");
#endregion

await bootstrap.StopAsync();

#region step-2
/// <summary>An order document stored in the <c>orders</c> index.</summary>
[SearchIndex("orders")]
public sealed record Order(string Id, string Status, int Total) : IIndexableEntity
{
    /// <summary>Stable document id within the index.</summary>
    public string IndexId => Id;
}
#endregion
