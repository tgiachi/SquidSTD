using Testcontainers.Redis;

namespace SquidStd.Tests.Caching.Redis;

/// <summary>
/// Starts a Redis container once for the whole collection and exposes its connection string.
/// </summary>
public sealed class RedisContainerFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder().Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync()
        => _container.StartAsync();

    public Task DisposeAsync()
        => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class RedisCollection : ICollectionFixture<RedisContainerFixture>
{
    public const string Name = "Redis";
}
