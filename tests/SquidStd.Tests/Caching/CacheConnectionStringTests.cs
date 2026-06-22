using SquidStd.Caching.Abstractions.Data.Config;

namespace SquidStd.Tests.Caching;

public class CacheConnectionStringTests
{
    [Fact]
    public void Parse_Memory_ReadsScheme()
    {
        var cs = CacheConnectionString.Parse("memory://localhost");

        Assert.Equal("memory", cs.Scheme);
        Assert.Equal("localhost", cs.Host);
    }

    [Fact]
    public void Parse_Redis_ReadsHostPortAndUserInfo()
    {
        var cs = CacheConnectionString.Parse("redis://user:pass@cache-host:6380");

        Assert.Equal("redis", cs.Scheme);
        Assert.Equal("cache-host", cs.Host);
        Assert.Equal(6380, cs.Port);
        Assert.Equal("user", cs.UserName);
        Assert.Equal("pass", cs.Password);
    }

    [Fact]
    public void ToCacheOptions_ReadsTtlAndPrefix()
    {
        var cs = CacheConnectionString.Parse("memory://localhost?defaultTtlSeconds=30&keyPrefix=app:");

        var options = cs.ToCacheOptions();

        Assert.Equal(TimeSpan.FromSeconds(30), options.DefaultTtl);
        Assert.Equal("app:", options.KeyPrefix);
    }

    [Fact]
    public void ToCacheOptions_Defaults_WhenNoParams()
    {
        var options = CacheConnectionString.Parse("memory://localhost").ToCacheOptions();

        Assert.Null(options.DefaultTtl);
        Assert.Equal(string.Empty, options.KeyPrefix);
    }
}
