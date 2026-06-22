using System.Collections.Frozen;
using System.Web;

namespace SquidStd.Caching.Abstractions.Data.Config;

/// <summary>
/// Parsed cache connection string of the form <c>scheme://[user:pass@]host[:port][?params]</c>.
/// </summary>
public sealed class CacheConnectionString
{
    private CacheConnectionString(
        string scheme,
        string host,
        int? port,
        string? userName,
        string? password,
        IReadOnlyDictionary<string, string> parameters
    )
    {
        Scheme = scheme;
        Host = host;
        Port = port;
        UserName = userName;
        Password = password;
        Parameters = parameters;
    }

    /// <summary>URI scheme, e.g. "memory" or "redis".</summary>
    public string Scheme { get; }

    /// <summary>Host component.</summary>
    public string Host { get; }

    /// <summary>Port, when specified.</summary>
    public int? Port { get; }

    /// <summary>User name from the user-info component, when present.</summary>
    public string? UserName { get; }

    /// <summary>Password from the user-info component, when present.</summary>
    public string? Password { get; }

    /// <summary>Query-string parameters.</summary>
    public IReadOnlyDictionary<string, string> Parameters { get; }

    /// <summary>Parses a cache connection string.</summary>
    public static CacheConnectionString Parse(string connectionString)
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
            parameters
        );
    }

    /// <summary>Builds <see cref="CacheOptions" /> from the query parameters.</summary>
    public CacheOptions ToCacheOptions()
        => new()
        {
            DefaultTtl = Parameters.TryGetValue("defaultTtlSeconds", out var ttl) && int.TryParse(ttl, out var seconds)
                             ? TimeSpan.FromSeconds(seconds)
                             : null,
            KeyPrefix = Parameters.TryGetValue("keyPrefix", out var prefix) ? prefix : string.Empty
        };
}
