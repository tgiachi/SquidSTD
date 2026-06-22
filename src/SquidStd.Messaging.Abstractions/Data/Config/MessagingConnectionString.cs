using System.Collections.Frozen;
using System.Web;

namespace SquidStd.Messaging.Abstractions.Data.Config;

/// <summary>
/// Parsed messaging connection string of the form <c>scheme://[user:pass@]host[:port][/vhost][?params]</c>.
/// </summary>
public sealed class MessagingConnectionString
{
    private MessagingConnectionString(
        string scheme,
        string host,
        int? port,
        string? userName,
        string? password,
        string virtualHost,
        IReadOnlyDictionary<string, string> parameters
    )
    {
        Scheme = scheme;
        Host = host;
        Port = port;
        UserName = userName;
        Password = password;
        VirtualHost = virtualHost;
        Parameters = parameters;
    }

    /// <summary>URI scheme, e.g. "memory" or "rabbitmq".</summary>
    public string Scheme { get; }

    /// <summary>Host component.</summary>
    public string Host { get; }

    /// <summary>Port, when specified.</summary>
    public int? Port { get; }

    /// <summary>User name from the user-info component, when present.</summary>
    public string? UserName { get; }

    /// <summary>Password from the user-info component, when present.</summary>
    public string? Password { get; }

    /// <summary>Virtual host from the path (default "/").</summary>
    public string VirtualHost { get; }

    /// <summary>Query-string parameters.</summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }

    /// <summary>Parses a messaging connection string.</summary>
    public static MessagingConnectionString Parse(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var uri = new Uri(connectionString, UriKind.Absolute);

        string? userName = null;
        string? password = null;

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            userName = Uri.UnescapeDataString(parts[0]);
            password = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : null;
        }

        var virtualHost = uri.AbsolutePath.Trim('/');
        var query = HttpUtility.ParseQueryString(uri.Query);
        var parameters = query.AllKeys
                              .Where(static key => key is not null)
                              .ToFrozenDictionary(key => key!, key => query[key] ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        return new(
            uri.Scheme,
            uri.Host,
            uri.Port > 0 ? uri.Port : null,
            userName,
            password,
            string.IsNullOrEmpty(virtualHost) ? "/" : virtualHost,
            parameters
        );
    }

    /// <summary>Builds <see cref="MessagingOptions" /> from the query parameters.</summary>
    public MessagingOptions ToMessagingOptions()
        => new()
        {
            MaxDeliveryAttempts = Parameters.TryGetValue("maxDeliveryAttempts", out var max) && int.TryParse(max, out var parsedMax)
                                      ? parsedMax
                                      : 3,
            DeadLetterQueueSuffix = Parameters.TryGetValue("deadLetterSuffix", out var suffix) ? suffix : ".dlq",
            RetryDelay = Parameters.TryGetValue("retryDelayMs", out var delay) && int.TryParse(delay, out var parsedDelay)
                             ? TimeSpan.FromMilliseconds(parsedDelay)
                             : TimeSpan.Zero
        };
}
