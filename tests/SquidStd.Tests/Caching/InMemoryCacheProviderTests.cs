using Microsoft.Extensions.Caching.Memory;
using SquidStd.Caching.Services;

namespace SquidStd.Tests.Caching;

public class InMemoryCacheProviderTests
{
    private static InMemoryCacheProvider NewProvider()
        => new(new MemoryCache(new MemoryCacheOptions()));

    private static ReadOnlyMemory<byte> Bytes(string s) => System.Text.Encoding.UTF8.GetBytes(s);

    [Fact]
    public async Task SetThenGet_ReturnsValue()
    {
        var provider = NewProvider();
        await provider.SetAsync("k", Bytes("v"), null);

        var value = await provider.GetAsync("k");

        Assert.NotNull(value);
        Assert.Equal("v", System.Text.Encoding.UTF8.GetString(value!.Value.Span));
    }

    [Fact]
    public async Task Get_Missing_ReturnsNull()
        => Assert.Null(await NewProvider().GetAsync("absent"));

    [Fact]
    public async Task Exists_And_Remove()
    {
        var provider = NewProvider();
        await provider.SetAsync("k", Bytes("v"), null);

        Assert.True(await provider.ExistsAsync("k"));
        Assert.True(await provider.RemoveAsync("k"));
        Assert.False(await provider.ExistsAsync("k"));
        Assert.False(await provider.RemoveAsync("k"));
    }

    [Fact]
    public async Task Ttl_Expires()
    {
        var provider = NewProvider();
        await provider.SetAsync("k", Bytes("v"), TimeSpan.FromMilliseconds(50));

        await Task.Delay(120);

        Assert.Null(await provider.GetAsync("k"));
    }
}
