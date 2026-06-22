using DryIoc;
using SquidStd.Caching.Abstractions.Interfaces;
using SquidStd.Caching.Extensions;

namespace SquidStd.Tests.Caching;

public class CacheRegistrationTests
{
    [Fact]
    public void AddInMemoryCache_ResolvesService()
    {
        var container = new Container();

        container.AddInMemoryCache();

        Assert.NotNull(container.Resolve<ICacheService>());
        Assert.NotNull(container.Resolve<ICacheProvider>());
    }

    [Fact]
    public void AddInMemoryCache_FromUrl_WrongScheme_Throws()
    {
        var container = new Container();

        Assert.Throws<ArgumentException>(() => container.AddInMemoryCache("redis://localhost"));
    }

    [Fact]
    public async Task AddInMemoryCache_FromUrl_ResolvesAndWorks()
    {
        var container = new Container();

        container.AddInMemoryCache("memory://localhost?keyPrefix=app:");
        var cache = container.Resolve<ICacheService>();

        await cache.SetAsync("k", 5);
        Assert.Equal(5, await cache.GetAsync<int>("k"));
    }
}
