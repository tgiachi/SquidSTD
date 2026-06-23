using DryIoc;
using SquidStd.Search.Abstractions.Attributes;
using SquidStd.Search.Abstractions.Interfaces;
using SquidStd.Search.Elasticsearch.Data.Config;
using SquidStd.Search.Elasticsearch.Extensions;
using SquidStd.Search.Elasticsearch.Linq;

namespace SquidStd.Tests.Integration.Search;

[Collection(ElasticsearchCollection.Name)]
public class ElasticSearchServiceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    private readonly ElasticsearchContainerFixture _fixture;

    public ElasticSearchServiceTests(ElasticsearchContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [SearchIndex("it_orders")]
    private sealed record Order(string Id, string Status, int Total, string Name) : IIndexableEntity
    {
        public string IndexId => Id;
    }

    private ISearchService NewService()
    {
        var container = new Container();
        container.AddElasticsearch(new ElasticsearchOptions { Uri = new Uri(_fixture.ConnectionString) });

        return container.Resolve<ISearchService>();
    }

    [Fact]
    public async Task Index_Then_Query_FindsDocument()
    {
        var search = NewService();
        await search.IndexAsync(new Order("1", "open", 150, "Laptop"), refresh: true).WaitAsync(Timeout);

        var results = await search.Query<Order>().Where(o => o.Status == "open").ToListAsync();

        Assert.Contains(results, o => o.Id == "1");
    }

    [Fact]
    public async Task Query_Range_Order_Take()
    {
        var search = NewService();
        await search.IndexManyAsync(
            [new Order("a", "open", 50, "A"), new Order("b", "open", 200, "B"), new Order("c", "open", 300, "C")],
            refresh: true).WaitAsync(Timeout);

        var results = await search.Query<Order>()
            .Where(o => o.Total > 100)
            .OrderByDescending(o => o.Total)
            .Take(1)
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal("c", results[0].Id);
    }

    [Fact]
    public async Task FullText_Match()
    {
        var search = NewService();
        await search.IndexAsync(new Order("ft1", "open", 10, "Gaming Laptop Pro"), refresh: true).WaitAsync(Timeout);

        var results = await search.Query<Order>().Match("name", "laptop").ToListAsync();

        Assert.Contains(results, o => o.Id == "ft1");
    }

    [Fact]
    public async Task Count_And_FirstOrDefault()
    {
        var search = NewService();
        await search.IndexAsync(new Order("cnt", "counted", 1, "X"), refresh: true).WaitAsync(Timeout);

        var count = await search.Query<Order>().Where(o => o.Status == "counted").CountAsync();
        var first = await search.Query<Order>().Where(o => o.Status == "counted").FirstOrDefaultAsync();
        var none = await search.Query<Order>().Where(o => o.Status == "nope-nothing").FirstOrDefaultAsync();

        Assert.True(count >= 1);
        Assert.NotNull(first);
        Assert.Null(none);
    }

    [Fact]
    public async Task Delete_RemovesDocument()
    {
        var search = NewService();
        await search.IndexAsync(new Order("del", "open", 1, "Y"), refresh: true).WaitAsync(Timeout);

        var deleted = await search.DeleteAsync<Order>("del", refresh: true).WaitAsync(Timeout);
        var missing = await search.DeleteAsync<Order>("does-not-exist", refresh: true).WaitAsync(Timeout);
        var results = await search.Query<Order>().Where(o => o.Id == "del").ToListAsync();

        Assert.True(deleted);
        Assert.False(missing);
        Assert.DoesNotContain(results, o => o.Id == "del");
    }

    [Fact]
    public async Task Query_MissingIndex_ReturnsEmpty()
    {
        var search = NewService();

        var results = await search.Query<UnusedDoc>().Where(d => d.Status == "x").ToListAsync().WaitAsync(Timeout);

        Assert.Empty(results);
    }

    [SearchIndex("it_unused_index")]
    private sealed record UnusedDoc(string Id, string Status) : IIndexableEntity
    {
        public string IndexId => Id;
    }
}
