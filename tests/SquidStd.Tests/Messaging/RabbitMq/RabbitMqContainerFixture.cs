using Testcontainers.RabbitMq;

namespace SquidStd.Tests.Messaging.RabbitMq;

/// <summary>
/// Starts a RabbitMQ container once for the whole collection and exposes its AMQP URI.
/// </summary>
public sealed class RabbitMqContainerFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder().Build();

    public string AmqpUri => _container.GetConnectionString();

    public Task InitializeAsync()
        => _container.StartAsync();

    public Task DisposeAsync()
        => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class RabbitMqCollection : ICollectionFixture<RabbitMqContainerFixture>
{
    public const string Name = "RabbitMq";
}
