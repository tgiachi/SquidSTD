namespace SquidStd.Storage.S3.Data.Config;

/// <summary>
/// Connection options for the S3-compatible (MinIO) storage provider.
/// </summary>
public sealed class S3StorageOptions
{
    /// <summary>Endpoint host[:port], e.g. "localhost:9000".</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>Access key.</summary>
    public string AccessKey { get; init; } = string.Empty;

    /// <summary>Secret key.</summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>Bucket name.</summary>
    public string Bucket { get; init; } = string.Empty;

    /// <summary>Whether to use TLS. Default false.</summary>
    public bool UseSsl { get; init; }

    /// <summary>Optional region.</summary>
    public string? Region { get; init; }
}
