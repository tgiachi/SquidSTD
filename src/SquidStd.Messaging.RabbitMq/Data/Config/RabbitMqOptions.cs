namespace SquidStd.Messaging.RabbitMq.Data.Config;

/// <summary>
/// Connection options for the RabbitMQ queue provider.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>Broker host. Default "localhost".</summary>
    public string HostName { get; init; } = "localhost";

    /// <summary>Broker port. Default 5672.</summary>
    public int Port { get; init; } = 5672;

    /// <summary>Virtual host. Default "/".</summary>
    public string VirtualHost { get; init; } = "/";

    /// <summary>User name. Default "guest".</summary>
    public string UserName { get; init; } = "guest";

    /// <summary>Password. Default "guest".</summary>
    public string Password { get; init; } = "guest";

    /// <summary>When set, the AMQP URI overrides the individual fields.</summary>
    public Uri? Uri { get; init; }

    /// <summary>Consumer prefetch count. Default 10.</summary>
    public ushort PrefetchCount { get; init; } = 10;
}
