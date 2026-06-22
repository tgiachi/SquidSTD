using System.Text;
using SquidStd.Caching.Redis.Data.Config;
using SquidStd.Caching.Redis.Services;

namespace SquidStd.Tests.Caching.Redis;

[Collection(RedisCollection.Name)]
public class RedisCacheProviderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly RedisContainerFixture _fixture;

    public RedisCacheProviderTests(RedisContainerFixture fixture)
    {
        _fixture = fixture;
    }

    private RedisCacheProvider NewProvider()
        => new(new RedisCacheOptions { Configuration = _fixture.ConnectionString });

    private static ReadOnlyMemory<byte> Bytes(string s) => Encoding.UTF8.GetBytes(s);
    private static string Text(ReadOnlyMemory<byte> b) => Encoding.UTF8.GetString(b.Span);
    private static string Key() => "k-" + Guid.NewGuid().ToString("N");

    [Fact]
    public async Task SetThenGet_RoundTrips()
    {
        await using var provider = NewProvider();
        await provider.StartAsync();
        var key = Key();

        await provider.SetAsync(key, Bytes("hello"), null);
        var value = await provider.GetAsync(key);

        Assert.NotNull(value);
        Assert.Equal("hello", Text(value!.Value));
    }

    [Fact]
    public async Task Exists_And_Remove()
    {
        await using var provider = NewProvider();
        await provider.StartAsync();
        var key = Key();
        await provider.SetAsync(key, Bytes("v"), null);

        Assert.True(await provider.ExistsAsync(key));
        Assert.True(await provider.RemoveAsync(key));
        Assert.False(await provider.ExistsAsync(key));
    }

    [Fact]
    public async Task Ttl_Expires()
    {
        await using var provider = NewProvider();
        await provider.StartAsync();
        var key = Key();

        await provider.SetAsync(key, Bytes("v"), TimeSpan.FromMilliseconds(200));
        await Task.Delay(500);

        Assert.Null(await provider.GetAsync(key).WaitAsync(Timeout));
    }
}
