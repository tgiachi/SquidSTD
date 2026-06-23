namespace SquidStd.Search.Elasticsearch.Data.Config;

/// <summary>Configuration for the Elasticsearch search provider.</summary>
public sealed class ElasticsearchOptions
{
    /// <summary>Cluster endpoint, e.g. <c>http://localhost:9200</c>.</summary>
    public Uri Uri { get; set; } = new("http://localhost:9200");

    /// <summary>Optional basic-auth username.</summary>
    public string? Username { get; set; }

    /// <summary>Optional basic-auth password.</summary>
    public string? Password { get; set; }

    /// <summary>Optional API key (Base64) used instead of basic auth.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Optional prefix prepended to every resolved index name.</summary>
    public string? IndexPrefix { get; set; }

    /// <summary>When true, accepts self-signed/untrusted server certificates (tests/local only).</summary>
    public bool AllowUntrustedCertificate { get; set; }
}
