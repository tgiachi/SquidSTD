using SquidStd.Caching.Abstractions.Data.Config;
using SquidStd.Caching.Abstractions.Services;
using SquidStd.Core.Json;
using SquidStd.Tests.Support;

namespace SquidStd.Tests.Caching;

public class CacheServiceTests
{
    private static CacheService NewService(
        FakeCacheProvider provider,
        CacheOptions? options = null,
        CacheMetricsProvider? metrics = null
    )
    {
        var serializer = new JsonDataSerializer();

        return new CacheService(provider, serializer, serializer, options ?? new CacheOptions(), metrics);
    }

    [Fact]
    public async Task SetThenGet_RoundTrips()
    {
        var service = NewService(new FakeCacheProvider());

        await service.SetAsync("k", 42);

        Assert.Equal(42, await service.GetAsync<int>("k"));
    }

    [Fact]
    public async Task Get_Missing_ReturnsDefault()
        => Assert.Null(await NewService(new FakeCacheProvider()).GetAsync<string>("absent"));

    [Fact]
    public async Task Set_UsesDefaultTtl_WhenNoneGiven()
    {
        var provider = new FakeCacheProvider();
        var service = NewService(provider, new CacheOptions { DefaultTtl = TimeSpan.FromSeconds(30) });

        await service.SetAsync("k", "v");

        Assert.Equal(TimeSpan.FromSeconds(30), provider.LastTtl);
    }

    [Fact]
    public async Task Set_PerEntryTtl_OverridesDefault()
    {
        var provider = new FakeCacheProvider();
        var service = NewService(provider, new CacheOptions { DefaultTtl = TimeSpan.FromSeconds(30) });

        await service.SetAsync("k", "v", TimeSpan.FromSeconds(5));

        Assert.Equal(TimeSpan.FromSeconds(5), provider.LastTtl);
    }

    [Fact]
    public async Task KeyPrefix_IsAppliedToProvider()
    {
        var provider = new FakeCacheProvider();
        var service = NewService(provider, new CacheOptions { KeyPrefix = "app:" });

        await service.SetAsync("k", "v");

        Assert.True(await provider.ExistsAsync("app:k"));
        Assert.False(await provider.ExistsAsync("k"));
    }

    [Fact]
    public async Task GetOrSet_Miss_InvokesFactoryAndStores()
    {
        var provider = new FakeCacheProvider();
        var service = NewService(provider);
        var calls = 0;

        var value = await service.GetOrSetAsync("k", _ => { calls++; return Task.FromResult(99); });

        Assert.Equal(99, value);
        Assert.Equal(1, calls);
        Assert.Equal(99, await service.GetAsync<int>("k"));
    }

    [Fact]
    public async Task GetOrSet_Hit_DoesNotInvokeFactory()
    {
        var service = NewService(new FakeCacheProvider());
        await service.SetAsync("k", 7);
        var calls = 0;

        var value = await service.GetOrSetAsync("k", _ => { calls++; return Task.FromResult(0); });

        Assert.Equal(7, value);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Metrics_RecordHitAndMiss()
    {
        var metrics = new CacheMetricsProvider();
        var service = NewService(new FakeCacheProvider(), metrics: metrics);

        await service.GetAsync<int>("absent"); // miss
        await service.SetAsync("k", 1);
        await service.GetAsync<int>("k");       // hit

        var samples = await metrics.CollectAsync();
        Assert.Equal(1, samples.Single(s => s.Name == "hits").Value);
        Assert.Equal(1, samples.Single(s => s.Name == "misses").Value);
    }
}
