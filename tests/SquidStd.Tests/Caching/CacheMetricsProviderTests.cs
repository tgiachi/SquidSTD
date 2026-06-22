using SquidStd.Caching.Abstractions.Services;

namespace SquidStd.Tests.Caching;

public class CacheMetricsProviderTests
{
    [Fact]
    public void ProviderName_IsCache()
        => Assert.Equal("cache", new CacheMetricsProvider().ProviderName);

    [Fact]
    public async Task CollectAsync_ReportsCountersAndHitRatio()
    {
        var metrics = new CacheMetricsProvider();
        metrics.OnHit("a");
        metrics.OnHit("b");
        metrics.OnMiss("c");
        metrics.OnSet("c");
        metrics.OnRemove("a");

        var samples = await metrics.CollectAsync();

        double Value(string name) => samples.Single(s => s.Name == name).Value;

        Assert.Equal(2, Value("hits"));
        Assert.Equal(1, Value("misses"));
        Assert.Equal(1, Value("sets"));
        Assert.Equal(1, Value("removes"));
        Assert.Equal(2d / 3d, Value("hit_ratio"), 3);
    }
}
